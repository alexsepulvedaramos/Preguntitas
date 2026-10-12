import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideUsers } from '@ng-icons/lucide';

import { GroupsService } from '../../services/groups.service';
import { AuthService } from '../../../../core/auth/auth.service';
import { InvitePreview } from '../../models/group.models';
import { I18nService } from '../../../../core/i18n/i18n.service';

type PageState = 'loading' | 'preview' | 'joining' | 'already_member' | 'not_found' | 'error';

@Component({
  selector: 'app-join-by-link',
  imports: [HlmButtonImports, HlmCardImports, NgIcon],
  providers: [provideIcons({ lucideUsers })],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './join-by-link.component.html',
})
export class JoinByLinkComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly groupsService = inject(GroupsService);
  private readonly authService = inject(AuthService);
  protected readonly t = inject(I18nService).t;

  readonly state = signal<PageState>('loading');
  readonly preview = signal<InvitePreview | null>(null);
  readonly errorMessage = signal<string>('');

  private code = '';

  ngOnInit(): void {
    this.code = this.route.snapshot.paramMap.get('code') ?? '';
    if (!this.code) {
      this.state.set('not_found');
      return;
    }

    // Hydrate currentUser from a non-expired stored token (no refresh, no HTTP).
    // verifySession() must not be called here because it can trigger token rotation,
    // which races with the guest guard or auth guard if the user navigates away.
    this.authService.hydrateUser();

    this.groupsService.getInvitePreview(this.code).subscribe({
      next: (data) => {
        this.preview.set(data);
        this.state.set('preview');
        // Back from login/register with the invite: finish the join without another tap.
        if (this.route.snapshot.queryParamMap.get('join') === '1' && this.authService.isAuthenticated()) {
          this.joinGroup();
        }
      },
      error: () => this.state.set('not_found'),
    });
  }

  get isAuthenticated(): boolean {
    return this.authService.isAuthenticated();
  }

  // Guests sign up (most invitees are new) or log in, then come back here and join automatically.
  authenticateAndJoin(mode: 'register' | 'login'): void {
    this.router.navigate(['/auth', mode], {
      queryParams: { returnUrl: `/join/${this.code}?join=1` },
    });
  }

  joinGroup(): void {
    if (!this.authService.isAuthenticated()) {
      this.authenticateAndJoin('login');
      return;
    }

    this.state.set('joining');
    this.groupsService.joinGroup({ invitationCode: this.code }).subscribe({
      next: () => {
        const groupId = this.preview()?.id;
        this.router.navigate(groupId ? ['/groups', groupId] : ['/groups']);
      },
      error: (err) => {
        const msg: string = err.error?.message ?? '';
        if (msg.toLowerCase().includes('already')) {
          this.state.set('already_member');
        } else {
          this.errorMessage.set(msg || this.t('groups.joinLink.failed'));
          this.state.set('error');
        }
      },
    });
  }

  goToGroups(): void {
    this.router.navigate(['/groups']);
  }
}
