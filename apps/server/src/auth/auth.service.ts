import { HttpStatus, Inject, Injectable } from '@nestjs/common';
import { createHash, randomBytes } from 'node:crypto';
import { SignJWT, jwtVerify } from 'jose';
import { v7 as uuidv7 } from 'uuid';
import type { GuestAuthRequest, GuestAuthResponse } from '@kingdom/contracts';
import { DatabaseService } from '../database/database.service.js';
import { ENVIRONMENT } from '../config/environment.module.js';
import type { Environment } from '../config/environment.js';
import { DomainError } from '../platform/errors.js';

export interface AccessClaims {
  accountId: string;
  playerId: string;
  sessionId: string;
}

function hashToken(token: string): string {
  return createHash('sha256').update(token).digest('hex');
}

@Injectable()
export class AuthService {
  private readonly signingKey: Uint8Array;

  constructor(
    @Inject(DatabaseService) private readonly database: DatabaseService,
    @Inject(ENVIRONMENT) private readonly environment: Environment,
  ) {
    this.signingKey = new TextEncoder().encode(environment.JWT_SECRET);
  }

  async authenticateGuest(request: GuestAuthRequest): Promise<GuestAuthResponse> {
    let identity: { account_id: string; account_status: string; player_id: string };
    try {
      identity = await this.database.transaction().execute(async (transaction) => {
        const existing = await transaction
          .selectFrom('accounts')
          .innerJoin('players', 'players.account_id', 'accounts.id')
          .select([
            'accounts.id as account_id',
            'accounts.status as account_status',
            'players.id as player_id',
          ])
          .where('accounts.installation_id', '=', request.installation_id)
          .executeTakeFirst();

        if (existing) {
          if (existing.account_status !== 'active') {
            throw new DomainError(
              HttpStatus.FORBIDDEN,
              'ACCOUNT_DISABLED',
              'This account is disabled.',
            );
          }
          return existing;
        }

        const kingdom = await transaction
          .selectFrom('kingdoms')
          .select(['id'])
          .where('code', '=', 'K1')
          .where('status', '=', 'open')
          .executeTakeFirst();
        if (!kingdom) {
          throw new DomainError(
            HttpStatus.SERVICE_UNAVAILABLE,
            'KINGDOM_UNAVAILABLE',
            'Kingdom 1 has not been seeded.',
          );
        }

        const accountId = uuidv7();
        const playerId = uuidv7();
        const cityId = uuidv7();
        const coordinateSeed = Number.parseInt(accountId.slice(-6), 16);
        const x = 50 + (coordinateSeed % 1_100);
        const y = 50 + (Math.floor(coordinateSeed / 1_100) % 1_100);

        await transaction
          .insertInto('accounts')
          .values({ id: accountId, installation_id: request.installation_id, status: 'active' })
          .execute();
        await transaction
          .insertInto('players')
          .values({
            id: playerId,
            account_id: accountId,
            kingdom_id: kingdom.id,
            display_name: request.display_name ?? `Lord-${playerId.slice(-6)}`,
          })
          .execute();
        await transaction
          .insertInto('cities')
          .values({
            id: cityId,
            player_id: playerId,
            kingdom_id: kingdom.id,
            x,
            y,
            resources: {
              food: '100000',
              wood: '100000',
              iron: '10000',
              silver: '1000',
              gold: '500',
            },
            shield_until: new Date(Date.now() + 72 * 60 * 60 * 1_000),
          })
          .execute();
        await transaction
          .insertInto('buildings')
          .values([
            {
              id: uuidv7(),
              city_id: cityId,
              slot_key: 'palace',
              building_type: 'palace',
              level: 1,
            },
            { id: uuidv7(), city_id: cityId, slot_key: 'farm_1', building_type: 'farm', level: 1 },
            {
              id: uuidv7(),
              city_id: cityId,
              slot_key: 'barracks',
              building_type: 'barracks',
              level: 1,
            },
          ])
          .execute();
        await transaction
          .insertInto('troop_stacks')
          .values({ city_id: cityId, troop_type: 'infantry', tier: 1, count: '1000' })
          .execute();

        return { account_id: accountId, account_status: 'active', player_id: playerId };
      });
    } catch (error: unknown) {
      if (
        typeof error === 'object' &&
        error !== null &&
        'code' in error &&
        error.code === '23505' &&
        'constraint' in error &&
        error.constraint === 'players_kingdom_name_idx'
      ) {
        throw new DomainError(
          HttpStatus.CONFLICT,
          'DISPLAY_NAME_TAKEN',
          'Display name is already in use.',
        );
      }
      throw error;
    }

    const sessionId = uuidv7();
    const refreshToken = randomBytes(32).toString('base64url');
    const accessExpiresAt = new Date(
      Date.now() + this.environment.ACCESS_TOKEN_TTL_SECONDS * 1_000,
    );
    await this.database
      .insertInto('sessions')
      .values({
        id: sessionId,
        account_id: identity.account_id,
        refresh_token_hash: hashToken(refreshToken),
        expires_at: new Date(Date.now() + this.environment.REFRESH_TOKEN_TTL_SECONDS * 1_000),
        revoked_at: null,
      })
      .execute();

    const accessToken = await new SignJWT({
      player_id: identity.player_id,
      session_id: sessionId,
      token_type: 'access',
    })
      .setProtectedHeader({ alg: 'HS256', typ: 'JWT' })
      .setSubject(identity.account_id)
      .setIssuer(this.environment.JWT_ISSUER)
      .setAudience(this.environment.JWT_AUDIENCE)
      .setIssuedAt()
      .setExpirationTime(accessExpiresAt)
      .sign(this.signingKey);

    return {
      account_id: identity.account_id,
      player_id: identity.player_id,
      access_token: accessToken,
      refresh_token: refreshToken,
      access_token_expires_at: accessExpiresAt.toISOString(),
    };
  }

  async verifyAccessToken(token: string): Promise<AccessClaims> {
    try {
      const result = await jwtVerify(token, this.signingKey, {
        issuer: this.environment.JWT_ISSUER,
        audience: this.environment.JWT_AUDIENCE,
      });
      const playerId = result.payload.player_id;
      const sessionId = result.payload.session_id;
      if (
        typeof result.payload.sub !== 'string' ||
        typeof playerId !== 'string' ||
        typeof sessionId !== 'string' ||
        result.payload.token_type !== 'access'
      ) {
        throw new Error('Required claims are missing');
      }

      const session = await this.database
        .selectFrom('sessions')
        .select(['revoked_at', 'expires_at'])
        .where('id', '=', sessionId)
        .where('account_id', '=', result.payload.sub)
        .executeTakeFirst();
      if (!session || session.revoked_at || new Date(session.expires_at) <= new Date()) {
        throw new Error('Session is no longer active');
      }

      return { accountId: result.payload.sub, playerId, sessionId };
    } catch {
      throw new DomainError(
        HttpStatus.UNAUTHORIZED,
        'INVALID_TOKEN',
        'Access token is invalid or expired.',
      );
    }
  }
}
