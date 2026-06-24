import { Component, DestroyRef, OnInit, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { fromEvent } from 'rxjs';
import { filter } from 'rxjs/operators';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideSend } from '@ng-icons/lucide';

import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmInputImports } from '@spartan-ng/helm/input';
import { HlmSpinnerImports } from '@spartan-ng/helm/spinner';

import { DailyService } from '../../../../core/services/daily.service';
import { ChatMessage } from '../../../../core/models/chat-message.model';

@Component({
  selector: 'app-group-chat',
  imports: [DatePipe, FormsModule, HlmButtonImports, HlmInputImports, HlmSpinnerImports, NgIcon],
  providers: [provideIcons({ lucideSend })],
  templateUrl: './group-chat.component.html',
})
export class GroupChatComponent implements OnInit {
  private readonly dailyService = inject(DailyService);
  private readonly destroyRef = inject(DestroyRef);

  public readonly groupId = input.required<number>();

  protected messages = signal<ChatMessage[]>([]);
  protected loading = signal(false);
  protected sending = signal(false);
  protected messageText = '';
  protected sendError = signal<string | null>(null);

  ngOnInit() {
    this.loadMessages();

    fromEvent(document, 'visibilitychange')
      .pipe(
        filter(() => document.visibilityState === 'visible'),
        takeUntilDestroyed(this.destroyRef),
      )
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
        this.sendError.set(err?.error?.error ?? 'No se ha podido enviar el mensaje.');
        this.sending.set(false);
      },
    });
  }

  private loadMessages(silent = false) {
    if (this.loading()) return;
    if (!silent) this.loading.set(true);

    this.dailyService.getChat(this.groupId()).subscribe({
      next: (messages) => {
        this.messages.set(messages);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      },
    });
  }
}
