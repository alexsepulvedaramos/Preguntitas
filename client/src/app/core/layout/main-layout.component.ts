import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { HeaderComponent } from './header/header.component';
import { FooterComponent } from './footer/footer.component';
import { HlmToasterImports } from '../../shared/ui/sonner/src';
import { MemberCardComponent } from '../../features/groups/components/member-card/member-card.component';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [RouterOutlet, HeaderComponent, FooterComponent, HlmToasterImports, MemberCardComponent],
  template: `
<div class="flex flex-col min-h-screen">
      <app-header></app-header>

      <main class="grow">
        <router-outlet></router-outlet>
      </main>

      <app-footer></app-footer>
    </div>
    <hlm-toaster richColors />
    <app-member-card />
  `
})
export class MainLayoutComponent { }