import { Component, input, computed } from '@angular/core';
import { HlmAvatarImports } from '@spartan-ng/helm/avatar';

@Component({
  selector: 'app-user-avatar',
  standalone: true,
  imports: [HlmAvatarImports],
  template: `
    <div
      class="rounded-full shrink-0 overflow-hidden bg-card ring-1 ring-border/40"
      [class]="sizeClass()"
      [style.box-shadow]="frameColor() ? '0 0 0 3px ' + frameColor() : null"
    >
      <hlm-avatar [class]="sizeClass()">
        @if (avatarUrl()) {
          <img hlmAvatarImage [src]="avatarUrl()!" [alt]="username() + ' avatar'" class="object-cover w-full h-full" />
        }
        <span hlmAvatarFallback class="font-semibold bg-primary/15 text-primary" [class]="fallbackSizeClass()">
          @if (initials()) {
            {{ initials() }}
          } @else {
            ?
          }
        </span>
      </hlm-avatar>
    </div>
  `,
})
export class UserAvatarComponent {
  readonly avatarUrl = input<string | null | undefined>(null);
  readonly username = input<string>('');
  readonly frameColor = input<string | null | undefined>(null);
  readonly size = input<'xs' | 'sm' | 'md' | 'lg' | 'xl'>('md');

  protected readonly initials = computed(() =>
    this.username().slice(0, 2).toUpperCase()
  );

  protected readonly sizeClass = computed(() => ({
    xs: 'size-6',
    sm: 'size-8',
    md: 'size-10',
    lg: 'size-14',
    xl: 'size-24',
  })[this.size()]);

  protected readonly fallbackSizeClass = computed(() => ({
    xs: 'text-[9px]',
    sm: 'text-xs',
    md: 'text-sm',
    lg: 'text-lg',
    xl: 'text-2xl',
  })[this.size()]);
}
