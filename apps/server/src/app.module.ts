import { Module } from '@nestjs/common';
import { AuthModule } from './auth/auth.module.js';
import { CityModule } from './city/city.module.js';
import { DatabaseModule } from './database/database.module.js';
import { EnvironmentModule } from './config/environment.module.js';
import { HealthModule } from './health/health.module.js';
import { MapModule } from './map/map.module.js';
import { PlayerModule } from './player/player.module.js';

@Module({
  imports: [
    EnvironmentModule,
    DatabaseModule,
    AuthModule,
    HealthModule,
    PlayerModule,
    CityModule,
    MapModule,
  ],
})
export class AppModule {}
