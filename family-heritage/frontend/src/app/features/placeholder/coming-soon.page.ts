import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { LucideDynamicIcon } from '@lucide/angular';
import { NAV_ITEMS } from '../../core/navigation';

/** An honest placeholder for features delivered in later phases — nothing here pretends to work. */
@Component({
  selector: 'fh-coming-soon-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, LucideDynamicIcon],
  template: `
    @if (item; as i) {
      <div class="card mx-auto max-w-2xl p-8 text-center sm:p-12">
        <span class="mx-auto mb-5 flex size-14 items-center justify-center rounded-2xl bg-brand-50 text-brand-700 dark:bg-brand-950 dark:text-brand-300">
          <svg [lucideIcon]="i.icon" class="size-7"></svg>
        </span>
        <p class="text-sm font-semibold uppercase tracking-widest text-gold-700 dark:text-gold-300">Coming in Phase {{ i.phase }}</p>
        <h1 class="mt-2 text-3xl font-semibold">{{ i.label }}</h1>
        <p class="mx-auto mt-4 max-w-md text-stone-600 dark:text-stone-400">{{ i.description }}</p>
        <p class="mx-auto mt-2 max-w-md text-sm text-stone-500">This section is not available yet.</p>
        <a routerLink="/dashboard" class="btn-secondary mt-8">Back to dashboard</a>
      </div>
    }
  `,
})
export class ComingSoonPage {
  protected readonly item = NAV_ITEMS.find((i) => i.path === `/${inject(ActivatedRoute).snapshot.routeConfig?.path ?? ''}`);
}
