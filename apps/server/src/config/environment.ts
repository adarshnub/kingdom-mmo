import { config as loadDotEnv } from 'dotenv';
import { z } from 'zod';

loadDotEnv({ path: process.env.DOTENV_CONFIG_PATH ?? '../../.env' });
loadDotEnv();

const environmentSchema = z.object({
  NODE_ENV: z.enum(['development', 'test', 'production']).default('development'),
  API_PORT: z.coerce.number().int().min(1).max(65_535).default(3000),
  REALTIME_PORT: z.coerce.number().int().min(1).max(65_535).default(3001),
  DATABASE_URL: z.string().url().startsWith('postgres'),
  REDIS_URL: z.string().url().startsWith('redis'),
  JWT_SECRET: z.string().min(32),
  JWT_ISSUER: z.string().min(1).default('kingdom-local'),
  JWT_AUDIENCE: z.string().min(1).default('kingdom-client'),
  ACCESS_TOKEN_TTL_SECONDS: z.coerce.number().int().positive().default(900),
  REFRESH_TOKEN_TTL_SECONDS: z.coerce.number().int().positive().default(2_592_000),
  WORKER_POLL_INTERVAL_MS: z.coerce.number().int().min(100).max(60_000).default(1_000),
  CORS_ORIGINS: z.string().default('http://localhost:3002'),
  LOG_LEVEL: z.enum(['debug', 'info', 'warn', 'error']).default('info'),
});

export type Environment = z.infer<typeof environmentSchema>;

let cachedEnvironment: Environment | undefined;

export function getEnvironment(): Environment {
  cachedEnvironment ??= environmentSchema.parse(process.env);
  return cachedEnvironment;
}
