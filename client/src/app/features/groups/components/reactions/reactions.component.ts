import { Component, computed, inject, input, linkedSignal, output, signal } from '@angular/core';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideSmilePlus } from '@ng-icons/lucide';
import { toast } from '@spartan-ng/brain/sonner';

import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmPopoverImports } from '@spartan-ng/helm/popover';

import { AuthService } from '../../../../core/auth/auth.service';
import { I18nService } from '../../../../core/i18n/i18n.service';
import {
  REACTION_EMOJIS,
  REACTION_KEYS,
  Reaction,
  ReactionKey,
  ReactionTarget,
} from '../../../../core/models/reaction.model';
import { DailyService } from '../../../../core/services/daily.service';
import { UserAvatarComponent } from '../../../../shared/components/user-avatar/user-avatar.component';
import { UserNameComponent } from '../../../../shared/components/user-name/user-name.component';
import { GroupStreaksService } from '../../services/group-streaks.service';

interface ReactionGroup {
  key: ReactionKey;
  emoji: string;
  count: number;
  mine: boolean;
}

// Reactions under a chat message or a player-written answer (rama 48, §17.15 item 6). One
// button shows the emoji counts (or a "react" icon when there are none) and opens a popover
// with the seven emojis — picking another replaces yours, picking yours removes it — and who
// reacted with what, with their avatar and name as everywhere else in the group (§14). Never
// on your own message or answer: there it only lists them.
@Component({
  selector: 'app-reactions',
  imports: [HlmButtonImports, HlmPopoverImports, NgIcon, UserAvatarComponent, UserNameComponent],
  providers: [provideIcons({ lucideSmilePlus })],
  templateUrl: './reactions.component.html',
})
export class ReactionsComponent {
  private readonly dailyService = inject(DailyService);
  private readonly authService = inject(AuthService);
  protected readonly groupStreaks = inject(GroupStreaksService);
  protected readonly t = inject(I18nService).t;

  public readonly groupId = input.required<number>();
  public readonly target = input.required<ReactionTarget>();
  // Who wrote the message or answer.
  public readonly authorId = input.required<number | null>();
  public readonly reactions = input.required<Reaction[]>();
  // The target's reactions after a change of ours, for a parent that lays them out itself.
  public readonly changed = output<Reaction[]>();

  // What the server last sent: the input from the poll, or the answer to our own change.
  protected readonly current = linkedSignal(() => this.reactions());
  protected readonly saving = signal(false);

  protected readonly keys = REACTION_KEYS;
  protected readonly emojis = REACTION_EMOJIS;

  private readonly userId = computed(() => this.authService.currentUser()?.id ?? null);
  protected readonly canReact = computed(() => this.userId() != null && this.authorId() !== this.userId());
  protected readonly mine = computed(() => this.current().find((r) => r.userId === this.userId())?.emoji ?? null);

  protected readonly groups = computed<ReactionGroup[]>(() =>
    REACTION_KEYS.map((key) => {
      const of = this.current().filter((r) => r.emoji === key);
      return { key, emoji: REACTION_EMOJIS[key], count: of.length, mine: of.some((r) => r.userId === this.userId()) };
    }).filter((g) => g.count > 0),
  );

  protected readonly summary = computed(() => this.groups().map((g) => `${g.emoji} ${g.count}`).join(', '));

  pick(key: ReactionKey) {
    if (this.saving()) return;
    const emoji = this.mine() === key ? null : key;
    this.saving.set(true);
    this.dailyService.setReaction(this.groupId(), this.target(), emoji).subscribe({
      next: (reactions) => {
        this.current.set(reactions);
        this.changed.emit(reactions);
        this.saving.set(false);
      },
      error: (err) => {
        // The API answers in the UI language (Accept-Language)
        toast.error(typeof err?.error === 'string' ? err.error : this.t('reactions.error'));
        this.saving.set(false);
      },
    });
  }
}
