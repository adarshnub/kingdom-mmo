import { HttpStatus, Inject, Injectable } from '@nestjs/common';
import { createHash } from 'node:crypto';
import type { Transaction } from 'kysely';
import { DatabaseService } from '../database/database.service.js';
import type { Database } from '../database/types.js';
import { DomainError } from './errors.js';

export interface IdempotentResult<T extends Record<string, unknown>> {
  statusCode: number;
  response: T;
}

@Injectable()
export class IdempotencyService {
  constructor(@Inject(DatabaseService) private readonly database: DatabaseService) {}

  static hashRequest(value: unknown): string {
    return createHash('sha256').update(JSON.stringify(value)).digest('hex');
  }

  async execute<T extends Record<string, unknown>>(
    playerId: string,
    key: string,
    requestHash: string,
    handler: (transaction: Transaction<Database>) => Promise<IdempotentResult<T>>,
  ): Promise<IdempotentResult<T>> {
    return this.database.transaction().execute(async (transaction) => {
      const existing = await transaction
        .selectFrom('idempotency_keys')
        .select(['request_hash', 'status_code', 'response'])
        .where('player_id', '=', playerId)
        .where('key', '=', key)
        .forUpdate()
        .executeTakeFirst();

      if (existing) {
        if (existing.request_hash !== requestHash) {
          throw new DomainError(
            HttpStatus.CONFLICT,
            'IDEMPOTENCY_KEY_REUSED',
            'The idempotency key was already used with a different request.',
          );
        }
        return { statusCode: existing.status_code, response: existing.response as T };
      }

      const result = await handler(transaction);
      await transaction
        .insertInto('idempotency_keys')
        .values({
          player_id: playerId,
          key,
          request_hash: requestHash,
          status_code: result.statusCode,
          response: result.response,
          expires_at: new Date(Date.now() + 24 * 60 * 60 * 1_000),
        })
        .execute();
      return result;
    });
  }
}
