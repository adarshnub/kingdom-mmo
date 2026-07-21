import { Module } from '@nestjs/common';
import { AuthModule } from '../auth/auth.module.js';
import { PlatformModule } from '../platform/platform.module.js';
import { MapController } from './map.controller.js';
import { MapService } from './map.service.js';

@Module({
  imports: [AuthModule, PlatformModule],
  controllers: [MapController],
  providers: [MapService],
})
export class MapModule {}
