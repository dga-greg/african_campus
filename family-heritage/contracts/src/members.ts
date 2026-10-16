import type { FuzzyDate } from './dates.js';

export const GENDERS = ['female', 'male', 'non_binary', 'other', 'undisclosed'] as const;
export type Gender = (typeof GENDERS)[number];

export const VITAL_STATUSES = ['living', 'deceased', 'unknown'] as const;
export type VitalStatus = (typeof VITAL_STATUSES)[number];

export const MEMBER_PRIVACY_LEVELS = ['family', 'public', 'restricted'] as const;
export type MemberPrivacyLevel = (typeof MEMBER_PRIVACY_LEVELS)[number];

export const GENERATION_STATUSES = [
  'founder',
  'computed',
  'conflict',
  'married_in',
  'unconnected',
  'unverified_path',
  'pending',
] as const;
export type GenerationStatus = (typeof GENERATION_STATUSES)[number];

export interface GenerationInfo {
  /** Null unless status is `founder` or `computed` (or an override resolved a conflict). */
  number: number | null;
  status: GenerationStatus;
  /** Candidate numbers when status is `conflict`, or provisional number for `unverified_path`. */
  candidates: number[];
  overridden: boolean;
}

/** Compact person representation used by tree nodes and search results. */
export interface PersonCard {
  id: string;
  displayName: string;
  firstName: string;
  surname: string;
  photoUrl: string | null;
  birthDate: FuzzyDate | null;
  deathDate: FuzzyDate | null;
  /** Omitted (null) when the viewer is not authorised to know it. */
  vitalStatus: VitalStatus | null;
  generation: GenerationInfo;
}
