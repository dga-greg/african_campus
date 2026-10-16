import { z } from 'zod';

const bool = z
  .enum(['true', 'false', '1', '0'])
  .transform((v) => v === 'true' || v === '1');

const EnvSchema = z
  .object({
    NODE_ENV: z.enum(['development', 'test', 'production']).default('development'),
    PORT: z.coerce.number().int().positive().default(3000),
    LOG_LEVEL: z.enum(['fatal', 'error', 'warn', 'info', 'debug', 'trace', 'silent']).default('info'),
    TRUST_PROXY: z.coerce.number().int().min(0).default(0),
    APP_ORIGIN: z.url(),

    MONGODB_URI: z.string().min(1),
    REDIS_URL: z.string().optional().transform((v) => (v ? v : undefined)),

    JWT_ACCESS_SECRET: z.string().min(32, 'JWT_ACCESS_SECRET must be at least 32 characters'),
    CSRF_SECRET: z.string().min(32, 'CSRF_SECRET must be at least 32 characters'),

    ACCESS_TOKEN_TTL_SECONDS: z.coerce.number().int().min(60).max(3600).default(900),
    REFRESH_TOKEN_TTL_DAYS: z.coerce.number().int().min(1).max(90).default(30),
    SESSION_MAX_LIFETIME_DAYS: z.coerce.number().int().min(1).max(365).default(90),
    COOKIE_SECURE: bool.optional(),

    ARGON2_MEMORY_KIB: z.coerce.number().int().min(1024).default(19456),
    ARGON2_TIME_COST: z.coerce.number().int().min(1).default(2),
    ARGON2_PARALLELISM: z.coerce.number().int().min(1).default(1),
    LOGIN_MAX_ATTEMPTS: z.coerce.number().int().min(1).default(5),
    LOGIN_LOCK_MINUTES: z.coerce.number().int().min(1).default(15),
    PASSWORD_RESET_TTL_MINUTES: z.coerce.number().int().min(5).max(24 * 60).default(30),

    RATE_LIMIT_GLOBAL_PER_MINUTE: z.coerce.number().int().min(1).default(300),
    RATE_LIMIT_AUTH_PER_15_MINUTES: z.coerce.number().int().min(1).default(10),

    SMTP_URL: z.string().optional().transform((v) => (v ? v : undefined)),
    MAIL_FROM: z.string().default('Family Heritage <no-reply@localhost>'),
  })
  .superRefine((env, ctx) => {
    if (env.NODE_ENV !== 'production') return;
    if (!env.REDIS_URL) ctx.addIssue({ code: 'custom', path: ['REDIS_URL'], message: 'required in production' });
    if (!env.SMTP_URL) ctx.addIssue({ code: 'custom', path: ['SMTP_URL'], message: 'required in production' });
    if (env.COOKIE_SECURE === false)
      ctx.addIssue({ code: 'custom', path: ['COOKIE_SECURE'], message: 'must not be false in production' });
    if (!env.APP_ORIGIN.startsWith('https://'))
      ctx.addIssue({ code: 'custom', path: ['APP_ORIGIN'], message: 'must use https in production' });
  });

export type Env = z.infer<typeof EnvSchema> & { COOKIE_SECURE: boolean };

export function loadEnv(source: NodeJS.ProcessEnv = process.env): Env {
  const parsed = EnvSchema.safeParse(source);
  if (!parsed.success) {
    const lines = parsed.error.issues.map((i) => `  - ${i.path.join('.')}: ${i.message}`);
    throw new Error(`Invalid environment configuration:\n${lines.join('\n')}`);
  }
  const env = parsed.data;
  return { ...env, COOKIE_SECURE: env.COOKIE_SECURE ?? env.NODE_ENV === 'production' };
}
