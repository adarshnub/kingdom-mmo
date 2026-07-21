import { v7 as uuidv7 } from 'uuid';
import { WebSocket, type RawData } from 'ws';
import { guestAuthResponseSchema, realtimeEnvelopeSchema } from '@kingdom/contracts';

const apiBaseUrl = process.env.API_BASE_URL ?? 'http://127.0.0.1:3000';
const realtimeBaseUrl = process.env.REALTIME_BASE_URL ?? 'ws://127.0.0.1:3001/v1/realtime';

const installationId = uuidv7();
const authResponse = await fetch(`${apiBaseUrl}/v1/auth/guest`, {
  method: 'POST',
  headers: { 'content-type': 'application/json' },
  body: JSON.stringify({
    installation_id: installationId,
    display_name: `RT-${installationId.slice(-8)}`,
  }),
});
if (!authResponse.ok) throw new Error(`Guest authentication failed with ${authResponse.status}`);
const authEnvelope = (await authResponse.json()) as { data: unknown };
const auth = guestAuthResponseSchema.parse(authEnvelope.data);

await new Promise<void>((resolve, reject) => {
  const timeout = setTimeout(() => reject(new Error('Realtime ready event timed out')), 5_000);
  const socket = new WebSocket(
    `${realtimeBaseUrl}?access_token=${encodeURIComponent(auth.access_token)}`,
  );
  socket.once('message', (raw: RawData) => {
    try {
      const serialized = Array.isArray(raw)
        ? Buffer.concat(raw).toString('utf8')
        : Buffer.from(raw as ArrayBuffer).toString('utf8');
      const event = realtimeEnvelopeSchema.parse(JSON.parse(serialized));
      if (event.type !== 'system.ready.v1') throw new Error(`Unexpected event ${event.type}`);
      clearTimeout(timeout);
      socket.close();
      resolve();
    } catch (error) {
      reject(error instanceof Error ? error : new Error(String(error)));
    }
  });
  socket.once('error', reject);
});

console.log('Realtime authentication and ready envelope passed.');
