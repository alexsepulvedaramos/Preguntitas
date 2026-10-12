import { Component, OnInit, inject } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';
import { toast } from '@spartan-ng/brain/sonner';
import { HeaderComponent } from './header/header.component';
import { FooterComponent } from './footer/footer.component';
import { HlmToasterImports } from '../../shared/ui/sonner/src';
import { TopLayerDirective } from '../../shared/directives/top-layer.directive';
import { MemberCardComponent } from '../../features/groups/components/member-card/member-card.component';
import { AuthService } from '../auth/auth.service';
import { ReleaseNotesService } from '../services/release-notes.service';
import { I18nService } from '../i18n/i18n.service';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [RouterOutlet, HeaderComponent, FooterComponent, HlmToasterImports, TopLayerDirective, MemberCardComponent],
  template: `
<div class="flex flex-col min-h-screen">
      <app-header></app-header>

      <main class="grow">
        <router-outlet></router-outlet>
      </main>

      <app-footer></app-footer>
    </div>
    <!-- appTopLayer: toasts must show above dialogs and drawers (CDK top-layer popovers) -->
    <hlm-toaster richColors appTopLayer />
    <app-member-card />
  `
})
export class MainLayoutComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly releaseNotes = inject(ReleaseNotesService);
  private readonly router = inject(Router);
  private readonly i18n = inject(I18nService);
  private readonly t = this.i18n.t;

  ngOnInit(): void {
    // "What's new" toast, once per release (rama 26). Delayed so the page settles first —
    // and so landing straight on /releases (which marks the release as seen) skips it.
    setTimeout(() => {
      if (!this.authService.isAuthenticated() || !this.releaseNotes.takeAnnouncement()) return;
      const { version, title } = this.releaseNotes.latest;
      toast(this.t('layout.releaseToast', { version }), {
        description: title[this.i18n.lang()],
        action: { label: this.t('layout.view'), onClick: () => this.router.navigate(['/releases']) },
      });
    }, 1500);
  }
}
