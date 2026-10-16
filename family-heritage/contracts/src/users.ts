import type { PlatformRole } from './roles.js';

export const USER_STATUSES = ['active', 'suspended', 'deleted'] as const;
export type UserStatus = (typeof USER_STATUSES)[number];

export const THEMES = ['system', 'light', 'dark'] as const;
export type ThemePreference = (typeof THEMES)[number];

export interface UserPreferences {
  locale: string;
  theme: ThemePreference;
  defaultFamilyId: string | null;
}

/** The signed-in user's own account, as returned by /auth endpoints. */
export interface CurrentUser {
  id: string;
  email: string;
  displayName: string;
  platformRole: PlatformRole;
  emailVerified: boolean;
  preferences: UserPreferences;
  createdAt: string;
}

/** Account view for platform administrators. Contains no genealogy data. */
export interface AdminUserView {
  id: string;
  email: string;
  displayName: string;
  platformRole: PlatformRole;
  status: UserStatus;
  emailVerified: boolean;
  lastLoginAt: string | null;
  createdAt: string;
  familyCount: number;
}

export interface AdminUpdateUserRequest {
  status?: Exclude<UserStatus, 'deleted'>;
  platformRole?: PlatformRole;
}

export interface PlatformStats {
  users: { total: number; active: number; suspended: number };
  families: { total: number; active: number };
}
