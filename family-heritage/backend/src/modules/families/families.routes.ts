import { Router } from 'express';
import { requireAuth } from '../../middleware/authenticate.js';
import { loadFamilyAccess, requireFamilyPermission } from './family-access.js';
import type { FamiliesController } from './families.controller.js';

export function familiesRoutes(controller: FamiliesController): Router {
  const router = Router();
  router.get('/', requireAuth, controller.listMine);
  // Public families are visible to guests, so this route does not require authentication.
  router.get('/:familyId', loadFamilyAccess, requireFamilyPermission('family.view'), controller.get);
  return router;
}
