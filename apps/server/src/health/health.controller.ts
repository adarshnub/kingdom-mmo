import { Controller, Get, Inject, ServiceUnavailableException } from '@nestjs/common';
import { DatabaseService } from '../database/database.service.js';

@Controller('health')
export class HealthController {
  constructor(@Inject(DatabaseService) private readonly database: DatabaseService) {}

  @Get('live')
  liveness(): Record<string, unknown> {
    return { data: { status: 'ok' } };
  }

  @Get('ready')
  async readiness(): Promise<Record<string, unknown>> {
    try {
      await this.database.checkConnection();
      return { data: { status: 'ready', database: 'ready' } };
    } catch {
      throw new ServiceUnavailableException('Database is unavailable');
    }
  }
}
