import { Global, Module } from '@nestjs/common';
import { getEnvironment, type Environment } from './environment.js';

export const ENVIRONMENT = Symbol('ENVIRONMENT');

@Global()
@Module({
  providers: [{ provide: ENVIRONMENT, useFactory: (): Environment => getEnvironment() }],
  exports: [ENVIRONMENT],
})
export class EnvironmentModule {}
