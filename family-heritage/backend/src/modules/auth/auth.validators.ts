import { z } from 'zod';
import { DISPLAY_NAME_MAX_LENGTH, PASSWORD_MAX_LENGTH, PASSWORD_MIN_LENGTH, THEMES } from '@fh/contracts';
import { email, objectId } from '../../core/validation.js';

export const password = z
  .string()
  .min(PASSWORD_MIN_LENGTH, `Use at least ${PASSWORD_MIN_LENGTH} characters`)
  .max(PASSWORD_MAX_LENGTH, `Use at most ${PASSWORD_MAX_LENGTH} characters`);

const displayName = z.string().trim().min(1, 'Enter your name').max(DISPLAY_NAME_MAX_LENGTH);

export const registerBody = z
  .object({ email, password, displayName })
  .strict()
  .refine((b) => b.password.toLowerCase() !== b.email, { path: ['password'], message: 'Password must not be your e-mail address' });

export const loginBody = z
  .object({
    email,
    // Do not apply the length policy at login: existing passwords must keep working if the policy changes.
    password: z.string().min(1, 'Enter your password').max(PASSWORD_MAX_LENGTH),
  })
  .strict();

export const forgotPasswordBody = z.object({ email }).strict();

export const resetPasswordBody = z.object({ token: z.string().min(20).max(200), password }).strict();

export const changePasswordBody = z
  .object({ currentPassword: z.string().min(1).max(PASSWORD_MAX_LENGTH), newPassword: password })
  .strict()
  .refine((b) => b.currentPassword !== b.newPassword, { path: ['newPassword'], message: 'Choose a different password' });

export const updateProfileBody = z
  .object({
    displayName: displayName.optional(),
    preferences: z
      .object({
        locale: z.string().regex(/^[a-z]{2,3}(-[A-Z]{2})?$/, 'Invalid locale').optional(),
        theme: z.enum(THEMES).optional(),
        defaultFamilyId: objectId.nullable().optional(),
      })
      .strict()
      .optional(),
  })
  .strict();
