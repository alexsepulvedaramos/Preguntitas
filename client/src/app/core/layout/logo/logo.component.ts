import { Component, Input } from '@angular/core';

@Component({
    selector: 'app-logo',
    standalone: true,
    template: `
    <div class="flex items-center" [class]="sizeClasses[size].wrapper">

      <!-- Icono -->
      <!-- <div class="bg-logo-bg rounded-lg flex flex-col items-center justify-center shrink-0"
           [class]="sizeClasses[size].icon">
        <div class="flex items-baseline leading-none" style="letter-spacing: -0.03em">
          <span class="font-display italic text-logo-v" [class]="sizeClasses[size].letter">V</span>
          <span class="font-display italic text-logo-q" [class]="sizeClasses[size].letter">?</span>
        </div>
        <div class="flex items-center" [class]="sizeClasses[size].dots">
          <span class="rounded-full block bg-logo-dot-1" [class]="sizeClasses[size].dot"></span>
          <span class="rounded-full block bg-logo-dot-2" [class]="sizeClasses[size].dot"></span>
          <span class="rounded-full block bg-logo-dot-3" [class]="sizeClasses[size].dot"></span>
        </div>
      </div> -->

      <!-- Wordmark -->
      <div class="flex flex-col">
        <span class="font-bold uppercase tracking-widest text-primary/60"
              [class]="sizeClasses[size].eyebrow">VAYA</span>
        <span class="font-display italic leading-tight tracking-wide text-foreground"
              [class]="sizeClasses[size].wordmark">
          Preguntita

        <span class="text-logo-dot-1">.</span>
        <span class="text-logo-dot-2">.</span>
        <span class="text-logo-dot-3">.</span>
        </span>
      </div>

    </div>
  `
})
export class LogoComponent {
    @Input() size: 'sm' | 'lg' = 'sm';

    sizeClasses = {
        sm: {
            wrapper: 'gap-3',
            icon: 'w-12 h-12 gap-1 pt-1.5 pb-2',
            letter: 'text-xl',
            dots: 'gap-1 mt-1',
            dot: 'w-1 h-1',
            eyebrow: 'text-xs',
            wordmark: 'text-2xl',
        },
        lg: {
            wrapper: 'gap-5',
            icon: 'w-20 h-20 gap-2 pt-2.5 pb-3',
            letter: 'text-4xl',
            dots: 'gap-1.5 mt-1.5',
            dot: 'w-1.5 h-1.5',
            eyebrow: 'text-sm',
            wordmark: 'text-5xl',
        }
    };
}