import type { MailMessage } from '../../infra/mailer.js';

export function passwordResetEmail(to: string, displayName: string, resetUrl: string, ttlMinutes: number): MailMessage {
  return {
    to,
    subject: 'Reset your Family Heritage password',
    text: [
      `Hello ${displayName},`,
      '',
      'We received a request to reset the password for your Family Heritage account.',
      `Open this link within ${ttlMinutes} minutes to choose a new password:`,
      '',
      resetUrl,
      '',
      'If you did not ask for this, you can ignore this e-mail — your password will not change.',
    ].join('\n'),
  };
}

export function passwordChangedEmail(to: string, displayName: string): MailMessage {
  return {
    to,
    subject: 'Your Family Heritage password was changed',
    text: [
      `Hello ${displayName},`,
      '',
      'The password for your Family Heritage account was just changed and all other sessions were signed out.',
      'If this was not you, reset your password immediately and contact your family administrator.',
    ].join('\n'),
  };
}
