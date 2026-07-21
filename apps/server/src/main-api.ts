import 'reflect-metadata';
import { NestFactory } from '@nestjs/core';
import { FastifyAdapter, type NestFastifyApplication } from '@nestjs/platform-fastify';
import { AppModule } from './app.module.js';
import { getEnvironment } from './config/environment.js';
import { ProblemDetailsFilter } from './platform/problem-details.filter.js';

async function bootstrap(): Promise<void> {
  const environment = getEnvironment();
  const app = await NestFactory.create<NestFastifyApplication>(
    AppModule,
    new FastifyAdapter({ logger: { level: environment.LOG_LEVEL } }),
  );
  app.enableShutdownHooks();
  app.enableCors({
    origin: environment.CORS_ORIGINS.split(',').map((origin) => origin.trim()),
    credentials: false,
  });
  app.useGlobalFilters(new ProblemDetailsFilter());
  await app.listen(environment.API_PORT, '0.0.0.0');
}

void bootstrap();
