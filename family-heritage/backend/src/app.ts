import express, { type Express } from 'express';
import compression from 'compression';
import cookieParser from 'cookie-parser';
import helmet from 'helmet';
import mongoose from 'mongoose';
import { pinoHttp } from 'pino-http';
import type { Env } from './config/env.js';
import type { Logger } from './core/logger.js';
import type { Redis } from './infra/redis.js';
import { EmailDispatcher } from './infra/queues.js';
import type { Mailer } from './infra/mailer.js';
import { requestId } from './middleware/request-id.js';
import { CsrfProtection } from './middleware/csrf.js';
import { authenticate } from './middleware/authenticate.js';
import { authRateLimit, globalRateLimit } from './middleware/rate-limit.js';
import { errorHandler, notFoundHandler } from './middleware/error-handler.js';
import './modules/models.js';
import { AuditService } from './modules/audit/audit.service.js';
import { PasswordHasher } from './modules/auth/password.js';
import { AccessTokenService } from './modules/auth/tokens.js';
import { AuthCookies } from './modules/auth/cookies.js';
import { AuthService } from './modules/auth/auth.service.js';
import { AuthController } from './modules/auth/auth.controller.js';
import { authRoutes } from './modules/auth/auth.routes.js';
import { AdminService } from './modules/admin/admin.service.js';
import { AdminController } from './modules/admin/admin.controller.js';
import { adminRoutes } from './modules/admin/admin.routes.js';
import { FamiliesService } from './modules/families/families.service.js';
import { FamiliesController } from './modules/families/families.controller.js';
import { familiesRoutes } from './modules/families/families.routes.js';

export interface AppDependencies {
  env: Env;
  logger: Logger;
  mailer: Mailer;
  redis?: Redis;
}

export interface AppInstance {
  app: Express;
  close(): Promise<void>;
}

/** Builds the Express application. Does not listen or connect to databases (see server.ts). */
export function createApp({ env, logger, mailer, redis }: AppDependencies): AppInstance {
  const app = express();
  app.disable('x-powered-by');
  app.set('trust proxy', env.TRUST_PROXY);

  // ---- services (composition root)
  const hasher = new PasswordHasher({
    memoryCost: env.ARGON2_MEMORY_KIB,
    timeCost: env.ARGON2_TIME_COST,
    parallelism: env.ARGON2_PARALLELISM,
  });
  const accessTokens = new AccessTokenService(env.JWT_ACCESS_SECRET, env.ACCESS_TOKEN_TTL_SECONDS);
  const cookies = new AuthCookies(env.COOKIE_SECURE);
  const csrf = new CsrfProtection(env.CSRF_SECRET, { secure: env.COOKIE_SECURE, sameSite: 'strict', path: '/' });
  const emailDispatcher = new EmailDispatcher(mailer, logger, redis);
  const audit = new AuditService();
  const authService = new AuthService(
    {
      appOrigin: env.APP_ORIGIN,
      refreshTokenTtlDays: env.REFRESH_TOKEN_TTL_DAYS,
      sessionMaxLifetimeDays: env.SESSION_MAX_LIFETIME_DAYS,
      loginMaxAttempts: env.LOGIN_MAX_ATTEMPTS,
      loginLockMinutes: env.LOGIN_LOCK_MINUTES,
      passwordResetTtlMinutes: env.PASSWORD_RESET_TTL_MINUTES,
    },
    hasher,
    accessTokens,
    emailDispatcher,
    logger,
  );

  // ---- global middleware
  app.use(requestId);
  if (env.NODE_ENV !== 'test') {
    app.use(pinoHttp({ logger, genReqId: (req) => (req as express.Request).id }));
  }
  app.use(
    helmet({
      // The API only serves JSON; the SPA's own CSP is set by the web server that hosts it.
      contentSecurityPolicy: { directives: { defaultSrc: ["'none'"], frameAncestors: ["'none'"] } },
      crossOriginResourcePolicy: { policy: 'same-origin' },
    }),
  );
  app.use(compression());

  // Health endpoints sit outside /api so load balancers need neither cookies nor CSRF tokens.
  app.get('/health/live', (_req, res) => {
    res.json({ status: 'ok' });
  });
  app.get('/health/ready', (_req, res) => {
    const mongoReady = mongoose.connection.readyState === 1;
    const redisReady = redis ? redis.status === 'ready' : true;
    res.status(mongoReady && redisReady ? 200 : 503).json({ mongo: mongoReady, redis: redis ? redisReady : 'disabled' });
  });

  const api = express.Router();
  api.use(globalRateLimit(env.RATE_LIMIT_GLOBAL_PER_MINUTE, redis));
  api.use(express.json({ limit: '100kb' }));
  api.use(cookieParser());
  api.use((_req, res, next) => {
    res.setHeader('Cache-Control', 'no-store');
    next();
  });
  api.use(csrf.ensureCookie);
  api.use(csrf.verify);
  api.use(authenticate(accessTokens));

  const credentialLimiter = authRateLimit(env.RATE_LIMIT_AUTH_PER_15_MINUTES, redis);
  api.use('/auth', authRoutes(new AuthController(authService, cookies, csrf), credentialLimiter));
  api.use('/families', familiesRoutes(new FamiliesController(new FamiliesService())));
  api.use('/admin', adminRoutes(new AdminController(new AdminService(audit, authService))));
  api.use(notFoundHandler);

  app.use('/api/v1', api);
  app.use(notFoundHandler);
  app.use(errorHandler(logger));

  return {
    app,
    close: () => emailDispatcher.close(),
  };
}
