import { Inject, Injectable, OnModuleDestroy } from '@nestjs/common';
import { Kysely, PostgresDialect, sql } from 'kysely';
import { Pool } from 'pg';
import { ENVIRONMENT } from '../config/environment.module.js';
import type { Environment } from '../config/environment.js';
import type { Database } from './types.js';

@Injectable()
export class DatabaseService extends Kysely<Database> implements OnModuleDestroy {
  constructor(@Inject(ENVIRONMENT) environment: Environment) {
    const pool = new Pool({
      connectionString: environment.DATABASE_URL,
      max: environment.NODE_ENV === 'production' ? 20 : 10,
      idleTimeoutMillis: 30_000,
      connectionTimeoutMillis: 5_000,
    });
    super({ dialect: new PostgresDialect({ pool }) });
  }

  async checkConnection(): Promise<void> {
    await sql`select 1`.execute(this);
  }

  async onModuleDestroy(): Promise<void> {
    await this.destroy();
  }
}
