import { describe, expect, it } from 'vitest';
import { IdempotencyService } from './idempotency.service.js';

describe('IdempotencyService', () => {
  it('creates stable request hashes and distinguishes changed commands', () => {
    const first = IdempotencyService.hashRequest({ building_id: 'a' });
    expect(IdempotencyService.hashRequest({ building_id: 'a' })).toBe(first);
    expect(IdempotencyService.hashRequest({ building_id: 'b' })).not.toBe(first);
  });
});
