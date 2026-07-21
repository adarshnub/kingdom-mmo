import { Controller, Get, Inject, UseGuards } from '@nestjs/common';
import { AuthGuard } from '../auth/auth.guard.js';
import { CurrentPlayer } from '../auth/current-player.decorator.js';
import { PlayerService } from './player.service.js';

@Controller('v1')
@UseGuards(AuthGuard)
export class PlayerController {
  constructor(@Inject(PlayerService) private readonly playerService: PlayerService) {}

  @Get('bootstrap')
  async bootstrap(@CurrentPlayer() playerId: string): Promise<Record<string, unknown>> {
    return { data: await this.playerService.bootstrap(playerId) };
  }
}
