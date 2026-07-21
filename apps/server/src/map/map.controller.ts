import {
  Body,
  Controller,
  Get,
  Headers,
  HttpCode,
  Inject,
  Post,
  Query,
  UseGuards,
} from '@nestjs/common';
import { dispatchMarchRequestSchema, uuidSchema, viewportQuerySchema } from '@kingdom/contracts';
import { AuthGuard } from '../auth/auth.guard.js';
import { CurrentPlayer } from '../auth/current-player.decorator.js';
import { parseInput } from '../platform/errors.js';
import { MapService } from './map.service.js';

@Controller('v1/map')
@UseGuards(AuthGuard)
export class MapController {
  constructor(@Inject(MapService) private readonly mapService: MapService) {}

  @Get('viewport')
  async viewport(
    @CurrentPlayer() playerId: string,
    @Query() query: unknown,
  ): Promise<Record<string, unknown>> {
    return this.mapService.getViewport(playerId, parseInput(viewportQuerySchema, query));
  }

  @Post('march/dispatch')
  @HttpCode(201)
  async dispatch(
    @CurrentPlayer() playerId: string,
    @Headers('idempotency-key') rawIdempotencyKey: unknown,
    @Body() body: unknown,
  ): Promise<Record<string, unknown>> {
    const idempotencyKey = parseInput(uuidSchema, rawIdempotencyKey);
    const request = parseInput(dispatchMarchRequestSchema, body);
    return (await this.mapService.dispatchMarch(playerId, idempotencyKey, request)).response;
  }
}
