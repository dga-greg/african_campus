import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ApiError } from '../../core/api/api-error';
import { AuthStore } from '../../core/auth/auth.store';
import { safeReturnUrl } from '../../core/auth/guards';
import { AlertComponent } from '../../shared/ui/alert.component';
import { FormFieldComponent } from '../../shared/ui/form-field.component';

@Component({
  selector: 'fh-login-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink, FormFieldComponent, AlertComponent],
  template: `
    <h1 class="text-3xl font-semibold">Welcome back</h1>
    <p class="mt-2 text-sm text-stone-600 dark:text-stone-400">Sign in to continue to your family's history.</p>

    @if (expired) {
      <fh-alert tone="info" class="mt-6">Your session ended. Please sign in again.</fh-alert>
    }
    @if (error(); as e) {
      <fh-alert class="mt-6">{{ e.message }}</fh-alert>
    }

    <form [formGroup]="form" (ngSubmit)="submit()" class="mt-8 space-y-5" novalidate>
      <fh-form-field label="E-mail address" [control]="form.controls.email" #email>
        <input class="input" type="email" autocomplete="email" formControlName="email"
               [attr.id]="email.controlId" [attr.aria-describedby]="email.describedBy()"
               [attr.aria-invalid]="!!email.error()" />
      </fh-form-field>
      <fh-form-field label="Password" [control]="form.controls.password" #password>
        <input class="input" type="password" autocomplete="current-password" formControlName="password"
               [attr.id]="password.controlId" [attr.aria-describedby]="password.describedBy()"
               [attr.aria-invalid]="!!password.error()" />
      </fh-form-field>
      <div class="flex justify-end">
        <a routerLink="/auth/forgot-password" class="text-sm font-medium text-brand-700 hover:underline dark:text-brand-300">Forgot your password?</a>
      </div>
      <button type="submit" class="btn-primary w-full" [disabled]="busy()">
        {{ busy() ? 'Signing in…' : 'Sign in' }}
      </button>
    </form>

    <p class="mt-8 text-center text-sm text-stone-600 dark:text-stone-400">
      New here? <a routerLink="/auth/register" class="font-semibold text-brand-700 hover:underline dark:text-brand-300">Create an account</a>
    </p>
  `,
})
export class LoginPage {
  private readonly auth = inject(AuthStore);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly form = inject(NonNullableFormBuilder).group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });
  protected readonly busy = signal(false);
  protected readonly error = signal<ApiError | null>(null);
  protected readonly expired = this.route.snapshot.queryParamMap.get('reason') === 'expired';

  protected async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.busy.set(true);
    this.error.set(null);
    try {
      await this.auth.login(this.form.getRawValue());
      await this.router.navigateByUrl(safeReturnUrl(this.route.snapshot.queryParamMap.get('returnUrl')));
    } catch (err) {
      this.error.set(ApiError.from(err));
    } finally {
      this.busy.set(false);
    }
  }
}
