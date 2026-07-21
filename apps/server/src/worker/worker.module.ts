import { Module } from '@nestjs/common';
import { DatabaseModule } from '../database/database.module.js';
import { EnvironmentModule } from '../config/environment.module.js';
import { OutboxPublisherService } from './outbox-publisher.service.js';
import { WorkerService } from './worker.service.js';

@Module({
  imports: [EnvironmentModule, DatabaseModule],
  providers: [WorkerService, OutboxPublisherService],
})
export class WorkerModule {}
