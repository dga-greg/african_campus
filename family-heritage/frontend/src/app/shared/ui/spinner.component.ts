import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'fh-spinner',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { role: 'status', class: 'inline-flex items-center gap-2 text-sm text-stone-500' },
  template: `
    <span class="size-4 animate-spin rounded-full border-2 border-current border-t-transparent" aria-hidden="true"></span>
    <span [class.sr-only]="!showLabel()">{{ label() }}</span>
  `,
})
export class SpinnerComponent {
  readonly label = input('Loading…');
  readonly showLabel = input(false);
}
