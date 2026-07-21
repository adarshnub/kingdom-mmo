import 'reflect-metadata';
import { NestFactory } from '@nestjs/core';
import { getEnvironment } from './config/environment.js';
import { OutboxPublisherService } from './worker/outbox-publisher.service.js';
import { WorkerModule } from './worker/worker.module.js';
import { WorkerService } from './worker/worker.service.js';

async function main(): Promise<void> {
  const app = await NestFactory.createApplicationContext(WorkerModule);
  const worker = app.get(WorkerService);
  const outbox = app.get(OutboxPublisherService);
  const environment = getEnvironment();
  let stopping = false;

  const stop = (): void => {
    stopping = true;
  };
  process.once('SIGINT', stop);
  process.once('SIGTERM', stop);

  while (!stopping) {
    try {
      const processed = await worker.runIteration();
      const published = await outbox.publishBatch();
      if (processed === 0 && published === 0) {
        await new Promise((resolve) => setTimeout(resolve, environment.WORKER_POLL_INTERVAL_MS));
      }
    } catch (error: unknown) {
      console.error('Worker iteration failed', error);
      await new Promise((resolve) => setTimeout(resolve, environment.WORKER_POLL_INTERVAL_MS));
    }
  }

  await app.close();
}

void main();
