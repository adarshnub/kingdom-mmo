import { Module } from '@nestjs/common';
import { AuthModule } from '../auth/auth.module.js';
import { DatabaseModule } from '../database/database.module.js';
import { EnvironmentModule } from '../config/environment.module.js';

@Module({ imports: [EnvironmentModule, DatabaseModule, AuthModule] })
export class RealtimeModule {}
