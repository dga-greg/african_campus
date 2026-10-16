import { Router, type RequestHandler } from 'express';
import { requireAuth } from '../../middleware/authenticate.js';
import type { AuthController } from './auth.controller.js';

export function authRoutes(controller: AuthController, credentialLimiter: RequestHandler): Router {
  const router = Router();
  router.get('/csrf', controller.csrfToken);
  router.post('/register', credentialLimiter, controller.register);
  router.post('/login', credentialLimiter, controller.login);
  router.post('/refresh', controller.refresh);
  router.post('/logout', controller.logout);
  router.post('/forgot-password', credentialLimiter, controller.forgotPassword);
  router.post('/reset-password', credentialLimiter, controller.resetPassword);

  router.get('/me', requireAuth, controller.me);
  router.patch('/me', requireAuth, controller.updateMe);
  router.post('/change-password', requireAuth, credentialLimiter, controller.changePassword);
  router.get('/sessions', requireAuth, controller.sessions);
  router.delete('/sessions/:sessionId', requireAuth, controller.revokeSession);
  return router;
}
