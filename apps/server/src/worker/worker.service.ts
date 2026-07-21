import { Inject, Injectable } from '@nestjs/common';
import { resolveBattle } from '@kingdom/domain';
import { sql, type Transaction } from 'kysely';
import { v7 as uuidv7 } from 'uuid';
import { z } from 'zod';
import { DatabaseService } from '../database/database.service.js';
import type { Database } from '../database/types.js';

const marchPayloadSchema = z.object({
  troops: z.array(
    z.object({
      troop_type: z.enum(['infantry', 'cavalry', 'archer', 'siege']),
      tier: z.number().int(),
      count: z.string(),
    }),
  ),
  travel_duration_ms: z.number().int().positive(),
  seed: z.number().int().nonnegative(),
  attacker_casualties: z.string().optional(),
  battle_report_id: z.string().uuid().optional(),
});

@Injectable()
export class WorkerService {
  constructor(@Inject(DatabaseService) private readonly database: DatabaseService) {}

  async runIteration(): Promise<number> {
    let processed = 0;
    for (let index = 0; index < 20; index += 1) {
      const didProcess = await this.processOneQueueJob();
      if (!didProcess) break;
      processed += 1;
    }
    for (let index = 0; index < 20; index += 1) {
      const didProcess = await this.processOneMarch();
      if (!didProcess) break;
      processed += 1;
    }
    return processed;
  }

  private async processOneQueueJob(): Promise<boolean> {
    return this.database.transaction().execute(async (transaction) => {
      const job = await transaction
        .selectFrom('queue_jobs')
        .selectAll()
        .where('status', 'in', ['pending', 'processing'])
        .where('finish_at', '<=', new Date())
        .where((expression) =>
          expression.or([
            expression('lease_until', 'is', null),
            expression('lease_until', '<', new Date()),
          ]),
        )
        .orderBy('finish_at')
        .forUpdate()
        .skipLocked()
        .executeTakeFirst();
      if (!job) return false;

      if (job.job_type === 'building_upgrade' && job.ref_id) {
        const targetLevel = Number(job.payload['target_level']);
        if (!Number.isInteger(targetLevel))
          throw new Error(`Queue ${job.id} has invalid target_level`);
        await transaction
          .updateTable('buildings')
          .set({ level: targetLevel, version: sql<number>`version + 1` })
          .where('id', '=', job.ref_id)
          .executeTakeFirstOrThrow();
      }

      await transaction
        .updateTable('queue_jobs')
        .set({ status: 'completed', lease_until: null })
        .where('id', '=', job.id)
        .execute();
      const owner = await transaction
        .selectFrom('cities')
        .select('player_id')
        .where('id', '=', job.city_id)
        .executeTakeFirstOrThrow();
      await this.addOutboxEvent(transaction, owner.player_id, 'city.queue.completed.v1', {
        queue_id: job.id,
        ref_id: job.ref_id,
      });
      return true;
    });
  }

  private async processOneMarch(): Promise<boolean> {
    return this.database.transaction().execute(async (transaction) => {
      const march = await transaction
        .selectFrom('marches')
        .selectAll()
        .where('status', 'in', ['outbound', 'returning'])
        .where('arrive_at', '<=', new Date())
        .orderBy('arrive_at')
        .forUpdate()
        .skipLocked()
        .executeTakeFirst();
      if (!march) return false;

      const payload = marchPayloadSchema.parse(march.payload);
      if (march.status === 'returning') {
        await this.restoreSurvivors(transaction, march.city_id, payload);
        await transaction
          .updateTable('marches')
          .set({ status: 'completed' })
          .where('id', '=', march.id)
          .execute();
        await this.addOutboxEvent(transaction, march.player_id, 'map.march.completed.v1', {
          march_id: march.id,
        });
        return true;
      }

      let updatedPayload = payload;
      if (march.march_type === 'attack') {
        const target = await transaction
          .selectFrom('map_objects')
          .select(['object_type', 'state'])
          .where('id', '=', march.target_id)
          .executeTakeFirstOrThrow();
        if (target.object_type === 'monster') {
          const attackerTroops = payload.troops.reduce(
            (total, stack) => total + BigInt(stack.count),
            0n,
          );
          const defenderValue = target.state['defender_troops'];
          const defenderTroops =
            typeof defenderValue === 'string' || typeof defenderValue === 'number'
              ? BigInt(defenderValue)
              : 100n;
          const result = resolveBattle({
            seed: payload.seed,
            attacker: {
              troops: attackerTroops,
              attackBasisPoints: 10_000,
              defenseBasisPoints: 10_000,
            },
            defender: {
              troops: defenderTroops,
              attackBasisPoints: 10_000,
              defenseBasisPoints: 10_000,
            },
          });
          const reportId = uuidv7();
          await transaction
            .insertInto('battle_reports')
            .values({
              id: reportId,
              kingdom_id: march.kingdom_id,
              owner_player_id: march.player_id,
              source_march_id: march.id,
              rules_version: result.rulesVersion,
              seed: result.seed,
              summary: {
                winner: result.winner,
                attacker_casualties: result.attackerCasualties.toString(),
                defender_casualties: result.defenderCasualties.toString(),
              },
              details: { target_id: march.target_id },
            })
            .execute();
          updatedPayload = {
            ...payload,
            attacker_casualties: result.attackerCasualties.toString(),
            battle_report_id: reportId,
          };
          await this.addOutboxEvent(transaction, march.player_id, 'battle.report.created.v1', {
            march_id: march.id,
            report_id: reportId,
            winner: result.winner,
          });
        }
      }

      await transaction
        .updateTable('marches')
        .set({
          status: 'returning',
          arrive_at: new Date(Date.now() + payload.travel_duration_ms),
          payload: updatedPayload,
        })
        .where('id', '=', march.id)
        .execute();
      await this.addOutboxEvent(transaction, march.player_id, 'map.march.returning.v1', {
        march_id: march.id,
      });
      return true;
    });
  }

  private async restoreSurvivors(
    transaction: Transaction<Database>,
    cityId: string,
    payload: z.infer<typeof marchPayloadSchema>,
  ): Promise<void> {
    let casualtiesRemaining = BigInt(payload.attacker_casualties ?? '0');
    for (const stack of payload.troops) {
      const committed = BigInt(stack.count);
      const casualties = casualtiesRemaining > committed ? committed : casualtiesRemaining;
      casualtiesRemaining -= casualties;
      const survivors = committed - casualties;
      if (survivors === 0n) continue;
      await transaction
        .insertInto('troop_stacks')
        .values({
          city_id: cityId,
          troop_type: stack.troop_type,
          tier: stack.tier,
          count: survivors.toString(),
        })
        .onConflict((conflict) =>
          conflict.columns(['city_id', 'troop_type', 'tier']).doUpdateSet({
            count: sql<string>`troop_stacks.count + excluded.count`,
          }),
        )
        .execute();
    }
  }

  private async addOutboxEvent(
    transaction: Transaction<Database>,
    playerId: string,
    eventType: string,
    payload: Record<string, unknown>,
  ): Promise<void> {
    await transaction
      .insertInto('outbox_events')
      .values({
        event_id: uuidv7(),
        aggregate_type: 'player',
        aggregate_id: playerId,
        event_type: eventType,
        payload,
        published_at: null,
      })
      .execute();
  }
}
