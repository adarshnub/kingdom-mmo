import 'reflect-metadata';
import { NestFactory } from '@nestjs/core';
import { Redis } from 'ioredis';
import { createServer } from 'node:http';
import { v7 as uuidv7 } from 'uuid';
import { WebSocket, WebSocketServer } from 'ws';
import { AuthService } from './auth/auth.service.js';
import { getEnvironment } from './config/environment.js';
import { RealtimeModule } from './realtime/realtime.module.js';

interface RoutedEvent {
  aggregate_type: string;
  aggregate_id: string;
  envelope: Record<string, unknown>;
}

async function main(): Promise<void> {
  const app = await NestFactory.createApplicationContext(RealtimeModule);
  const auth = app.get(AuthService);
  const environment = getEnvironment();
  const subscriber = new Redis(environment.REDIS_URL, { maxRetriesPerRequest: null });
  const clientsByPlayer = new Map<string, Set<WebSocket>>();
  const httpServer = createServer((request, response) => {
    if (request.url === '/health') {
      response.writeHead(200, { 'content-type': 'application/json' });
      response.end(JSON.stringify({ data: { status: 'ok' } }));
      return;
    }
    response.writeHead(404).end();
  });
  const server = new WebSocketServer({ noServer: true });
  httpServer.on('upgrade', (request, socket, head) => {
    const url = new URL(request.url ?? '/', `http://${request.headers.host ?? 'localhost'}`);
    if (url.pathname !== '/v1/realtime') {
      socket.destroy();
      return;
    }
    server.handleUpgrade(request, socket, head, (client) =>
      server.emit('connection', client, request),
    );
  });
  httpServer.listen(environment.REALTIME_PORT, '0.0.0.0');

  server.on('connection', (socket, request) => {
    void (async () => {
      try {
        const url = new URL(
          request.url ?? '/v1/realtime',
          `http://${request.headers.host ?? 'localhost'}`,
        );
        const token = url.searchParams.get('access_token');
        if (!token) throw new Error('Missing access token');
        const claims = await auth.verifyAccessToken(token);
        const playerSockets = clientsByPlayer.get(claims.playerId) ?? new Set<WebSocket>();
        playerSockets.add(socket);
        clientsByPlayer.set(claims.playerId, playerSockets);
        socket.send(
          JSON.stringify({
            event_id: uuidv7(),
            type: 'system.ready.v1',
            sequence: '0',
            occurred_at: new Date().toISOString(),
            payload: { player_id: claims.playerId },
          }),
        );
        socket.once('close', () => {
          playerSockets.delete(socket);
          if (playerSockets.size === 0) clientsByPlayer.delete(claims.playerId);
        });
      } catch {
        socket.close(1008, 'Unauthorized');
      }
    })();
  });

  await subscriber.subscribe('kingdom:events');
  subscriber.on('message', (_channel: string, raw: string) => {
    try {
      const event = JSON.parse(raw) as RoutedEvent;
      if (event.aggregate_type !== 'player') return;
      for (const socket of clientsByPlayer.get(event.aggregate_id) ?? []) {
        if (socket.readyState === WebSocket.OPEN) socket.send(JSON.stringify(event.envelope));
      }
    } catch (error: unknown) {
      console.error('Discarded invalid realtime event', error);
    }
  });

  const shutdown = (): void => {
    httpServer.close(() =>
      server.close(() => {
        void subscriber.quit().finally(() => app.close());
      }),
    );
  };
  process.once('SIGINT', shutdown);
  process.once('SIGTERM', shutdown);
}

void main();
