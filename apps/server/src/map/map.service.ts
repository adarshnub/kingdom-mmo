import { HttpStatus, Inject, Injectable } from '@nestjs/common';
import { calculateTravelDurationMs } from '@kingdom/domain';
import type { DispatchMarchRequest, ViewportQuery } from '@kingdom/contracts';
import { v7 as uuidv7 } from 'uuid';
import { DatabaseService } from '../database/database.service.js';
import { IdempotencyService } from '../platform/idempotency.service.js';
import { DomainError } from '../platform/errors.js';

@Injectable()
export class MapService {
  constructor(
    @Inject(DatabaseService) private readonly database: DatabaseService,
    @Inject(IdempotencyService) private readonly idempotency: IdempotencyService,
  ) {}

  async getViewport(playerId: string, viewport: ViewportQuery): Promise<Record<string, unknown>> {
    const player = await this.database
      .selectFrom('players')
      .select('kingdom_id')
      .where('id', '=', playerId)
      .executeTakeFirst();
    if (!player)
      throw new DomainError(HttpStatus.NOT_FOUND, 'PLAYER_NOT_FOUND', 'Player does not exist.');

    const objects = await this.database
      .selectFrom('map_objects')
      .select(['id', 'object_type', 'x', 'y', 'level', 'state'])
      .where('kingdom_id', '=', player.kingdom_id)
      .where('x', '>=', viewport.min_x)
      .where('x', '<=', viewport.max_x)
      .where('y', '>=', viewport.min_y)
      .where('y', '<=', viewport.max_y)
      .limit(1_000)
      .execute();

    return {
      data: { server_time: new Date().toISOString(), objects },
      meta: { count: objects.length },
    };
  }

  async dispatchMarch(
    playerId: string,
    idempotencyKey: string,
    request: DispatchMarchRequest,
  ): Promise<{ statusCode: number; response: Record<string, unknown> }> {
    return this.idempotency.execute(
      playerId,
      idempotencyKey,
      IdempotencyService.hashRequest(request),
      async (transaction) => {
        const city = await transaction
          .selectFrom('cities')
          .select(['id', 'kingdom_id', 'x', 'y'])
          .where('player_id', '=', playerId)
          .forUpdate()
          .executeTakeFirst();
        if (!city)
          throw new DomainError(HttpStatus.NOT_FOUND, 'CITY_NOT_FOUND', 'City does not exist.');

        const target = await transaction
          .selectFrom('map_objects')
          .select(['id', 'object_type', 'x', 'y'])
          .where('id', '=', request.target_id)
          .where('kingdom_id', '=', city.kingdom_id)
          .executeTakeFirst();
        if (!target)
          throw new DomainError(
            HttpStatus.NOT_FOUND,
            'TARGET_NOT_FOUND',
            'Map target does not exist.',
          );

        const activeMarches = await transaction
          .selectFrom('marches')
          .select(({ fn }) => fn.countAll<number>().as('count'))
          .where('player_id', '=', playerId)
          .where('status', 'in', ['outbound', 'resolving', 'returning'])
          .executeTakeFirstOrThrow();
        if (Number(activeMarches.count) >= 1) {
          throw new DomainError(
            HttpStatus.CONFLICT,
            'MARCH_SLOTS_FULL',
            'All march slots are occupied.',
          );
        }

        for (const requestedStack of request.troops) {
          const stack = await transaction
            .selectFrom('troop_stacks')
            .select('count')
            .where('city_id', '=', city.id)
            .where('troop_type', '=', requestedStack.troop_type)
            .where('tier', '=', requestedStack.tier)
            .forUpdate()
            .executeTakeFirst();
          if (!stack || BigInt(stack.count) < BigInt(requestedStack.count)) {
            throw new DomainError(
              HttpStatus.CONFLICT,
              'INSUFFICIENT_TROOPS',
              `Not enough ${requestedStack.troop_type} tier ${requestedStack.tier}.`,
            );
          }
          await transaction
            .updateTable('troop_stacks')
            .set({ count: (BigInt(stack.count) - BigInt(requestedStack.count)).toString() })
            .where('city_id', '=', city.id)
            .where('troop_type', '=', requestedStack.troop_type)
            .where('tier', '=', requestedStack.tier)
            .execute();
        }

        const marchId = uuidv7();
        const departAt = new Date();
        const travelDurationMs = calculateTravelDurationMs(city.x, city.y, target.x, target.y);
        const arriveAt = new Date(departAt.getTime() + travelDurationMs);
        const seed = Number.parseInt(marchId.slice(-8), 16) >>> 0;
        await transaction
          .insertInto('marches')
          .values({
            id: marchId,
            kingdom_id: city.kingdom_id,
            player_id: playerId,
            city_id: city.id,
            march_type: request.march_type,
            target_id: target.id,
            origin_x: city.x,
            origin_y: city.y,
            target_x: target.x,
            target_y: target.y,
            depart_at: departAt,
            arrive_at: arriveAt,
            status: 'outbound',
            payload: { troops: request.troops, travel_duration_ms: travelDurationMs, seed },
          })
          .execute();
        await transaction
          .insertInto('outbox_events')
          .values({
            event_id: uuidv7(),
            aggregate_type: 'player',
            aggregate_id: playerId,
            event_type: 'map.march.dispatched.v1',
            payload: { march_id: marchId, target_id: target.id, arrive_at: arriveAt.toISOString() },
            published_at: null,
          })
          .execute();

        return {
          statusCode: HttpStatus.CREATED,
          response: {
            data: {
              id: marchId,
              march_type: request.march_type,
              status: 'outbound',
              depart_at: departAt.toISOString(),
              arrive_at: arriveAt.toISOString(),
              target_id: target.id,
            },
          },
        };
      },
    );
  }
}
