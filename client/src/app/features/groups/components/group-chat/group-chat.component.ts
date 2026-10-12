import { Component, DestroyRef, Injector, OnInit, afterNextRender, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, of } from 'rxjs';
import { catchError, exhaustMap, map } from 'rxjs/operators';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideSend } from '@ng-icons/lucide';

import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmInputImports } from '@spartan-ng/helm/input';
import { HlmSpinnerImports } from '@spartan-ng/helm/spinner';

import { DailyService } from '../../../../core/services/daily.service';
import { ChatMessage } from '../../../../core/models/chat-message.model';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { visiblePolling } from '../../../../shared/utils/visible-polling';

export const CHAT_POLL_INTERVAL_MS = 15_000;

// Within this many px of the page bottom counts as "already at the bottom".
const AT_BOTTOM_THRESHOLD_PX = 80;

@Component({
  selector: 'app-group-chat',
  imports: [DatePipe, FormsModule, HlmButtonImports, HlmInputImports, HlmSpinnerImports, NgIcon],
  providers: [provideIcons({ lucideSend })],
  templateUrl: './group-chat.component.html',
})
export class GroupChatComponent implements OnInit {
  private readonly dailyService = inject(DailyService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);
  protected readonly t = inject(I18nService).t;

  public readonly groupId = input.required<number>();

  protected messages = signal<ChatMessage[]>([]);
  protected loading = signal(false);
  protected sending = signal(false);
  protected messageText = '';
  protected sendError = signal<string | null>(null);

  // Every fetch (initial, manual, focus, polling) goes through one exhaustMap so requests
  // never overlap. `true` = silent (no spinner).
  private readonly fetchRequests = new Subject<boolean>();

  ngOnInit() {
    this.fetchRequests
      .pipe(
        exhaustMap((silent) => {
          if (!silent) this.loading.set(true);
          return this.dailyService.getChat(this.groupId()).pipe(
            map((messages) => ({ messages })),
            catchError(() => of(null)),
          );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((res) => {
        if (res) this.applyMessages(res.messages);
        this.loading.set(false);
      });

    this.loadMessages();

    // Live refresh (spec §10): poll while visible; coming back to the tab refreshes at once.
    visiblePolling(CHAT_POLL_INTERVAL_MS)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.loadMessages(true));
  }

  reload() {
    this.loadMessages(true);
  }

  onInputFocus() {
    this.loadMessages(true);
  }

  sendMessage() {
    const body = this.messageText.trim();
    if (!body || this.sending()) return;

    this.sending.set(true);
    this.sendError.set(null);

    this.dailyService.sendChatMessage(this.groupId(), body).subscribe({
      next: (messages) => {
        this.messages.set(messages);
        this.messageText = '';
        this.sending.set(false);
      },
      error: (err) => {
        // The API answers in the UI language (Accept-Language)
        this.sendError.set(err?.error?.error ?? this.t('chat.sendError'));
        this.sending.set(false);
      },
    });
  }

  private loadMessages(silent = false) {
    this.fetchRequests.next(silent);
  }

  // Skips the update when nothing changed, and keeps the reader at the bottom only if they
  // were already there (otherwise the scroll position is left alone). The input text is
  // never touched.
  private applyMessages(messages: ChatMessage[]) {
    const current = this.messages();
    const unchanged =
      current.length === messages.length && current[current.length - 1]?.id === messages[messages.length - 1]?.id;
    if (unchanged) return;

    const wasAtBottom = this.isAtBottom();
    this.messages.set(messages);
    if (wasAtBottom && messages.length > current.length) {
      afterNextRender(() => window.scrollTo({ top: document.documentElement.scrollHeight }), {
        injector: this.injector,
      });
    }
  }

  private isAtBottom(): boolean {
    const el = document.documentElement;
    return window.innerHeight + window.scrollY >= el.scrollHeight - AT_BOTTOM_THRESHOLD_PX;
  }
}
