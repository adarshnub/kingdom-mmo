import { CanActivate, ExecutionContext, Inject, Injectable } from '@nestjs/common';
import type { FastifyRequest } from 'fastify';
import { AuthService, type AccessClaims } from './auth.service.js';
import { DomainError } from '../platform/errors.js';

export interface AuthenticatedRequest extends FastifyRequest {
  auth: AccessClaims;
}

@Injectable()
export class AuthGuard implements CanActivate {
  constructor(@Inject(AuthService) private readonly authService: AuthService) {}

  async canActivate(context: ExecutionContext): Promise<boolean> {
    const request = context.switchToHttp().getRequest<AuthenticatedRequest>();
    const authorization = request.headers.authorization;
    if (!authorization?.startsWith('Bearer ')) {
      throw new DomainError(401, 'MISSING_TOKEN', 'A bearer access token is required.');
    }
    request.auth = await this.authService.verifyAccessToken(authorization.slice('Bearer '.length));
    return true;
  }
}
