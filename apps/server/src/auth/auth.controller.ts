import { Body, Controller, HttpCode, HttpStatus, Inject, Post } from '@nestjs/common';
import { guestAuthRequestSchema } from '@kingdom/contracts';
import { AuthService } from './auth.service.js';
import { parseInput } from '../platform/errors.js';

@Controller('v1/auth')
export class AuthController {
  constructor(@Inject(AuthService) private readonly authService: AuthService) {}

  @Post('guest')
  @HttpCode(HttpStatus.CREATED)
  async guest(@Body() body: unknown): Promise<Record<string, unknown>> {
    return {
      data: await this.authService.authenticateGuest(parseInput(guestAuthRequestSchema, body)),
    };
  }
}
