import { describe, expect, it } from 'vitest';
import { quoteBuildingUpgrade } from './building.js';
import { resolveBattle } from './combat.js';
import { calculateTravelDurationMs } from './march.js';

describe('domain rules', () => {
  it('quotes building upgrades with integer costs and durations', () => {
    expect(quoteBuildingUpgrade(2)).toEqual({
      targetLevel: 3,
      durationMs: 540_000,
      foodCost: 9_000n,
      woodCost: 9_000n,
    });
  });

  it('uses deterministic Manhattan travel in the vertical slice', () => {
    expect(calculateTravelDurationMs(10, 10, 13, 14)).toBe(7_000);
    expect(calculateTravelDurationMs(10, 10, 10, 10)).toBe(1_000);
  });

  it('produces deterministic bounded casualties', () => {
    const input = {
      seed: 42,
      attacker: { troops: 1_000n, attackBasisPoints: 11_000, defenseBasisPoints: 10_000 },
      defender: { troops: 900n, attackBasisPoints: 10_000, defenseBasisPoints: 10_500 },
    };
    const first = resolveBattle(input);
    const second = resolveBattle(input);
    expect(first).toEqual(second);
    expect(first.attackerCasualties).toBeGreaterThan(0n);
    expect(first.attackerCasualties).toBeLessThanOrEqual(1_000n);
    expect(first.defenderCasualties).toBeLessThanOrEqual(900n);
  });
});
