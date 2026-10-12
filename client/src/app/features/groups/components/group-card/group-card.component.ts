import { Component, computed, inject, input, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';

import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { HlmDialogImports } from '@spartan-ng/helm/dialog';
import { HlmInputImports } from '@spartan-ng/helm/input';
import { NgIcon, provideIcons } from '@ng-icons/core';
import {
  lucideUserPlus,
  lucideShare2,
  lucideCopy,
  lucideCheck,
  lucideClock,
  lucideFlame,
} from '@ng-icons/lucide';

import { GroupResponse } from '../../models/group.models';
import { inviteLink, shareInvite } from '../../utils/invite-share';
import { I18nService } from '../../../../core/i18n/i18n.service';

@Component({
  selector: 'app-group-card',
  imports: [
    RouterLink,
    HlmButtonImports,
    HlmCardImports,
    HlmDialogImports,
    HlmInputImports,
    NgIcon
  ],
  providers: [provideIcons({ lucideUserPlus, lucideShare2, lucideCopy, lucideCheck, lucideClock, lucideFlame })],
  templateUrl: './group-card.component.html',
  styleUrl: './group-card.component.css',
})
export class GroupCardComponent {
  private readonly router = inject(Router);
  protected readonly t = inject(I18nService).t;

  public group = input.required<GroupResponse>();

  private readonly ACCENT_COLORS = [
    'bg-group-1',
    'bg-group-2',
    'bg-group-3',
    'bg-group-4',
    'bg-group-5',
    'bg-group-6',
  ];

  private readonly BADGE_COLORS = [
    'bg-group-1/15 text-group-1',
    'bg-group-2/15 text-group-2',
    'bg-group-3/15 text-group-3',
    'bg-group-4/15 text-group-4',
    'bg-group-5/15 text-group-5',
    'bg-group-6/15 text-group-6',
  ];

  readonly accentColor = computed(
    () => this.ACCENT_COLORS[this.group().id % this.ACCENT_COLORS.length]
  );

  readonly accentBadgeClass = computed(
    () => this.BADGE_COLORS[this.group().id % this.BADGE_COLORS.length]
  );

  readonly nextQuestionTime = computed(() =>
    this.group().dailyQuestionTime?.slice(0, 5) ?? null
  );


  // Track independent clipboard feedback states
  public isLinkCopied = signal<boolean>(false);
  public isCodeCopied = signal<boolean>(false);

  // Keyboard-activated navigation (Enter), mirroring the routerLink-driven click
  navigateToDetail() {
    this.router.navigate(['/groups', this.group().id]);
  }

  // Generates the full invitation link using the invitation code
  getInviteLink(): string {
    return inviteLink(this.group().invitationCode);
  }

  // Copies the invitation code to the clipboard
  async copyCodeToClipboard() {
    const code = this.group().invitationCode || '';
    try {
      await navigator.clipboard.writeText(code);
      this.isCodeCopied.set(true);
      setTimeout(() => this.isCodeCopied.set(false), 3000);
    } catch (err) {
      console.error('Failed to copy code: ', err);
    }
  }

  // Copies the link to the clipboard
  async copyLinkToClipboard() {
    const link = this.getInviteLink();
    try {
      await navigator.clipboard.writeText(link);
      this.isLinkCopied.set(true);
      setTimeout(() => this.isLinkCopied.set(false), 3000);
    } catch (err) {
      console.error('Failed to copy link: ', err);
    }
  }

  // Triggers the device's native sharing context sheet
  async shareWithNativeApi() {
    const result = await shareInvite(this.group().name, this.group().invitationCode, this.t);
    if (result === 'copied') {
      this.isLinkCopied.set(true);
      setTimeout(() => this.isLinkCopied.set(false), 3000);
    }
  }

  // Resets layout indicators when the dialog container closes
  resetCopiedStates() {
    this.isLinkCopied.set(false);
    this.isCodeCopied.set(false);
  }
}