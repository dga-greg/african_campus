import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import type { CurrentUser, LoginRequest, RegisterRequest } from '@fh/contracts';
import { AuthApi } from './auth.api';

export type AuthStatus = 'unknown' | 'authenticated' | 'anonymous';

/** The signed-in user. The access/refresh tokens live in HttpOnly cookies and are never visible here. */
@Injectable({ providedIn: 'root' })
export class AuthStore {
  private readonly authApi = inject(AuthApi);
  private readonly router = inject(Router);

  private readonly _user = signal<CurrentUser | null>(null);
  private readonly _status = signal<AuthStatus>('unknown');

  readonly user = this._user.asReadonly();
  readonly status = this._status.asReadonly();
  readonly isAuthenticated = computed(() => this._status() === 'authenticated');
  readonly isSuperAdmin = computed(() => this._user()?.platformRole === 'super_admin');

  /** Restores the session at start-up (the refresh interceptor renews an expired access token). */
  async init(): Promise<void> {
    try {
      this.setUser(await this.authApi.me());
    } catch {
      this.setUser(null);
    }
  }

  async login(body: LoginRequest): Promise<void> {
    this.setUser(await this.authApi.login(body));
  }

  async register(body: RegisterRequest): Promise<void> {
    this.setUser(await this.authApi.register(body));
  }

  async logout(): Promise<void> {
    try {
      await this.authApi.logout();
    } finally {
      this.setUser(null);
      await this.router.navigateByUrl('/auth/login');
    }
  }

  setUser(user: CurrentUser | null): void {
    this._user.set(user);
    this._status.set(user ? 'authenticated' : 'anonymous');
  }

  /** Called when the session cannot be renewed (expired, revoked elsewhere, account suspended). */
  sessionEnded(): void {
    if (this._status() !== 'authenticated') return;
    this.setUser(null);
    const returnUrl = this.router.url;
    void this.router.navigate(['/auth/login'], { queryParams: { returnUrl, reason: 'expired' } });
  }
}
