import type { Request, Response } from 'express';
import { authOf } from '../../middleware/authenticate.js';
import { accessOf } from './family-access.js';
import { toFamilyView } from './family.mapper.js';
import type { FamiliesService } from './families.service.js';

export class FamiliesController {
  constructor(private readonly families: FamiliesService) {}

  listMine = async (req: Request, res: Response) => {
    res.json({ data: await this.families.listForUser(authOf(req).userId) });
  };

  get = (req: Request, res: Response) => {
    res.json({ data: toFamilyView(accessOf(req)) });
  };
}
