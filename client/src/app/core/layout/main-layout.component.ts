import { Component, OnInit, inject } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';
import { toast } from '@spartan-ng/brain/sonner';
import { HeaderComponent } from './header/header.component';
import { FooterComponent } from './footer/footer.component';
import { HlmToasterImports } from '../../shared/ui/sonner/src';
import { MemberCardComponent } from '../../features/groups/components/member-card/member-card.component';
import { AuthService } from '../auth/auth.service';
import { ReleaseNotesService } from '../services/release-notes.service';

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
export class MainLayoutComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly releaseNotes = inject(ReleaseNotesService);
  private readonly router = inject(Router);

  ngOnInit(): void {
    // "What's new" toast, once per release (rama 26). Delayed so the page settles first —
    // and so landing straight on /releases (which marks the release as seen) skips it.
    setTimeout(() => {
      if (!this.authService.isAuthenticated() || !this.releaseNotes.takeAnnouncement()) return;
      const { version, title } = this.releaseNotes.latest;
      toast(`Novedades en la versión ${version}`, {
        description: title,
        action: { label: 'Ver', onClick: () => this.router.navigate(['/releases']) },
      });
    }, 1500);
  }
}
