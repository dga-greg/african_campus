import { rateLimit, type Store } from 'express-rate-limit';
import { RedisStore, type RedisReply } from 'rate-limit-redis';
import type { RequestHandler } from 'express';
import type { Redis } from '../infra/redis.js';
import { AppError } from '../core/errors.js';

function store(redis: Redis | undefined, prefix: string): Store | undefined {
  if (!redis) return undefined; // in-memory (single instance, development only)
  return new RedisStore({
    prefix: `rl:${prefix}:`,
    sendCommand: (command: string, ...args: string[]) => redis.call(command, ...args) as Promise<RedisReply>,
  });
}

function limiter(options: { windowMs: number; limit: number; prefix: string; redis?: Redis }): RequestHandler {
  return rateLimit({
    windowMs: options.windowMs,
    limit: options.limit,
    standardHeaders: 'draft-8',
    legacyHeaders: false,
    store: store(options.redis, options.prefix),
    handler: (_req, _res, next) => next(new AppError('RATE_LIMITED', 'Too many requests. Please wait and try again.')),
  });
}

export function globalRateLimit(perMinute: number, redis?: Redis): RequestHandler {
  return limiter({ windowMs: 60_000, limit: perMinute, prefix: 'global', redis });
}

/** Stricter limit for credential endpoints (login, register, password recovery). */
export function authRateLimit(per15Minutes: number, redis?: Redis): RequestHandler {
  return limiter({ windowMs: 15 * 60_000, limit: per15Minutes, prefix: 'auth', redis });
}
