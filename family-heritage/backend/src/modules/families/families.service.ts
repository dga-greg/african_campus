import type { Types } from 'mongoose';
import type { FamilySummary } from '@fh/contracts';
import { FamilyModel } from './family.model.js';
import { FamilyMembershipModel } from './membership.model.js';
import { toFamilySummary } from './family.mapper.js';

export class FamiliesService {
  /** Families the user belongs to. Suspended/archived families stay listed so members can see why. */
  async listForUser(userId: Types.ObjectId): Promise<FamilySummary[]> {
    const memberships = await FamilyMembershipModel.find({ userId, status: 'active' });
    if (memberships.length === 0) return [];
    const families = await FamilyModel.find({ _id: { $in: memberships.map((m) => m.familyId) } });
    const byId = new Map(families.map((f) => [f.id as string, f]));
    return memberships
      .flatMap((m) => {
        const family = byId.get(m.familyId.toString());
        return family ? [toFamilySummary(family, m)] : [];
      })
      .sort((a, b) => a.name.localeCompare(b.name));
  }
}
