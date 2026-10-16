import type { Request, Response } from 'express';
import type { AuthResponse, CsrfResponse } from '@fh/contracts';
import { parse, parseIdParam } from '../../core/validation.js';
import { authOf } from '../../middleware/authenticate.js';
import type { CsrfProtection } from '../../middleware/csrf.js';
import { toCurrentUser } from '../users/user.mapper.js';
import type { AuthResult, AuthService, ClientMeta } from './auth.service.js';
import { REFRESH_COOKIE, type AuthCookies } from './cookies.js';
import {
  changePasswordBody,
  forgotPasswordBody,
  loginBody,
  registerBody,
  resetPasswordBody,
  updateProfileBody,
} from './auth.validators.js';

function clientMeta(req: Request): ClientMeta {
  return { ip: req.ip ?? null, userAgent: req.get('user-agent') ?? null };
}

export class AuthController {
  constructor(
    private readonly auth: AuthService,
    private readonly cookies: AuthCookies,
    private readonly csrf: CsrfProtection,
  ) {}

  private respondWithSession(res: Response, result: AuthResult, status = 200): void {
    const { tokens } = result;
    this.cookies.setAccess(res, tokens.accessToken, tokens.accessExpiresAt);
    if (tokens.refresh) this.cookies.setRefresh(res, tokens.refresh.token, tokens.refresh.expiresAt);
    // A fresh CSRF token per session limits the lifetime of any leaked token.
    this.csrf.issue(res);
    const body: { data: AuthResponse } = { data: { user: toCurrentUser(result.user) } };
    res.status(status).json(body);
  }

  csrfToken = (_req: Request, res: Response) => {
    const body: { data: CsrfResponse } = { data: { token: this.csrf.tokenFor(res) } };
    res.json(body);
  };

  register = async (req: Request, res: Response) => {
    const input = parse(registerBody, req.body);
    this.respondWithSession(res, await this.auth.register(input, clientMeta(req)), 201);
  };

  login = async (req: Request, res: Response) => {
    const input = parse(loginBody, req.body);
    this.respondWithSession(res, await this.auth.login(input, clientMeta(req)));
  };

  refresh = async (req: Request, res: Response) => {
    const token: unknown = req.cookies?.[REFRESH_COOKIE];
    try {
      this.respondWithSession(res, await this.auth.refresh(typeof token === 'string' ? token : undefined, clientMeta(req)));
    } catch (err) {
      this.cookies.clear(res);
      throw err;
    }
  };

  logout = async (req: Request, res: Response) => {
    const token: unknown = req.cookies?.[REFRESH_COOKIE];
    await this.auth.logout(req.auth?.sessionId, typeof token === 'string' ? token : undefined);
    this.cookies.clear(res);
    res.status(204).end();
  };

  forgotPassword = async (req: Request, res: Response) => {
    const { email } = parse(forgotPasswordBody, req.body);
    await this.auth.forgotPassword(email, clientMeta(req));
    res.status(202).json({ data: { message: 'If an account exists for that address, a reset link has been sent.' } });
  };

  resetPassword = async (req: Request, res: Response) => {
    const { token, password } = parse(resetPasswordBody, req.body);
    await this.auth.resetPassword(token, password);
    this.cookies.clear(res);
    res.status(204).end();
  };

  me = (req: Request, res: Response) => {
    const body: { data: AuthResponse } = { data: { user: toCurrentUser(authOf(req).user) } };
    res.json(body);
  };

  updateMe = async (req: Request, res: Response) => {
    const patch = parse(updateProfileBody, req.body);
    const user = await this.auth.updateProfile(authOf(req).userId, patch);
    res.json({ data: { user: toCurrentUser(user) } });
  };

  changePassword = async (req: Request, res: Response) => {
    const { currentPassword, newPassword } = parse(changePasswordBody, req.body);
    const auth = authOf(req);
    await this.auth.changePassword(auth.userId, auth.sessionId, currentPassword, newPassword);
    res.status(204).end();
  };

  sessions = async (req: Request, res: Response) => {
    const auth = authOf(req);
    res.json({ data: await this.auth.listSessions(auth.userId, auth.sessionId) });
  };

  revokeSession = async (req: Request, res: Response) => {
    const auth = authOf(req);
    await this.auth.revokeSession(auth.userId, parseIdParam(req.params['sessionId'], 'Session'));
    res.status(204).end();
  };
}
