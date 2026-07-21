import {
  ArgumentsHost,
  Catch,
  HttpException,
  HttpStatus,
  type ExceptionFilter,
} from '@nestjs/common';
import type { FastifyReply, FastifyRequest } from 'fastify';
import { randomUUID } from 'node:crypto';
import { DomainError } from './errors.js';

@Catch()
export class ProblemDetailsFilter implements ExceptionFilter {
  catch(exception: unknown, host: ArgumentsHost): void {
    const context = host.switchToHttp();
    const request = context.getRequest<FastifyRequest>();
    const response = context.getResponse<FastifyReply>();
    const correlationId = request.headers['x-correlation-id']?.toString() ?? randomUUID();

    let status = HttpStatus.INTERNAL_SERVER_ERROR;
    let code = 'INTERNAL_ERROR';
    let detail = 'The server could not complete the request.';

    if (exception instanceof DomainError) {
      status = exception.status;
      code = exception.code;
      detail = exception.message;
    } else if (exception instanceof HttpException) {
      status = exception.getStatus();
      code = status === HttpStatus.UNAUTHORIZED ? 'UNAUTHORIZED' : 'HTTP_ERROR';
      detail = exception.message;
    } else {
      console.error('Unhandled request error', exception);
    }

    void response
      .status(status)
      .type('application/problem+json')
      .send({
        type: `https://kingdom.local/problems/${code.toLowerCase()}`,
        title: code.replaceAll('_', ' '),
        status,
        code,
        detail,
        instance: request.url,
        correlation_id: correlationId,
      });
  }
}
