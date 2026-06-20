import { Component } from '@angular/core';

@Component({
    selector: 'app-logo-wordmark',
    standalone: true,
    template: `
    <div class="flex items-baseline gap-2">
    <span class="text-3xl font-display tracking-wider">
      <span class="text-xl  uppercase tracking-widest text-primary">VAYA</span>
      <span class="font-display italicleading-tight tracking-wide text-foreground">
        Preguntita
        <span class="text-logo-dot-1">.</span>
        <span class="text-logo-dot-2">.</span>
        <span class="text-logo-dot-3">.</span>
      </span>
</span>
    </div>
    <!-- <div class="flex-1 flex justify-center">
    <span class="text-2xl font-display tracking-wider">
        <span class="text-logo-v">VAYA </span>
        <span class="text-foreground italic">Preguntita</span>
    </span>
  </div> -->
        <!-- <div class="flex-1 flex justify-center">
    <span class="text-2xl font-display italic tracking-wider text-foreground">
      Vaya <span class="text-primary">Preguntita</span>
        <span class="text-logo-dot-1">.</span>
        <span class="text-logo-dot-2">.</span>
        <span class="text-logo-dot-3">.</span>
    </span>
  </div> -->

  `
})
export class LogoWordmarkComponent { }