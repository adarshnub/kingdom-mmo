export function calculateTravelDurationMs(
  originX: number,
  originY: number,
  targetX: number,
  targetY: number,
): number {
  for (const coordinate of [originX, originY, targetX, targetY]) {
    if (!Number.isInteger(coordinate) || coordinate < 0 || coordinate > 1199) {
      throw new RangeError('Map coordinates must be integers from 0 through 1199');
    }
  }

  const distance = Math.abs(targetX - originX) + Math.abs(targetY - originY);
  return Math.max(1_000, distance * 1_000);
}
