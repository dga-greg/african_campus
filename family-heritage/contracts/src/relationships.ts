export const RELATIONSHIP_TYPES = ['parent_child', 'partnership'] as const;
export type RelationshipType = (typeof RELATIONSHIP_TYPES)[number];

export const PARENT_CHILD_SUBTYPES = ['biological', 'adoptive', 'step', 'foster', 'guardian', 'unknown'] as const;
export type ParentChildSubtype = (typeof PARENT_CHILD_SUBTYPES)[number];

export const PARTNERSHIP_SUBTYPES = [
  'marriage',
  'customary_marriage',
  'civil_union',
  'partnership',
  'engagement',
  'unknown',
] as const;
export type PartnershipSubtype = (typeof PARTNERSHIP_SUBTYPES)[number];

export const PARTNERSHIP_STATUSES = ['current', 'divorced', 'separated', 'widowed', 'annulled', 'unknown'] as const;
export type PartnershipStatus = (typeof PARTNERSHIP_STATUSES)[number];

export const VERIFICATION_STATES = ['verified', 'unverified', 'disputed'] as const;
export type VerificationState = (typeof VERIFICATION_STATES)[number];

/** Parent-child subtypes that can ever count as descent for generation numbering. */
export const DESCENT_SUBTYPES: readonly ParentChildSubtype[] = ['biological', 'adoptive'];
