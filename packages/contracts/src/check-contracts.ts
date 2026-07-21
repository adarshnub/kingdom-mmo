import { readFile } from 'node:fs/promises';
import { resolve } from 'node:path';
import { realtimeEnvelopeSchema } from './index.js';

const requiredOpenApiPaths = [
  '/health/live',
  '/health/ready',
  '/v1/auth/guest',
  '/v1/bootstrap',
  '/v1/city/build/start',
  '/v1/map/viewport',
  '/v1/map/march/dispatch',
];

async function main(): Promise<void> {
  const openApiPath = resolve(import.meta.dirname, '../openapi/kingdom-v1.yaml');
  const openApi = await readFile(openApiPath, 'utf8');

  for (const path of requiredOpenApiPaths) {
    if (!openApi.includes(`  ${path}:`)) {
      throw new Error(`OpenAPI contract is missing ${path}`);
    }
  }

  realtimeEnvelopeSchema.parse({
    event_id: '0198a7c9-5e62-7f54-a22d-c40f2f82ed48',
    type: 'system.ready.v1',
    sequence: '1',
    occurred_at: '2026-07-21T08:30:00.000Z',
    payload: {},
  });
}

await main();
