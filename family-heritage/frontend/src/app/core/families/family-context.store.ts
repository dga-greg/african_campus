import { Injectable, computed, inject, signal } from '@angular/core';
import { canFamily, type FamilyPermission, type FamilySummary } from '@fh/contracts';
import { ApiError } from '../api/api-error';
import { AuthApi } from '../auth/auth.api';
import { AuthStore } from '../auth/auth.store';
import { FamiliesApi } from './families.api';

/** The families the user belongs to and the one currently being worked on. */
@Injectable({ providedIn: 'root' })
export class FamilyContextStore {
  private readonly familiesApi = inject(FamiliesApi);
  private readonly authApi = inject(AuthApi);
  private readonly auth = inject(AuthStore);

  private readonly _families = signal<FamilySummary[]>([]);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);
  private readonly _selectedId = signal<string | null>(null);

  readonly families = this._families.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();

  readonly active = computed<FamilySummary | null>(() => {
    const families = this._families();
    const preferred = this._selectedId() ?? this.auth.user()?.preferences.defaultFamilyId ?? null;
    return families.find((f) => f.id === preferred) ?? families[0] ?? null;
  });

  /** UI hint only; the server re-checks every permission. */
  can(permission: FamilyPermission): boolean {
    const family = this.active();
    return family ? canFamily({ role: family.role }, permission) : false;
  }

  async load(): Promise<void> {
    this._loading.set(true);
    this._error.set(null);
    try {
      this._families.set(await this.familiesApi.listMine());
    } catch (err) {
      this._error.set(ApiError.from(err).message);
    } finally {
      this._loading.set(false);
    }
  }

  async select(familyId: string): Promise<void> {
    this._selectedId.set(familyId);
    try {
      this.auth.setUser(await this.authApi.updateProfile({ preferences: { defaultFamilyId: familyId } }));
    } catch {
      // The selection still applies for this session; persisting it is best-effort.
    }
  }

  clear(): void {
    this._families.set([]);
    this._selectedId.set(null);
  }
}
