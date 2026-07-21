import { Module } from '@nestjs/common';
import { AuthModule } from '../auth/auth.module.js';
import { PlatformModule } from '../platform/platform.module.js';
import { CityController } from './city.controller.js';
import { CityService } from './city.service.js';

@Module({
  imports: [AuthModule, PlatformModule],
  controllers: [CityController],
  providers: [CityService],
})
export class CityModule {}
