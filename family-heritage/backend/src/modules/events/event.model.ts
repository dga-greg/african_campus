import { Schema, model, type InferSchemaType } from 'mongoose';
import { FuzzyDateSchema, SourceReferenceSchema, softDeleteFields } from '../../core/schemas.js';

const FamilyEventSchema = new Schema(
  {
    familyId: { type: Schema.Types.ObjectId, ref: 'Family', required: true },
    kind: {
      type: String,
      enum: ['birth', 'death', 'marriage', 'reunion', 'migration', 'tradition', 'historical', 'other'],
      required: true,
    },
    title: { type: String, required: true, trim: true, maxlength: 200 },
    description: { type: String, default: '', maxlength: 20_000 },
    date: { type: FuzzyDateSchema, default: null },
    place: { type: String, default: '', maxlength: 200 },
    memberIds: { type: [Schema.Types.ObjectId], ref: 'FamilyMember', default: [] },
    mediaIds: { type: [Schema.Types.ObjectId], ref: 'FamilyMedia', default: [] },
    sources: { type: [SourceReferenceSchema], default: [] },
    privacy: { type: String, enum: ['family', 'admins', 'public'], default: 'family' },
    createdBy: { type: Schema.Types.ObjectId, ref: 'User', required: true },
    ...softDeleteFields,
  },
  { timestamps: true, collection: 'family_events' },
);

FamilyEventSchema.index({ familyId: 1, 'date.sortKey': 1 });
FamilyEventSchema.index({ familyId: 1, memberIds: 1 });

export type FamilyEvent = InferSchemaType<typeof FamilyEventSchema>;
export const FamilyEventModel = model('FamilyEvent', FamilyEventSchema);
