import type { AdminUserView, CurrentUser } from '@fh/contracts';
import { iso } from '../../core/dates.js';
import type { UserDoc } from './user.model.js';

export function toCurrentUser(user: UserDoc): CurrentUser {
  return {
    id: user.id as string,
    email: user.email,
    displayName: user.displayName,
    platformRole: user.platformRole,
    emailVerified: user.emailVerifiedAt != null,
    preferences: {
      locale: user.preferences?.locale ?? 'en',
      theme: user.preferences?.theme ?? 'system',
      defaultFamilyId: user.preferences?.defaultFamilyId?.toString() ?? null,
    },
    createdAt: user.createdAt.toISOString(),
  };
}

export function toAdminUserView(user: UserDoc, familyCount: number): AdminUserView {
  return {
    id: user.id as string,
    email: user.email,
    displayName: user.displayName,
    platformRole: user.platformRole,
    status: user.status,
    emailVerified: user.emailVerifiedAt != null,
    lastLoginAt: iso(user.lastLoginAt),
    createdAt: user.createdAt.toISOString(),
    familyCount,
  };
}
