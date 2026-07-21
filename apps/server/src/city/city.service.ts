import { HttpStatus, Inject, Injectable } from '@nestjs/common';
import { quoteBuildingUpgrade } from '@kingdom/domain';
import { sql } from 'kysely';
import { v7 as uuidv7 } from 'uuid';
import type { Resources } from '@kingdom/contracts';
import { getCityBuildingRule } from './building-rules.js';
import { IdempotencyService } from '../platform/idempotency.service.js';
import { DomainError } from '../platform/errors.js';

@Injectable()
export class CityService {
  constructor(@Inject(IdempotencyService) private readonly idempotency: IdempotencyService) {}

  async startBuildingUpgrade(
    playerId: string,
    idempotencyKey: string,
    buildingId: string,
  ): Promise<{ statusCode: number; response: Record<string, unknown> }> {
    return this.idempotency.execute(
      playerId,
      idempotencyKey,
      IdempotencyService.hashRequest({ building_id: buildingId }),
      async (transaction) => {
        const building = await transaction
          .selectFrom('buildings')
          .innerJoin('cities', 'cities.id', 'buildings.city_id')
          .select([
            'buildings.id',
            'buildings.city_id',
            'buildings.slot_key',
            'buildings.level',
            'cities.resources',
          ])
          .where('buildings.id', '=', buildingId)
          .where('cities.player_id', '=', playerId)
          .forUpdate()
          .executeTakeFirst();
        if (!building) {
          throw new DomainError(
            HttpStatus.NOT_FOUND,
            'BUILDING_NOT_FOUND',
            'Building does not exist.',
          );
        }

        if (building.level >= 40) {
          throw new DomainError(
            HttpStatus.CONFLICT,
            'BUILDING_MAX_LEVEL',
            'This building has reached the current maximum level.',
          );
        }

        const rule = getCityBuildingRule(building.slot_key);
        if (rule) {
          const palace = await transaction
            .selectFrom('buildings')
            .select('level')
            .where('city_id', '=', building.city_id)
            .where('slot_key', '=', 'palace')
            .executeTakeFirst();
          const palaceLevel = palace?.level ?? 0;
          if (palaceLevel < rule.requiredPalaceLevel) {
            throw new DomainError(
              HttpStatus.CONFLICT,
              'PALACE_LEVEL_REQUIRED',
              `${rule.displayName} requires Palace level ${rule.requiredPalaceLevel}.`,
            );
          }
        }

        const activeQueue = await transaction
          .selectFrom('queue_jobs')
          .select('id')
          .where('city_id', '=', building.city_id)
          .where('job_type', '=', 'building_upgrade')
          .where('status', 'in', ['pending', 'processing'])
          .executeTakeFirst();
        if (activeQueue) {
          throw new DomainError(
            HttpStatus.CONFLICT,
            'BUILD_QUEUE_OCCUPIED',
            'The city already has an active building upgrade.',
          );
        }

        const quote = quoteBuildingUpgrade(building.level);
        const resources = building.resources as Resources;
        const food = BigInt(resources.food);
        const wood = BigInt(resources.wood);
        if (food < quote.foodCost || wood < quote.woodCost) {
          throw new DomainError(
            HttpStatus.CONFLICT,
            'INSUFFICIENT_RESOURCES',
            'The city does not have enough food and wood.',
          );
        }

        const updatedResources: Resources = {
          ...resources,
          food: (food - quote.foodCost).toString(),
          wood: (wood - quote.woodCost).toString(),
        };
        const queueId = uuidv7();
        const startAt = new Date();
        const finishAt = new Date(startAt.getTime() + quote.durationMs);
        const correlationId = idempotencyKey;

        await transaction
          .updateTable('cities')
          .set({ resources: updatedResources })
          .where('id', '=', building.city_id)
          .execute();
        await transaction
          .insertInto('queue_jobs')
          .values({
            id: queueId,
            city_id: building.city_id,
            job_type: 'building_upgrade',
            ref_id: building.id,
            start_at: startAt,
            finish_at: finishAt,
            status: 'pending',
            payload: { target_level: quote.targetLevel },
            lease_until: null,
          })
          .execute();
        await transaction
          .insertInto('economy_ledger')
          .values([
            {
              id: uuidv7(),
              player_id: playerId,
              resource_key: 'food',
              delta: (-quote.foodCost).toString(),
              reason: 'building_upgrade_started',
              ref_type: 'queue_job',
              ref_id: queueId,
              correlation_id: correlationId,
            },
            {
              id: uuidv7(),
              player_id: playerId,
              resource_key: 'wood',
              delta: (-quote.woodCost).toString(),
              reason: 'building_upgrade_started',
              ref_type: 'queue_job',
              ref_id: queueId,
              correlation_id: correlationId,
            },
          ])
          .execute();
        await transaction
          .insertInto('outbox_events')
          .values({
            event_id: uuidv7(),
            aggregate_type: 'player',
            aggregate_id: playerId,
            event_type: 'city.queue.started.v1',
            payload: {
              queue_id: queueId,
              building_id: building.id,
              finish_at: finishAt.toISOString(),
            },
            published_at: null,
          })
          .execute();

        return {
          statusCode: HttpStatus.CREATED,
          response: {
            data: {
              queue: {
                id: queueId,
                job_type: 'building_upgrade',
                ref_id: building.id,
                start_at: startAt.toISOString(),
                finish_at: finishAt.toISOString(),
                status: 'pending',
              },
              resources: updatedResources,
            },
          },
        };
      },
    );
  }

  async speedUpBuildingUpgrade(
    playerId: string,
    idempotencyKey: string,
    buildingId: string,
  ): Promise<{ statusCode: number; response: Record<string, unknown> }> {
    return this.idempotency.execute(
      playerId,
      idempotencyKey,
      IdempotencyService.hashRequest({ building_id: buildingId, speed_up: true }),
      async (transaction) => {
        const building = await transaction
          .selectFrom('buildings')
          .innerJoin('cities', 'cities.id', 'buildings.city_id')
          .select([
            'buildings.id',
            'buildings.city_id',
            'buildings.slot_key',
            'buildings.building_type',
            'buildings.level',
          ])
          .where('buildings.id', '=', buildingId)
          .where('cities.player_id', '=', playerId)
          .forUpdate()
          .executeTakeFirst();
        if (!building) {
          throw new DomainError(
            HttpStatus.NOT_FOUND,
            'BUILDING_NOT_FOUND',
            'Building does not exist.',
          );
        }

        const queue = await transaction
          .selectFrom('queue_jobs')
          .selectAll()
          .where('city_id', '=', building.city_id)
          .where('job_type', '=', 'building_upgrade')
          .where('ref_id', '=', building.id)
          .where('status', 'in', ['pending', 'processing'])
          .orderBy('finish_at')
          .forUpdate()
          .executeTakeFirst();
        if (!queue) {
          throw new DomainError(
            HttpStatus.CONFLICT,
            'NO_ACTIVE_BUILD_QUEUE',
            'This building has no active upgrade queue.',
          );
        }

        const payloadTarget = queue.payload['target_level'];
        const targetLevel =
          typeof payloadTarget === 'number' && Number.isInteger(payloadTarget)
            ? payloadTarget
            : building.level + 1;
        const completedAt = new Date();

        await transaction
          .updateTable('buildings')
          .set({ level: targetLevel, version: sql<number>`version + 1` })
          .where('id', '=', building.id)
          .executeTakeFirstOrThrow();
        await transaction
          .updateTable('queue_jobs')
          .set({ status: 'completed', finish_at: completedAt, lease_until: null })
          .where('id', '=', queue.id)
          .executeTakeFirstOrThrow();
        await transaction
          .insertInto('outbox_events')
          .values({
            event_id: uuidv7(),
            aggregate_type: 'player',
            aggregate_id: playerId,
            event_type: 'city.queue.completed.v1',
            payload: {
              queue_id: queue.id,
              ref_id: building.id,
              used_speedup: true,
            },
            published_at: null,
          })
          .execute();

        return {
          statusCode: HttpStatus.OK,
          response: {
            data: {
              queue: {
                id: queue.id,
                job_type: queue.job_type,
                ref_id: queue.ref_id,
                start_at: new Date(queue.start_at).toISOString(),
                finish_at: completedAt.toISOString(),
                status: 'completed',
              },
              building: {
                id: building.id,
                slot_key: building.slot_key,
                building_type: building.building_type,
                level: targetLevel,
              },
            },
          },
        };
      },
    );
  }
}
