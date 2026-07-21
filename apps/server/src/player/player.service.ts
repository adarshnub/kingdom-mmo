import { HttpStatus, Inject, Injectable } from '@nestjs/common';
import type { BootstrapResponse } from '@kingdom/contracts';
import { DatabaseService } from '../database/database.service.js';
import { DomainError } from '../platform/errors.js';

type ResourceRecord = BootstrapResponse['city']['resources'];

@Injectable()
export class PlayerService {
  constructor(@Inject(DatabaseService) private readonly database: DatabaseService) {}

  async bootstrap(playerId: string): Promise<BootstrapResponse> {
    const player = await this.database
      .selectFrom('players')
      .innerJoin('cities', 'cities.player_id', 'players.id')
      .select([
        'players.id as player_id',
        'players.kingdom_id',
        'players.display_name',
        'players.power',
        'cities.id as city_id',
        'cities.x',
        'cities.y',
        'cities.resources',
      ])
      .where('players.id', '=', playerId)
      .executeTakeFirst();
    if (!player) {
      throw new DomainError(HttpStatus.NOT_FOUND, 'PLAYER_NOT_FOUND', 'Player does not exist.');
    }

    const [buildings, queues, marches] = await Promise.all([
      this.database
        .selectFrom('buildings')
        .select(['id', 'slot_key', 'building_type', 'level'])
        .where('city_id', '=', player.city_id)
        .orderBy('slot_key')
        .execute(),
      this.database
        .selectFrom('queue_jobs')
        .select(['id', 'job_type', 'ref_id', 'start_at', 'finish_at', 'status'])
        .where('city_id', '=', player.city_id)
        .where('status', 'in', ['pending', 'processing'])
        .orderBy('finish_at')
        .execute(),
      this.database
        .selectFrom('marches')
        .select(['id', 'march_type', 'status', 'arrive_at'])
        .where('player_id', '=', playerId)
        .where('status', 'in', ['outbound', 'returning', 'resolving'])
        .orderBy('arrive_at')
        .execute(),
    ]);

    return {
      server_time: new Date().toISOString(),
      player: {
        id: player.player_id,
        kingdom_id: player.kingdom_id,
        display_name: player.display_name,
        power: String(player.power),
      },
      city: {
        id: player.city_id,
        x: player.x,
        y: player.y,
        resources: player.resources as ResourceRecord,
        buildings,
        active_queues: queues.map((queue) => ({
          ...queue,
          start_at: new Date(queue.start_at).toISOString(),
          finish_at: new Date(queue.finish_at).toISOString(),
        })),
      },
      active_marches: marches.map((march) => ({
        ...march,
        arrive_at: new Date(march.arrive_at).toISOString(),
      })),
    };
  }
}
