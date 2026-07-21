import { describe, expect, it } from 'vitest';
import {
  dispatchMarchRequestSchema,
  realtimeEnvelopeSchema,
  viewportQuerySchema,
} from './index.js';

describe('public contracts', () => {
  it('rejects viewports that exceed the server area budget', () => {
    expect(
      viewportQuerySchema.safeParse({ min_x: 0, max_x: 200, min_y: 0, max_y: 200 }).success,
    ).toBe(false);
  });

  it('accepts 64-bit troop counts as decimal strings', () => {
    expect(
      dispatchMarchRequestSchema.parse({
        target_id: '0198a7c9-5e62-7f54-a22d-c40f2f82ed48',
        march_type: 'attack',
        troops: [{ troop_type: 'infantry', tier: 1, count: '9007199254740993' }],
      }).troops[0]?.count,
    ).toBe('9007199254740993');
  });

  it('requires versioned realtime event names', () => {
    const base = {
      event_id: '0198a7c9-5e62-7f54-a22d-c40f2f82ed48',
      sequence: '1',
      occurred_at: '2026-07-21T08:30:00.000Z',
      payload: {},
    };
    expect(
      realtimeEnvelopeSchema.safeParse({ ...base, type: 'city.queue.completed.v1' }).success,
    ).toBe(true);
    expect(realtimeEnvelopeSchema.safeParse({ ...base, type: 'queue_completed' }).success).toBe(
      false,
    );
  });
});
