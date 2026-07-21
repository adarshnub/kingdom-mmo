import { Body, Controller, Headers, HttpCode, Inject, Post, UseGuards } from '@nestjs/common';
import { speedUpBuildRequestSchema, startBuildRequestSchema, uuidSchema } from '@kingdom/contracts';
import { AuthGuard } from '../auth/auth.guard.js';
import { CurrentPlayer } from '../auth/current-player.decorator.js';
import { parseInput } from '../platform/errors.js';
import { CityService } from './city.service.js';

@Controller('v1/city')
@UseGuards(AuthGuard)
export class CityController {
  constructor(@Inject(CityService) private readonly cityService: CityService) {}

  @Post('build/start')
  @HttpCode(201)
  async startBuild(
    @CurrentPlayer() playerId: string,
    @Headers('idempotency-key') rawIdempotencyKey: unknown,
    @Body() body: unknown,
  ): Promise<Record<string, unknown>> {
    const idempotencyKey = parseInput(uuidSchema, rawIdempotencyKey);
    const request = parseInput(startBuildRequestSchema, body);
    const result = await this.cityService.startBuildingUpgrade(
      playerId,
      idempotencyKey,
      request.building_id,
    );
    return result.response;
  }

  @Post('build/speed-up')
  @HttpCode(200)
  async speedUpBuild(
    @CurrentPlayer() playerId: string,
    @Headers('idempotency-key') rawIdempotencyKey: unknown,
    @Body() body: unknown,
  ): Promise<Record<string, unknown>> {
    const idempotencyKey = parseInput(uuidSchema, rawIdempotencyKey);
    const request = parseInput(speedUpBuildRequestSchema, body);
    const result = await this.cityService.speedUpBuildingUpgrade(
      playerId,
      idempotencyKey,
      request.building_id,
    );
    return result.response;
  }
}
