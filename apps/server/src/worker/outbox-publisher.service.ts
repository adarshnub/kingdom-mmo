import { Inject, Injectable, OnModuleDestroy } from '@nestjs/common';
import { Redis } from 'ioredis';
import { DatabaseService } from '../database/database.service.js';
import { ENVIRONMENT } from '../config/environment.module.js';
import type { Environment } from '../config/environment.js';

@Injectable()
export class OutboxPublisherService implements OnModuleDestroy {
  private readonly redis: Redis;

  constructor(
    @Inject(DatabaseService) private readonly database: DatabaseService,
    @Inject(ENVIRONMENT) environment: Environment,
  ) {
    this.redis = new Redis(environment.REDIS_URL, { lazyConnect: true, maxRetriesPerRequest: 1 });
  }

  async publishBatch(): Promise<number> {
    if (this.redis.status === 'wait') await this.redis.connect();
    const events = await this.database
      .selectFrom('outbox_events')
      .selectAll()
      .where('published_at', 'is', null)
      .orderBy('sequence')
      .limit(100)
      .execute();

    for (const event of events) {
      await this.redis.publish(
        'kingdom:events',
        JSON.stringify({
          aggregate_type: event.aggregate_type,
          aggregate_id: event.aggregate_id,
          envelope: {
            event_id: event.event_id,
            type: event.event_type,
            sequence: String(event.sequence),
            occurred_at: new Date(event.occurred_at).toISOString(),
            payload: event.payload,
          },
        }),
      );
      await this.database
        .updateTable('outbox_events')
        .set({ published_at: new Date() })
        .where('event_id', '=', event.event_id)
        .where('published_at', 'is', null)
        .execute();
    }
    return events.length;
  }

  async onModuleDestroy(): Promise<void> {
    await this.redis.quit().catch(() => undefined);
  }
}
