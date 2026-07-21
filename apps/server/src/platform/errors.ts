import { HttpStatus } from '@nestjs/common';
import type { ZodType } from 'zod';

export class DomainError extends Error {
  constructor(
    public readonly status: number,
    public readonly code: string,
    message: string,
  ) {
    super(message);
  }
}

export function parseInput<T>(schema: ZodType<T>, value: unknown): T {
  const result = schema.safeParse(value);
  if (!result.success) {
    throw new DomainError(
      HttpStatus.BAD_REQUEST,
      'INVALID_REQUEST',
      result.error.issues.map((issue) => issue.message).join('; '),
    );
  }
  return result.data;
}
