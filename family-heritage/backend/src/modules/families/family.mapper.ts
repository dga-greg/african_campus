import type { FamilySummary, FamilyView } from '@fh/contracts';
import { iso } from '../../core/dates.js';
import type { FamilyAccess } from './family-access.js';
import type { FamilyDoc } from './family.model.js';
import type { FamilyMembershipDoc } from './membership.model.js';

function origin(family: FamilyDoc) {
  return {
    hometown: family.origin?.hometown ?? '',
    region: family.origin?.region ?? '',
    country: family.origin?.country ?? '',
  };
}

/** Members see settings; guests see only the public face of a public family. */
export function toFamilyView(access: FamilyAccess): FamilyView {
  const f = access.family;
  const isMember = access.membership != null;
  return {
    id: f.id as string,
    name: f.name,
    slug: f.slug,
    description: f.description,
    history: f.history,
    emblemUrl: null, // media URLs are signed per request once the archive exists (Phase 7)
    foundingMemberId: f.foundingMemberId?.toString() ?? null,
    origin: origin(f),
    privacy: isMember && f.privacy
      ? {
          visibility: f.privacy.visibility,
          showLivingToMembers: f.privacy.showLivingToMembers,
          allowMemberUploads: f.privacy.allowMemberUploads,
          requireApprovalForMemberEdits: f.privacy.requireApprovalForMemberEdits,
        }
      : null,
    generationPolicy: isMember ? { countAdoptive: f.generationPolicy?.countAdoptive ?? true } : null,
    status: f.status,
    stats: {
      memberCount: f.stats?.memberCount ?? 0,
      generationCount: f.stats?.generationCount ?? 0,
      branchCount: f.stats?.branchCount ?? 0,
      // Living/deceased counts reveal information about living people; members only.
      livingCount: isMember ? (f.stats?.livingCount ?? 0) : 0,
      deceasedCount: isMember ? (f.stats?.deceasedCount ?? 0) : 0,
      recalculatedAt: iso(f.stats?.recalculatedAt),
    },
    createdAt: f.createdAt.toISOString(),
    viewerRole: access.role,
  };
}

export function toFamilySummary(family: FamilyDoc, membership: FamilyMembershipDoc): FamilySummary {
  return {
    id: family.id as string,
    name: family.name,
    slug: family.slug,
    origin: origin(family),
    role: membership.role,
    memberCount: family.stats?.memberCount ?? 0,
    generationCount: family.stats?.generationCount ?? 0,
    joinedAt: membership.joinedAt.toISOString(),
  };
}
