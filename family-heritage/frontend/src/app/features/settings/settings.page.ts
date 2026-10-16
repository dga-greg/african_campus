import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { LucideMonitor, LucideSmartphone } from '@lucide/angular';
import {
  DISPLAY_NAME_MAX_LENGTH,
  PASSWORD_MAX_LENGTH,
  PASSWORD_MIN_LENGTH,
  type SessionView,
  type ThemePreference,
} from '@fh/contracts';
import { ApiError } from '../../core/api/api-error';
import { AuthApi } from '../../core/auth/auth.api';
import { AuthStore } from '../../core/auth/auth.store';
import { AlertComponent } from '../../shared/ui/alert.component';
import { FormFieldComponent } from '../../shared/ui/form-field.component';
import { SpinnerComponent } from '../../shared/ui/spinner.component';

@Component({
  selector: 'fh-settings-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, DatePipe, FormFieldComponent, AlertComponent, SpinnerComponent, LucideMonitor, LucideSmartphone],
  template: `
    <h1 class="mb-8 text-3xl font-semibold">Settings</h1>
    <div class="grid gap-6 lg:grid-cols-2">
      <!-- Profile -->
      <section class="card p-6" aria-labelledby="profile-heading">
        <h2 id="profile-heading" class="text-lg font-semibold">Profile</h2>
        <p class="mt-1 text-sm text-stone-500">{{ auth.user()?.email }}</p>
        @if (profileMessage(); as m) {
          <fh-alert [tone]="m.tone" class="mt-4">{{ m.text }}</fh-alert>
        }
        <form [formGroup]="profile" (ngSubmit)="saveProfile()" class="mt-5 space-y-5" novalidate>
          <fh-form-field label="Display name" [control]="profile.controls.displayName" #dn>
            <input class="input" formControlName="displayName" autocomplete="name"
                   [attr.id]="dn.controlId" [attr.aria-describedby]="dn.describedBy()" [attr.aria-invalid]="!!dn.error()" />
          </fh-form-field>
          <fieldset>
            <legend class="label">Appearance</legend>
            <div class="grid grid-cols-3 gap-2">
              @for (t of themes; track t.value) {
                <label class="flex cursor-pointer items-center justify-center rounded-xl border border-stone-300 px-3 py-2 text-sm font-medium has-checked:border-brand-600 has-checked:bg-brand-50 has-checked:text-brand-800 dark:border-stone-700 dark:has-checked:bg-brand-950 dark:has-checked:text-brand-100">
                  <input type="radio" class="sr-only" formControlName="theme" [value]="t.value" />
                  {{ t.label }}
                </label>
              }
            </div>
          </fieldset>
          <button type="submit" class="btn-primary" [disabled]="busy() === 'profile'">Save profile</button>
        </form>
      </section>

      <!-- Password -->
      <section class="card p-6" aria-labelledby="password-heading">
        <h2 id="password-heading" class="text-lg font-semibold">Change password</h2>
        <p class="mt-1 text-sm text-stone-500">Other devices will be signed out.</p>
        @if (passwordMessage(); as m) {
          <fh-alert [tone]="m.tone" class="mt-4">{{ m.text }}</fh-alert>
        }
        <form [formGroup]="password" (ngSubmit)="changePassword()" class="mt-5 space-y-5" novalidate>
          <fh-form-field label="Current password" [control]="password.controls.currentPassword" [serverError]="passwordError()?.fieldError('currentPassword')" #cp>
            <input class="input" type="password" autocomplete="current-password" formControlName="currentPassword"
                   [attr.id]="cp.controlId" [attr.aria-describedby]="cp.describedBy()" [attr.aria-invalid]="!!cp.error()" />
          </fh-form-field>
          <fh-form-field label="New password" [control]="password.controls.newPassword" [serverError]="passwordError()?.fieldError('newPassword')" [hint]="'At least ' + minLength + ' characters.'" #np>
            <input class="input" type="password" autocomplete="new-password" formControlName="newPassword"
                   [attr.id]="np.controlId" [attr.aria-describedby]="np.describedBy()" [attr.aria-invalid]="!!np.error()" />
          </fh-form-field>
          <button type="submit" class="btn-primary" [disabled]="busy() === 'password'">Change password</button>
        </form>
      </section>

      <!-- Sessions -->
      <section class="card p-6 lg:col-span-2" aria-labelledby="sessions-heading">
        <h2 id="sessions-heading" class="text-lg font-semibold">Signed-in devices</h2>
        <p class="mt-1 text-sm text-stone-500">Sign out any device you do not recognise.</p>
        @if (sessions() === null) {
          <div class="mt-4"><fh-spinner [showLabel]="true" /></div>
        } @else {
          <ul class="mt-4 divide-y divide-stone-200 dark:divide-stone-800">
            @for (s of sessions(); track s.id) {
              <li class="flex flex-wrap items-center gap-3 py-3">
                @if (isMobile(s.userAgent)) {
                  <svg lucideSmartphone class="size-5 text-stone-400"></svg>
                } @else {
                  <svg lucideMonitor class="size-5 text-stone-400"></svg>
                }
                <div class="min-w-0 flex-1">
                  <p class="truncate text-sm font-medium">{{ describe(s.userAgent) }}
                    @if (s.current) { <span class="badge ml-1 bg-brand-50 text-brand-800 dark:bg-brand-950 dark:text-brand-200">This device</span> }
                  </p>
                  <p class="text-xs text-stone-500">Last active {{ s.lastUsedAt | date: 'medium' }} · {{ s.ip ?? 'unknown IP' }}</p>
                </div>
                @if (!s.current) {
                  <button type="button" class="btn-secondary !py-1.5 text-xs" (click)="revoke(s)">Sign out</button>
                }
              </li>
            }
          </ul>
        }
      </section>
    </div>
  `,
})
export class SettingsPage {
  protected readonly auth = inject(AuthStore);
  private readonly authApi = inject(AuthApi);
  private readonly fb = inject(NonNullableFormBuilder);
  protected readonly minLength = PASSWORD_MIN_LENGTH;
  protected readonly themes: { value: ThemePreference; label: string }[] = [
    { value: 'system', label: 'System' },
    { value: 'light', label: 'Light' },
    { value: 'dark', label: 'Dark' },
  ];

  protected readonly profile = this.fb.group({
    displayName: [this.auth.user()?.displayName ?? '', [Validators.required, Validators.maxLength(DISPLAY_NAME_MAX_LENGTH)]],
    theme: [this.auth.user()?.preferences.theme ?? ('system' as ThemePreference)],
  });
  protected readonly password = this.fb.group({
    currentPassword: ['', Validators.required],
    newPassword: ['', [Validators.required, Validators.minLength(PASSWORD_MIN_LENGTH), Validators.maxLength(PASSWORD_MAX_LENGTH)]],
  });

  protected readonly busy = signal<'profile' | 'password' | null>(null);
  protected readonly profileMessage = signal<{ tone: 'success' | 'error'; text: string } | null>(null);
  protected readonly passwordMessage = signal<{ tone: 'success' | 'error'; text: string } | null>(null);
  protected readonly passwordError = signal<ApiError | null>(null);
  protected readonly sessions = signal<SessionView[] | null>(null);

  constructor() {
    void this.loadSessions();
  }

  private async loadSessions(): Promise<void> {
    try {
      this.sessions.set(await this.authApi.sessions());
    } catch {
      this.sessions.set([]);
    }
  }

  protected async saveProfile(): Promise<void> {
    if (this.profile.invalid) return this.profile.markAllAsTouched();
    this.busy.set('profile');
    try {
      const { displayName, theme } = this.profile.getRawValue();
      this.auth.setUser(await this.authApi.updateProfile({ displayName, preferences: { theme } }));
      this.profileMessage.set({ tone: 'success', text: 'Profile saved.' });
    } catch (err) {
      this.profileMessage.set({ tone: 'error', text: ApiError.from(err).message });
    } finally {
      this.busy.set(null);
    }
  }

  protected async changePassword(): Promise<void> {
    if (this.password.invalid) return this.password.markAllAsTouched();
    this.busy.set('password');
    this.passwordError.set(null);
    this.passwordMessage.set(null);
    try {
      await this.authApi.changePassword(this.password.getRawValue());
      this.password.reset();
      this.passwordMessage.set({ tone: 'success', text: 'Password changed. Other devices have been signed out.' });
      await this.loadSessions();
    } catch (err) {
      const e = ApiError.from(err);
      this.passwordError.set(e);
      if (!e.details.length) this.passwordMessage.set({ tone: 'error', text: e.message });
    } finally {
      this.busy.set(null);
    }
  }

  protected async revoke(session: SessionView): Promise<void> {
    await this.authApi.revokeSession(session.id);
    this.sessions.update((list) => list?.filter((s) => s.id !== session.id) ?? null);
  }

  protected isMobile(ua: string | null): boolean {
    return !!ua && /Mobi|Android|iPhone|iPad/i.test(ua);
  }

  protected describe(ua: string | null): string {
    if (!ua) return 'Unknown device';
    const browser = /Edg\//.test(ua) ? 'Edge' : /Chrome\//.test(ua) ? 'Chrome' : /Firefox\//.test(ua) ? 'Firefox' : /Safari\//.test(ua) ? 'Safari' : 'Browser';
    const os = /Windows/.test(ua) ? 'Windows' : /Android/.test(ua) ? 'Android' : /iPhone|iPad/.test(ua) ? 'iOS' : /Mac OS/.test(ua) ? 'macOS' : /Linux/.test(ua) ? 'Linux' : 'unknown OS';
    return `${browser} on ${os}`;
  }
}
