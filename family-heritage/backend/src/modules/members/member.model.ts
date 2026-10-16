import { Schema, model, type InferSchemaType } from 'mongoose';
import { GENDERS, GENERATION_STATUSES, MEMBER_PRIVACY_LEVELS, VITAL_STATUSES } from '@fh/contracts';
import { FuzzyDateSchema, SourceReferenceSchema, softDeleteFields } from '../../core/schemas.js';

const TimelineEntrySchema = new Schema(
  {
    title: { type: String, required: true, trim: true, maxlength: 200 },
    organisation: { type: String, trim: true, maxlength: 200 },
    from: { type: FuzzyDateSchema, default: null },
    to: { type: FuzzyDateSchema, default: null },
    description: { type: String, maxlength: 2000 },
  },
  { _id: true },
);

const StorySchema = new Schema(
  {
    title: { type: String, required: true, trim: true, maxlength: 200 },
    body: { type: String, required: true, maxlength: 50_000 },
    kind: { type: String, enum: ['story', 'oral_history', 'tradition', 'achievement'], default: 'story' },
    told_by: { type: String, maxlength: 200 },
    sources: { type: [SourceReferenceSchema], default: [] },
  },
  { _id: true, timestamps: true },
);

/**
 * A person in one family's tree. Generation fields are a cache owned by the generation engine
 * (Phase 3) and must never be written from user input.
 */
const FamilyMemberSchema = new Schema(
  {
    familyId: { type: Schema.Types.ObjectId, ref: 'Family', required: true },

    firstName: { type: String, required: true, trim: true, maxlength: 100 },
    middleNames: { type: String, default: '', trim: true, maxlength: 200 },
    surname: { type: String, default: '', trim: true, maxlength: 100 },
    maidenName: { type: String, default: '', trim: true, maxlength: 100 },
    previousNames: { type: [String], default: [] },
    preferredName: { type: String, default: '', trim: true, maxlength: 100 },
    title: { type: String, default: '', trim: true, maxlength: 60 },
    gender: { type: String, enum: GENDERS, default: undefined },

    vitalStatus: { type: String, enum: VITAL_STATUSES, default: 'unknown' },
    birthDate: { type: FuzzyDateSchema, default: null },
    birthPlace: { type: String, default: '', maxlength: 200 },
    deathDate: { type: FuzzyDateSchema, default: null },
    deathPlace: { type: String, default: '', maxlength: 200 },
    nationality: { type: String, default: '', maxlength: 100 },
    hometown: { type: String, default: '', maxlength: 120 },

    biography: { type: String, default: '', maxlength: 50_000 },
    education: { type: [TimelineEntrySchema], default: [] },
    occupations: { type: [TimelineEntrySchema], default: [] },
    achievements: { type: [String], default: [] },
    stories: { type: [StorySchema], default: [] },
    sources: { type: [SourceReferenceSchema], default: [] },

    profilePhotoMediaId: { type: Schema.Types.ObjectId, ref: 'FamilyMedia', default: null },

    /** Private contact details: never returned unless explicitly selected for an authorised viewer. */
    contact: {
      type: new Schema(
        {
          email: { type: String, maxlength: 254 },
          phone: { type: String, maxlength: 40 },
          address: { type: String, maxlength: 500 },
        },
        { _id: false },
      ),
      select: false,
      default: undefined,
    },

    privacy: {
      level: { type: String, enum: MEMBER_PRIVACY_LEVELS, default: 'family' },
      hideDates: { type: Boolean, default: false },
    },

    generation: {
      number: { type: Number, default: null },
      status: { type: String, enum: GENERATION_STATUSES, default: 'pending' },
      candidates: { type: [Number], default: [] },
      override: {
        type: new Schema(
          {
            number: { type: Number, required: true, min: 1 },
            reason: { type: String, required: true, maxlength: 1000 },
            by: { type: Schema.Types.ObjectId, ref: 'User', required: true },
            at: { type: Date, required: true },
          },
          { _id: false },
        ),
        default: null,
      },
      computedAt: { type: Date, default: null },
    },

    /** Normalised (lower-case, accent-free) name tokens and prefixes for partial search. */
    searchTokens: { type: [String], default: [] },
    /** Phonetic keys for spelling-variation search. */
    phoneticKeys: { type: [String], default: [] },

    createdBy: { type: Schema.Types.ObjectId, ref: 'User', required: true },
    updatedBy: { type: Schema.Types.ObjectId, ref: 'User', default: null },
    ...softDeleteFields,
  },
  { timestamps: true, collection: 'family_members', optimisticConcurrency: true, versionKey: 'version' },
);

FamilyMemberSchema.index({ familyId: 1, deletedAt: 1, 'generation.number': 1 });
FamilyMemberSchema.index({ familyId: 1, searchTokens: 1 });
FamilyMemberSchema.index({ familyId: 1, phoneticKeys: 1 });
FamilyMemberSchema.index({ familyId: 1, surname: 1, firstName: 1 });
FamilyMemberSchema.index({ familyId: 1, 'birthDate.year': 1 });
FamilyMemberSchema.index({ familyId: 1, hometown: 1 });
FamilyMemberSchema.index({ familyId: 1, 'generation.status': 1 });

export type FamilyMember = InferSchemaType<typeof FamilyMemberSchema>;
export type FamilyMemberDoc = ReturnType<(typeof FamilyMemberModel)['hydrate']>;
export const FamilyMemberModel = model('FamilyMember', FamilyMemberSchema);
