import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideArrowRight, LucideDynamicIcon, LucideSprout, LucideTreePine } from '@lucide/angular';
import { FAMILY_ROLE_LABELS } from '@fh/contracts';
import { AuthStore } from '../../core/auth/auth.store';
import { FamilyContextStore } from '../../core/families/family-context.store';
import { NAV_ITEMS } from '../../core/navigation';
import { AlertComponent } from '../../shared/ui/alert.component';
import { SpinnerComponent } from '../../shared/ui/spinner.component';

@Component({
  selector: 'fh-dashboard-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, LucideDynamicIcon, LucideArrowRight, LucideSprout, LucideTreePine, AlertComponent, SpinnerComponent],
  template: `
    <header class="mb-8">
      <p class="text-sm font-medium text-brand-700 dark:text-brand-300">{{ greeting() }}</p>
      <h1 class="mt-1 text-3xl font-semibold sm:text-4xl">{{ auth.user()?.displayName }}</h1>
    </header>

    <section aria-labelledby="families-heading">
      <div class="mb-4 flex items-center justify-between">
        <h2 id="families-heading" class="text-xl font-semibold">Your families</h2>
      </div>

      @if (families.loading()) {
        <fh-spinner [showLabel]="true" label="Loading your families…" />
      } @else if (families.error(); as error) {
        <fh-alert>{{ error }}</fh-alert>
      } @else if (families.families().length === 0) {
        <div class="card relative overflow-hidden p-8 sm:p-10">
          <div class="absolute -right-10 -top-10 size-48 rounded-full bg-brand-50 dark:bg-brand-950" aria-hidden="true"></div>
          <div class="relative max-w-xl">
            <span class="mb-4 inline-flex size-12 items-center justify-center rounded-2xl bg-brand-100 text-brand-800 dark:bg-brand-900 dark:text-brand-100">
              <svg lucideSprout class="size-6"></svg>
            </span>
            <h3 class="text-2xl font-semibold">You are not part of a family tree yet</h3>
            <p class="mt-3 text-stone-600 dark:text-stone-400">
              When a relative invites you, their family will appear here. Registering a new family — with its founding
              ancestor, origin and history — arrives in the next release (Phase 2: Family Registry).
            </p>
          </div>
        </div>
      } @else {
        <ul class="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
          @for (f of families.families(); track f.id) {
            <li class="card flex flex-col p-5" [class.ring-2]="f.id === families.active()?.id" [class.ring-brand-500]="f.id === families.active()?.id">
              <div class="flex items-start gap-3">
                <span class="flex size-11 shrink-0 items-center justify-center rounded-xl bg-gen-1-tint text-gen-1-ink">
                  <svg lucideTreePine class="size-5"></svg>
                </span>
                <div class="min-w-0">
                  <h3 class="truncate text-lg font-semibold">{{ f.name }}</h3>
                  <p class="truncate text-sm text-stone-500">{{ originOf(f.origin) || 'Origin not recorded' }}</p>
                </div>
              </div>
              <dl class="mt-5 grid grid-cols-2 gap-3 text-sm">
                <div class="rounded-xl bg-stone-50 p-3 dark:bg-stone-800/60">
                  <dt class="text-stone-500">Members</dt>
                  <dd class="text-lg font-semibold">{{ f.memberCount }}</dd>
                </div>
                <div class="rounded-xl bg-stone-50 p-3 dark:bg-stone-800/60">
                  <dt class="text-stone-500">Generations</dt>
                  <dd class="text-lg font-semibold">{{ f.generationCount }}</dd>
                </div>
              </dl>
              <div class="mt-4 flex items-center justify-between">
                <span class="badge bg-brand-50 text-brand-800 dark:bg-brand-950 dark:text-brand-200">{{ roleLabel(f.role) }}</span>
                @if (f.id !== families.active()?.id) {
                  <button type="button" class="btn-ghost !px-2 !py-1 text-xs" (click)="families.select(f.id)">Make active</button>
                }
              </div>
            </li>
          }
        </ul>
      }
    </section>

    <section aria-labelledby="roadmap-heading" class="mt-12">
      <h2 id="roadmap-heading" class="mb-1 text-xl font-semibold">What's coming</h2>
      <p class="mb-5 text-sm text-stone-500">These features are being built in phases. Nothing below is active yet.</p>
      <ul class="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        @for (item of upcoming; track item.path) {
          <li>
            <a [routerLink]="item.path" class="card group flex h-full flex-col p-4 transition hover:shadow-(--shadow-raised)">
              <div class="flex items-center justify-between">
                <svg [lucideIcon]="item.icon" class="size-5 text-brand-700 dark:text-brand-300"></svg>
                <span class="badge bg-stone-100 text-stone-600 dark:bg-stone-800 dark:text-stone-300">Phase {{ item.phase }}</span>
              </div>
              <h3 class="mt-3 font-sans text-sm font-semibold">{{ item.label }}</h3>
              <p class="mt-1 flex-1 text-xs leading-relaxed text-stone-500">{{ item.description }}</p>
              <svg lucideArrowRight class="mt-3 size-4 text-stone-400 transition group-hover:translate-x-0.5"></svg>
            </a>
          </li>
        }
      </ul>
    </section>
  `,
})
export class DashboardPage {
  protected readonly auth = inject(AuthStore);
  protected readonly families = inject(FamilyContextStore);
  protected readonly upcoming = NAV_ITEMS.filter((i) => i.phase).sort((a, b) => a.phase! - b.phase!);

  protected readonly greeting = computed(() => {
    const h = new Date().getHours();
    return h < 12 ? 'Good morning' : h < 18 ? 'Good afternoon' : 'Good evening';
  });

  protected roleLabel(role: keyof typeof FAMILY_ROLE_LABELS) {
    return FAMILY_ROLE_LABELS[role];
  }

  protected originOf(o: { hometown: string; region: string; country: string }): string {
    return [o.hometown, o.region, o.country].filter(Boolean).join(', ');
  }
}
