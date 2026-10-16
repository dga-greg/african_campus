import { Redis } from 'ioredis';
import type { Logger } from '../core/logger.js';

export function createRedis(url: string, logger: Logger): Redis {
  const redis = new Redis(url, {
    // Required by BullMQ for blocking commands.
    maxRetriesPerRequest: null,
    enableReadyCheck: true,
  });
  redis.on('error', (err) => logger.error({ err }, 'Redis error'));
  redis.on('ready', () => logger.info('Redis connected'));
  return redis;
}

export type { Redis };
