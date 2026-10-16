import type { FamilyRole, FamilyViewerRole } from './roles.js';

export const FAMILY_VISIBILITIES = ['private', 'unlisted', 'public'] as const;
export type FamilyVisibility = (typeof FAMILY_VISIBILITIES)[number];

export const FAMILY_STATUSES = ['active', 'suspended', 'archived'] as const;
export type FamilyStatus = (typeof FAMILY_STATUSES)[number];

export interface FamilyOrigin {
  hometown: string;
  region: string;
  country: string;
}

export interface FamilyPrivacySettings {
  visibility: FamilyVisibility;
  showLivingToMembers: boolean;
  allowMemberUploads: boolean;
  requireApprovalForMemberEdits: boolean;
}

export interface FamilyGenerationPolicy {
  /** Whether adoptive parent-child links count as descent for generation numbering. */
  countAdoptive: boolean;
}

export interface FamilyStats {
  memberCount: number;
  generationCount: number;
  branchCount: number;
  livingCount: number;
  deceasedCount: number;
  recalculatedAt: string | null;
}

/** A family as seen by someone allowed to view it. Guests receive the `public` subset only. */
export interface FamilyView {
  id: string;
  name: string;
  slug: string;
  description: string;
  history: string;
  emblemUrl: string | null;
  foundingMemberId: string | null;
  origin: FamilyOrigin;
  privacy: FamilyPrivacySettings | null;
  generationPolicy: FamilyGenerationPolicy | null;
  status: FamilyStatus;
  stats: FamilyStats;
  createdAt: string;
  viewerRole: FamilyViewerRole;
}

/** An entry in "my families". */
export interface FamilySummary {
  id: string;
  name: string;
  slug: string;
  origin: FamilyOrigin;
  role: FamilyRole;
  memberCount: number;
  generationCount: number;
  joinedAt: string;
}
