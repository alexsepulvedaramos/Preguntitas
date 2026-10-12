import { Component, computed, inject, input, output, signal } from '@angular/core';
import { toast } from '@spartan-ng/brain/sonner';

import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmSpinnerImports } from '@spartan-ng/helm/spinner';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideLightbulb } from '@ng-icons/lucide';

import { AuthorGuess } from '../../../../core/models/daily.model';
import { AUTHOR_GUESS_REWARD, CoinsEarned, EMPTY_INVENTORY } from '../../../../core/models/coin.model';
import { DailyService } from '../../../../core/services/daily.service';
import { AuthService } from '../../../../core/auth/auth.service';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { GroupMember } from '../../models/group.models';
import { UserAvatarComponent } from '../../../../shared/components/user-avatar/user-avatar.component';
import { apiErrorMessage } from '../../../../shared/utils/api-error';

// "¿Quién la escribió?" (rama 36, §17.6): shown under today's question once you have voted on
// a member-written one. Pick a member and confirm, or skip to see the author; the answer is
// revealed at once. Leaving it alone keeps it pending until the question closes. The author
// sees how many people caught them instead.
@Component({
  selector: 'app-author-guess',
  imports: [HlmButtonImports, HlmSpinnerImports, NgIcon, UserAvatarComponent],
  providers: [provideIcons({ lucideLightbulb })],
  templateUrl: './author-guess.component.html',
})
export class AuthorGuessComponent {
  private readonly dailyService = inject(DailyService);
  private readonly authService = inject(AuthService);
  protected readonly t = inject(I18nService).t;

  readonly groupId = input.required<number>();
  readonly guess = input.required<AuthorGuess>();
  readonly members = input.required<GroupMember[]>();

  // The fresh guess state, for the parent to merge into daily/current.
  readonly changed = output<AuthorGuess>();
  // Coins a right guess paid, for the "+5 🪙" toast.
  readonly coinsEarned = output<CoinsEarned>();

  protected readonly reward = AUTHOR_GUESS_REWARD;
  protected readonly selectedId = signal<number | null>(null);
  protected readonly busy = signal<'guess' | 'skip' | 'hint' | null>(null);
  protected readonly hintError = signal<string | null>(null);

  protected readonly candidates = computed(() => {
    const ids = new Set(this.guess().candidateIds);
    return this.members().filter((m) => ids.has(m.id));
  });
  protected readonly balance = computed(() => this.authService.currentUser()?.coins ?? 0);
  protected readonly missingCoins = computed(() => Math.max(0, this.guess().hintPrice - this.balance()));
  // Hints bought ahead in the shop (rama 45) are used here without charging.
  protected readonly ownedHints = computed(
    () => (this.authService.currentUser()?.inventory ?? EMPTY_INVENTORY).authorHint,
  );
  protected readonly guessedName = computed(() => {
    const id = this.guess().guessedUserId;
    return this.members().find((m) => m.id === id)?.username ?? '';
  });

  select(id: number): void {
    if (this.busy()) return;
    this.selectedId.set(this.selectedId() === id ? null : id);
  }

  confirm(): void {
    const creatorId = this.selectedId();
    if (creatorId === null) return;
    this.send({ creatorId }, 'guess');
  }

  skip(): void {
    this.send({ skip: true }, 'skip');
  }

  buyHint(): void {
    if (this.busy()) return;
    this.busy.set('hint');
    this.hintError.set(null);
    this.dailyService.buyAuthorHint(this.groupId(), this.ownedHints() > 0).subscribe({
      next: (res) => {
        this.busy.set(null);
        this.authService.patchCurrentUser({ coins: res.balance, inventory: res.inventory });
        // Drop a pick the hint just ruled out.
        if (!res.guess.candidateIds.includes(this.selectedId() ?? -1)) this.selectedId.set(null);
        this.changed.emit(res.guess);
      },
      error: (err) => {
        this.busy.set(null);
        this.hintError.set(apiErrorMessage(err, this.t('guess.hintFailed')));
      },
    });
  }

  private send(dto: { creatorId?: number; skip?: boolean }, action: 'guess' | 'skip'): void {
    if (this.busy()) return;
    this.busy.set(action);
    this.dailyService.guessAuthor(this.groupId(), dto).subscribe({
      next: (res) => {
        this.busy.set(null);
        this.changed.emit(res.guess);
        this.coinsEarned.emit(res.coins);
      },
      error: (err) => {
        this.busy.set(null);
        toast.error(apiErrorMessage(err, this.t('guess.failed')));
      },
    });
  }
}
