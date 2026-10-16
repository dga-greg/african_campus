import { Schema, model, type InferSchemaType } from 'mongoose';
import { FuzzyDateSchema, softDeleteFields } from '../../core/schemas.js';

/** Metadata for a file held in object storage. Files are only served through signed, short-lived URLs. */
const FamilyMediaSchema = new Schema(
  {
    familyId: { type: Schema.Types.ObjectId, ref: 'Family', required: true },
    kind: { type: String, enum: ['photo', 'document', 'audio', 'video'], required: true },
    title: { type: String, required: true, trim: true, maxlength: 200 },
    description: { type: String, default: '', maxlength: 5000 },
    storageKey: { type: String, required: true },
    mimeType: { type: String, required: true },
    byteSize: { type: Number, required: true, min: 0 },
    sha256: { type: String, required: true },
    width: { type: Number },
    height: { type: Number },
    durationSec: { type: Number },
    memberIds: { type: [Schema.Types.ObjectId], ref: 'FamilyMember', default: [] },
    eventId: { type: Schema.Types.ObjectId, ref: 'FamilyEvent', default: null },
    date: { type: FuzzyDateSchema, default: null },
    privacy: { type: String, enum: ['family', 'admins', 'public'], default: 'family' },
    provenance: {
      source: { type: String, default: '', maxlength: 500 },
      citation: { type: String, default: '', maxlength: 2000 },
      originalHolder: { type: String, default: '', maxlength: 200 },
    },
    uploadedBy: { type: Schema.Types.ObjectId, ref: 'User', required: true },
    status: { type: String, enum: ['processing', 'pending_approval', 'published', 'rejected'], default: 'processing' },
    ...softDeleteFields,
  },
  { timestamps: true, collection: 'family_media' },
);

FamilyMediaSchema.index({ familyId: 1, memberIds: 1 });
FamilyMediaSchema.index({ familyId: 1, kind: 1, createdAt: -1 });
FamilyMediaSchema.index({ familyId: 1, status: 1 });
FamilyMediaSchema.index({ storageKey: 1 }, { unique: true });

export type FamilyMedia = InferSchemaType<typeof FamilyMediaSchema>;
export const FamilyMediaModel = model('FamilyMedia', FamilyMediaSchema);
