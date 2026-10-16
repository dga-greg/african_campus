import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import type {
  ApiSuccess,
  AuthResponse,
  ChangePasswordRequest,
  LoginRequest,
  RegisterRequest,
  ResetPasswordRequest,
  SessionView,
  UpdateProfileRequest,
} from '@fh/contracts';
import { api } from '../api/api';

@Injectable({ providedIn: 'root' })
export class AuthApi {
  private readonly http = inject(HttpClient);

  async me() {
    return (await firstValueFrom(this.http.get<ApiSuccess<AuthResponse>>(api('/auth/me')))).data.user;
  }

  async login(body: LoginRequest) {
    return (await firstValueFrom(this.http.post<ApiSuccess<AuthResponse>>(api('/auth/login'), body))).data.user;
  }

  async register(body: RegisterRequest) {
    return (await firstValueFrom(this.http.post<ApiSuccess<AuthResponse>>(api('/auth/register'), body))).data.user;
  }

  refresh() {
    return this.http.post<ApiSuccess<AuthResponse>>(api('/auth/refresh'), {});
  }

  async logout() {
    await firstValueFrom(this.http.post<void>(api('/auth/logout'), {}));
  }

  async forgotPassword(email: string) {
    await firstValueFrom(this.http.post(api('/auth/forgot-password'), { email }));
  }

  async resetPassword(body: ResetPasswordRequest) {
    await firstValueFrom(this.http.post<void>(api('/auth/reset-password'), body));
  }

  async updateProfile(body: UpdateProfileRequest) {
    return (await firstValueFrom(this.http.patch<ApiSuccess<AuthResponse>>(api('/auth/me'), body))).data.user;
  }

  async changePassword(body: ChangePasswordRequest) {
    await firstValueFrom(this.http.post<void>(api('/auth/change-password'), body));
  }

  async sessions() {
    return (await firstValueFrom(this.http.get<ApiSuccess<SessionView[]>>(api('/auth/sessions')))).data;
  }

  async revokeSession(id: string) {
    await firstValueFrom(this.http.delete<void>(api(`/auth/sessions/${encodeURIComponent(id)}`)));
  }
}
