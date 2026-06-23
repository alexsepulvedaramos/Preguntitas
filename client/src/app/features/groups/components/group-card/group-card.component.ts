import { Component, inject, input, signal } from '@angular/core';
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
  lucideCheck
} from '@ng-icons/lucide';

import { GroupResponse } from '../../models/group.models';

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
  providers: [provideIcons({ lucideUserPlus, lucideShare2, lucideCopy, lucideCheck })],
  templateUrl: './group-card.component.html',
  styleUrl: './group-card.component.css',
})
export class GroupCardComponent {
  private readonly router = inject(Router);

  public group = input.required<GroupResponse>();

  // Track independent clipboard feedback states
  public isLinkCopied = signal<boolean>(false);
  public isCodeCopied = signal<boolean>(false);

  // Keyboard-activated navigation (Enter), mirroring the routerLink-driven click
  navigateToDetail() {
    this.router.navigate(['/groups', this.group().id]);
  }

  // Generates the full invitation path
  getInviteLink(): string {
    return `${window.location.origin}/join/${this.group().id}`;
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
    const link = this.getInviteLink();
    const shareData: ShareData = {
      title: `Join ${this.group().name}`,
      text: `Join my group "${this.group().name}" using the invitation code: ${this.group().invitationCode}`,
      url: link,
    };

    if (navigator.share && navigator.canShare(shareData)) {
      try {
        await navigator.share(shareData);
      } catch (err) {
        console.error('Error executing native share: ', err);
      }
    } else {
      // Fallback behavior if the environment does not support native sharing
      this.copyLinkToClipboard();
    }
  }

  // Resets layout indicators when the dialog container closes
  resetCopiedStates() {
    this.isLinkCopied.set(false);
    this.isCodeCopied.set(false);
  }
}