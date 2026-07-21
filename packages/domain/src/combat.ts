export interface ArmyInput {
  troops: bigint;
  attackBasisPoints: number;
  defenseBasisPoints: number;
}

export interface BattleInput {
  seed: number;
  attacker: ArmyInput;
  defender: ArmyInput;
}

export interface BattleResult {
  winner: 'attacker' | 'defender';
  attackerCasualties: bigint;
  defenderCasualties: bigint;
  seed: number;
  rulesVersion: 1;
}

function validateArmy(army: ArmyInput): void {
  if (army.troops <= 0n) throw new RangeError('Army troops must be positive');
  if (!Number.isInteger(army.attackBasisPoints) || army.attackBasisPoints < 1) {
    throw new RangeError('Attack basis points must be a positive integer');
  }
  if (!Number.isInteger(army.defenseBasisPoints) || army.defenseBasisPoints < 1) {
    throw new RangeError('Defense basis points must be a positive integer');
  }
}

function nextRandom(state: number): number {
  return (Math.imul(state, 1_664_525) + 1_013_904_223) >>> 0;
}

function clamp(value: bigint, minimum: bigint, maximum: bigint): bigint {
  return value < minimum ? minimum : value > maximum ? maximum : value;
}

export function resolveBattle(input: BattleInput): BattleResult {
  validateArmy(input.attacker);
  validateArmy(input.defender);
  if (!Number.isInteger(input.seed) || input.seed < 0 || input.seed > 0xffffffff) {
    throw new RangeError('Seed must be an unsigned 32-bit integer');
  }

  const attackerRoll = BigInt((nextRandom(input.seed) % 2_001) + 9_000);
  const defenderRoll = BigInt((nextRandom(nextRandom(input.seed)) % 2_001) + 9_000);
  const attackerPower =
    (input.attacker.troops * BigInt(input.attacker.attackBasisPoints) * attackerRoll) /
    100_000_000n;
  const defenderPower =
    (input.defender.troops * BigInt(input.defender.defenseBasisPoints) * defenderRoll) /
    100_000_000n;
  const totalPower = attackerPower + defenderPower;

  const attackerLoss = clamp(
    totalPower === 0n ? 1n : (input.attacker.troops * defenderPower) / totalPower,
    1n,
    input.attacker.troops,
  );
  const defenderLoss = clamp(
    totalPower === 0n ? 1n : (input.defender.troops * attackerPower) / totalPower,
    1n,
    input.defender.troops,
  );

  return {
    winner: attackerPower >= defenderPower ? 'attacker' : 'defender',
    attackerCasualties: attackerLoss,
    defenderCasualties: defenderLoss,
    seed: input.seed,
    rulesVersion: 1,
  };
}
