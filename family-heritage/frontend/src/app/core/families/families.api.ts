import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import type { ApiSuccess, FamilySummary, FamilyView } from '@fh/contracts';
import { api } from '../api/api';

@Injectable({ providedIn: 'root' })
export class FamiliesApi {
  private readonly http = inject(HttpClient);

  async listMine() {
    return (await firstValueFrom(this.http.get<ApiSuccess<FamilySummary[]>>(api('/families')))).data;
  }

  async get(familyId: string) {
    return (await firstValueFrom(this.http.get<ApiSuccess<FamilyView>>(api(`/families/${encodeURIComponent(familyId)}`)))).data;
  }
}
