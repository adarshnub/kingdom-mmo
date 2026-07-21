export interface BuildingUpgradeQuote {
  targetLevel: number;
  durationMs: number;
  foodCost: bigint;
  woodCost: bigint;
}

export function quoteBuildingUpgrade(currentLevel: number): BuildingUpgradeQuote {
  if (!Number.isInteger(currentLevel) || currentLevel < 0 || currentLevel >= 40) {
    throw new RangeError('Building level must be an integer from 0 through 39');
  }

  const targetLevel = currentLevel + 1;
  const levelSquared = BigInt(targetLevel * targetLevel);

  return {
    targetLevel,
    durationMs: 60_000 * targetLevel * targetLevel,
    foodCost: 1_000n * levelSquared,
    woodCost: 1_000n * levelSquared,
  };
}
