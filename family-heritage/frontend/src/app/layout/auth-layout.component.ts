import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { LogoComponent } from '../shared/ui/logo.component';

@Component({
  selector: 'fh-auth-layout',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, LogoComponent],
  template: `
    <div class="grid min-h-dvh lg:grid-cols-[minmax(0,5fr)_minmax(0,6fr)]">
      <section class="relative hidden overflow-hidden bg-brand-950 p-12 text-brand-50 lg:flex lg:flex-col" aria-hidden="true">
        <fh-logo [inverse]="true" />
        <div class="my-auto max-w-md">
          <p class="mb-4 text-sm font-semibold uppercase tracking-[0.2em] text-gold-300">Generation 1 → ∞</p>
          <h2 class="text-4xl leading-tight font-semibold text-white">Every family is a story worth keeping.</h2>
          <p class="mt-5 text-base leading-relaxed text-brand-200">
            Record your ancestors, trace every branch of descendants and preserve the photographs, stories and
            traditions that make your family who it is — privately, together.
          </p>
        </div>
        <!-- Decorative generation bands -->
        <div class="pointer-events-none absolute inset-x-0 bottom-0 flex h-40 flex-col justify-end gap-1.5 px-12 pb-12 opacity-90">
          @for (w of bands; track $index) {
            <div class="flex gap-1.5">
              @for (n of w; track $index) {
                <span class="h-2.5 flex-1 rounded-full" [style.background]="n"></span>
              }
            </div>
          }
        </div>
      </section>
      <main class="flex items-center justify-center px-4 py-10 sm:px-8">
        <div class="w-full max-w-md">
          <div class="mb-8 lg:hidden"><fh-logo /></div>
          <router-outlet />
        </div>
      </main>
    </div>
  `,
})
export class AuthLayoutComponent {
  protected readonly bands = [
    ['#f0cf6b'],
    ['#8ec3a2', '#8ec3a2', '#8ec3a2'],
    ['#5ea47b', '#5ea47b', '#5ea47b', '#5ea47b', '#5ea47b', '#5ea47b'],
    Array<string>(11).fill('#3d875d'),
  ];
}
