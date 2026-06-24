import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideUsers } from '@ng-icons/lucide';

import { GroupsService } from '../../services/groups.service';
import { AuthService } from '../../../../core/auth/auth.service';
import { InvitePreview } from '../../models/group.models';

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
      },
      error: () => this.state.set('not_found'),
    });
  }

  get isAuthenticated(): boolean {
    return this.authService.isAuthenticated();
  }

  joinGroup(): void {
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/auth/login'], {
        queryParams: { returnUrl: `/join/${this.code}` },
      });
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
          this.errorMessage.set(msg || 'No se pudo unir al grupo. Inténtalo de nuevo.');
          this.state.set('error');
        }
      },
    });
  }

  goToGroups(): void {
    this.router.navigate(['/groups']);
  }
}
