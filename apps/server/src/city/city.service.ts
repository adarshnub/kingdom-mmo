import { HttpStatus, Inject, Injectable } from '@nestjs/common';
import { quoteBuildingUpgrade } from '@kingdom/domain';
import { v7 as uuidv7 } from 'uuid';
import type { Resources } from '@kingdom/contracts';
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
          .select(['buildings.id', 'buildings.city_id', 'buildings.level', 'cities.resources'])
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
}
