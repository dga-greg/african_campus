import { Schema, model, type InferSchemaType } from 'mongoose';
import { PLATFORM_ROLES, THEMES, USER_STATUSES } from '@fh/contracts';

/** Authentication accounts. Deliberately separate from genealogy profiles (family_members). */
const UserSchema = new Schema(
  {
    email: { type: String, required: true, lowercase: true, trim: true, maxlength: 254 },
    passwordHash: { type: String, required: true, select: false },
    displayName: { type: String, required: true, trim: true, maxlength: 80 },
    platformRole: { type: String, enum: PLATFORM_ROLES, default: 'user', required: true },
    status: { type: String, enum: USER_STATUSES, default: 'active', required: true },
    emailVerifiedAt: { type: Date, default: null },
    passwordChangedAt: { type: Date, default: () => new Date() },
    failedLoginCount: { type: Number, default: 0 },
    lockedUntil: { type: Date, default: null },
    lastLoginAt: { type: Date, default: null },
    preferences: {
      locale: { type: String, default: 'en', maxlength: 16 },
      theme: { type: String, enum: THEMES, default: 'system' },
      defaultFamilyId: { type: Schema.Types.ObjectId, ref: 'Family', default: null },
    },
  },
  { timestamps: true, collection: 'users' },
);

UserSchema.index({ email: 1 }, { unique: true });
UserSchema.index({ platformRole: 1, status: 1 });

export type User = InferSchemaType<typeof UserSchema>;
export type UserDoc = ReturnType<(typeof UserModel)['hydrate']>;
export const UserModel = model('User', UserSchema);
