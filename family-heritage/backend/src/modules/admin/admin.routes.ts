import { Router } from 'express';
import { requireAuth, requirePlatformPermission } from '../../middleware/authenticate.js';
import type { AdminController } from './admin.controller.js';

export function adminRoutes(controller: AdminController): Router {
  const router = Router();
  router.use(requireAuth);
  router.get('/stats', requirePlatformPermission('platform.stats'), controller.stats);
  router.get('/users', requirePlatformPermission('platform.users.view'), controller.listUsers);
  router.patch('/users/:userId', requirePlatformPermission('platform.users.manage'), controller.updateUser);
  return router;
}
