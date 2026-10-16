import { Worker } from 'bullmq';
import { loadEnv } from './config/env.js';
import { createLogger } from './core/logger.js';
import { connectMongo, disconnectMongo } from './infra/mongo.js';
import { createRedis } from './infra/redis.js';
import { LogMailer, SmtpMailer } from './infra/mailer.js';
import { QUEUE_NAMES, type EmailJob } from './infra/queues.js';
import './modules/models.js';

/**
 * Background worker. Phase 1 processes outgoing e-mail; generation recalculation (Phase 3) and media
 * processing (Phase 7) are added here as further queues.
 */
async function main(): Promise<void> {
  const env = loadEnv();
  const logger = createLogger(env.LOG_LEVEL, env.NODE_ENV === 'development');
  if (!env.REDIS_URL) throw new Error('The worker requires REDIS_URL');

  await connectMongo(env.MONGODB_URI, logger);
  const connection = createRedis(env.REDIS_URL, logger);
  const mailer = env.SMTP_URL ? new SmtpMailer(env.SMTP_URL, env.MAIL_FROM) : new LogMailer(logger);

  const emailWorker = new Worker<EmailJob>(
    QUEUE_NAMES.email,
    async (job) => {
      await mailer.send(job.data.message);
    },
    { connection, concurrency: 5 },
  );
  emailWorker.on('failed', (job, err) => logger.error({ err, jobId: job?.id, attempts: job?.attemptsMade }, 'E-mail job failed'));
  logger.info('Worker started');

  const shutdown = async () => {
    await emailWorker.close();
    await connection.quit();
    await disconnectMongo();
    process.exit(0);
  };
  process.on('SIGTERM', () => void shutdown());
  process.on('SIGINT', () => void shutdown());
}

main().catch((err: unknown) => {
  console.error(err);
  process.exit(1);
});
