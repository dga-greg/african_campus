import { Schema, model, type InferSchemaType } from 'mongoose';
import { FAMILY_STATUSES, FAMILY_VISIBILITIES } from '@fh/contracts';

const FamilySchema = new Schema(
  {
    name: { type: String, required: true, trim: true, maxlength: 120 },
    slug: { type: String, required: true, lowercase: true, trim: true, maxlength: 140 },
    description: { type: String, default: '', maxlength: 2000 },
    history: { type: String, default: '', maxlength: 50_000 },
    emblemMediaId: { type: Schema.Types.ObjectId, ref: 'FamilyMedia', default: null },
    /** Generation 1. All generation numbers are relative to this person. */
    foundingMemberId: { type: Schema.Types.ObjectId, ref: 'FamilyMember', default: null },
    origin: {
      hometown: { type: String, default: '', maxlength: 120 },
      region: { type: String, default: '', maxlength: 120 },
      country: { type: String, default: '', maxlength: 120 },
    },
    privacy: {
      visibility: { type: String, enum: FAMILY_VISIBILITIES, default: 'private' },
      showLivingToMembers: { type: Boolean, default: true },
      allowMemberUploads: { type: Boolean, default: true },
      requireApprovalForMemberEdits: { type: Boolean, default: true },
    },
    generationPolicy: {
      countAdoptive: { type: Boolean, default: true },
    },
    status: { type: String, enum: FAMILY_STATUSES, default: 'active' },
    /** Cache maintained by background jobs; never edited directly. */
    stats: {
      memberCount: { type: Number, default: 0 },
      generationCount: { type: Number, default: 0 },
      branchCount: { type: Number, default: 0 },
      livingCount: { type: Number, default: 0 },
      deceasedCount: { type: Number, default: 0 },
      recalculatedAt: { type: Date, default: null },
    },
    createdBy: { type: Schema.Types.ObjectId, ref: 'User', required: true },
  },
  { timestamps: true, collection: 'families', optimisticConcurrency: true, versionKey: 'version' },
);

FamilySchema.index({ slug: 1 }, { unique: true });
FamilySchema.index({ 'privacy.visibility': 1, status: 1 });
FamilySchema.index({ createdBy: 1 });

export type Family = InferSchemaType<typeof FamilySchema>;
export type FamilyDoc = ReturnType<(typeof FamilyModel)['hydrate']>;
export const FamilyModel = model('Family', FamilySchema);
