import type { CookieOptions, Response } from 'express';

export const ACCESS_COOKIE = 'fh_access';
export const REFRESH_COOKIE = 'fh_refresh';
export const ACCESS_COOKIE_PATH = '/api';
export const REFRESH_COOKIE_PATH = '/api/v1/auth';

export class AuthCookies {
  constructor(private readonly secure: boolean) {}

  get base(): CookieOptions {
    return { httpOnly: true, secure: this.secure, sameSite: 'strict' };
  }

  setAccess(res: Response, token: string, expiresAt: Date): void {
    res.cookie(ACCESS_COOKIE, token, { ...this.base, path: ACCESS_COOKIE_PATH, expires: expiresAt });
  }

  setRefresh(res: Response, token: string, expiresAt: Date): void {
    res.cookie(REFRESH_COOKIE, token, { ...this.base, path: REFRESH_COOKIE_PATH, expires: expiresAt });
  }

  clear(res: Response): void {
    res.clearCookie(ACCESS_COOKIE, { ...this.base, path: ACCESS_COOKIE_PATH });
    res.clearCookie(REFRESH_COOKIE, { ...this.base, path: REFRESH_COOKIE_PATH });
  }
}
