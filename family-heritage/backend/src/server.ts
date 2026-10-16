import { loadEnv } from './config/env.js';
import { createLogger } from './core/logger.js';
import { connectMongo, disconnectMongo, syncIndexes } from './infra/mongo.js';
import { createRedis } from './infra/redis.js';
import { LogMailer, SmtpMailer } from './infra/mailer.js';
import { createApp } from './app.js';

async function main(): Promise<void> {
  const env = loadEnv();
  const logger = createLogger(env.LOG_LEVEL, env.NODE_ENV === 'development');

  await connectMongo(env.MONGODB_URI, logger);
  const redis = env.REDIS_URL ? createRedis(env.REDIS_URL, logger) : undefined;
  if (!redis) logger.warn('REDIS_URL not set: using in-memory rate limits and inline e-mail delivery (development only)');

  const { app, close } = createApp({
    env,
    logger,
    redis,
    mailer: env.SMTP_URL ? new SmtpMailer(env.SMTP_URL, env.MAIL_FROM) : new LogMailer(logger),
  });
  await syncIndexes(logger);

  const server = app.listen(env.PORT, () => logger.info({ port: env.PORT }, 'API listening'));

  const shutdown = (signal: string) => {
    logger.info({ signal }, 'Shutting down');
    server.close(async () => {
      await close();
      await redis?.quit();
      await disconnectMongo();
      process.exit(0);
    });
    setTimeout(() => process.exit(1), 15_000).unref();
  };
  process.on('SIGTERM', () => shutdown('SIGTERM'));
  process.on('SIGINT', () => shutdown('SIGINT'));
}

main().catch((err: unknown) => {
  console.error(err);
  process.exit(1);
});
