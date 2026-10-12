import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideArrowLeft, lucideStore } from '@ng-icons/lucide';

import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { HlmEmptyImports } from '@spartan-ng/helm/empty';
import { HlmItemImports } from '@spartan-ng/helm/item';
import { HlmSpinnerImports } from '@spartan-ng/helm/spinner';

import { AuthService } from '../../core/auth/auth.service';
import { UserService } from '../../core/services/user.service';
import { I18nService } from '../../core/i18n/i18n.service';
import { TranslationKey } from '../../core/i18n/translation.types';
import { COIN_DAILY_CAP, CoinReason, CoinTransaction } from '../../core/models/coin.model';

const REASON_LABELS: Record<CoinReason, TranslationKey> = {
  [CoinReason.Vote]: 'coins.reasons.vote',
  [CoinReason.FirstVoter]: 'coins.reasons.firstVoter',
  [CoinReason.StreakTier]: 'coins.reasons.streakTier',
  [CoinReason.StreakMilestone]: 'coins.reasons.streakMilestone',
  [CoinReason.SelectorChoice]: 'coins.reasons.selectorChoice',
  [CoinReason.SelectorCreated]: 'coins.reasons.selectorCreated',
  [CoinReason.StreakFreeze]: 'coins.reasons.streakFreeze',
  [CoinReason.StreakFreezeGift]: 'coins.reasons.streakFreezeGift',
  [CoinReason.StreakRepair]: 'coins.reasons.streakRepair',
  [CoinReason.GuessCorrect]: 'coins.reasons.guessCorrect',
  [CoinReason.AuthorHint]: 'coins.reasons.authorHint',
  [CoinReason.RaceWon]: 'coins.reasons.raceWon',
};

const REASON_EMOJI: Record<CoinReason, string> = {
  [CoinReason.Vote]: '🗳️',
  [CoinReason.FirstVoter]: '⚡',
  [CoinReason.StreakTier]: '🔥',
  [CoinReason.StreakMilestone]: '🎉',
  [CoinReason.SelectorChoice]: '👉',
  [CoinReason.SelectorCreated]: '✍️',
  [CoinReason.StreakFreeze]: '🧊',
  [CoinReason.StreakFreezeGift]: '🎁',
  [CoinReason.StreakRepair]: '🔧',
  [CoinReason.GuessCorrect]: '🕵️',
  [CoinReason.AuthorHint]: '💡',
  [CoinReason.RaceWon]: '⏱️',
};

// Coins & shop (rama 34, §17.2): the wallet, how coins are earned and the ledger. The
// catalog itself arrives with the shop ramas (§17.10).
@Component({
  selector: 'app-shop-page',
  imports: [NgIcon, HlmButtonImports, HlmCardImports, HlmEmptyImports, HlmItemImports, HlmSpinnerImports],
  providers: [provideIcons({ lucideArrowLeft, lucideStore })],
  templateUrl: './shop-page.component.html',
})
export class ShopPageComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly userService = inject(UserService);
  private readonly router = inject(Router);
  private readonly i18n = inject(I18nService);
  protected readonly t = this.i18n.t;
  protected readonly dailyCap = COIN_DAILY_CAP;

  protected readonly balance = computed(() => this.authService.currentUser()?.coins ?? 0);
  protected readonly items = signal<CoinTransaction[]>([]);
  protected readonly nextBefore = signal<number | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal(false);

  // Re-derived when the language changes.
  protected readonly rows = computed(() => {
    const format = new Intl.DateTimeFormat(this.i18n.locale(), { day: 'numeric', month: 'short' });
    return this.items().map(item => ({
      ...item,
      emoji: REASON_EMOJI[item.reason] ?? '🪙',
      label: this.t(REASON_LABELS[item.reason] ?? 'coins.reasons.vote'),
      date: format.format(new Date(item.createdAt)),
    }));
  });

  ngOnInit(): void {
    // Selector rewards are paid by the daily tick, so refresh the balance on entry.
    this.userService.getProfile().subscribe({
      next: profile => this.authService.patchCurrentUser({ coins: profile.coins }),
    });
    this.loadPage(null);
  }

  loadMore(): void {
    this.loadPage(this.nextBefore());
  }

  goBack(): void {
    this.router.navigate(['/groups']);
  }

  private loadPage(before: number | null): void {
    this.loading.set(true);
    this.error.set(false);
    this.userService.getCoinHistory(before).subscribe({
      next: page => {
        this.items.update(items => [...items, ...page.items]);
        this.nextBefore.set(page.nextBefore);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(true);
        this.loading.set(false);
      },
    });
  }
}
