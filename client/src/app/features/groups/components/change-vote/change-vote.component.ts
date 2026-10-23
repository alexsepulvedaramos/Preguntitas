import { Component, computed, inject, input, output, signal } from '@angular/core';
import { toast } from '@spartan-ng/brain/sonner';

import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmDialogImports } from '@spartan-ng/helm/dialog';
import { HlmSpinnerImports } from '@spartan-ng/helm/spinner';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideX } from '@ng-icons/lucide';

import { QuestionToVote } from '../../../../core/models/question.model';
import { CreateVote } from '../../../../core/models/vote.model';
import { ChangeVoteResponse } from '../../../../core/models/daily.model';
import { EMPTY_INVENTORY, VOTE_CHANGE_PRICE } from '../../../../core/models/coin.model';
import { DailyService } from '../../../../core/services/daily.service';
import { AuthService } from '../../../../core/auth/auth.service';
import { OneTimeHintsService } from '../../../../core/services/one-time-hints.service';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { GroupMember } from '../../models/group.models';
import { VoteFormComponent } from '../vote-form/vote-form.component';
import { GlyphComponent } from '../../../../shared/components/glyph/glyph.component';
import { CoinTextComponent } from '../../../../shared/components/coin/coin-text.component';
import { apiErrorMessage } from '../../../../shared/utils/api-error';

// "Cambiar voto" (rama 43, §17.10): under the results of an open question you voted, once per
// question, for VOTE_CHANGE_PRICE or one from the inventory. A one-off hint points at it the
// first time (§17.15: it must be easy to find). Tapping it opens the same vote form in a
// window; sending the new vote is the confirmation.
@Component({
  selector: 'app-change-vote',
  host: { class: 'flex flex-col gap-3' },
  imports: [HlmButtonImports, HlmDialogImports, HlmSpinnerImports, NgIcon, VoteFormComponent, GlyphComponent, CoinTextComponent],
  providers: [provideIcons({ lucideX })],
  template: `
    @if (canChange()) {
      @if (hintPending()) {
        <div class="relative flex w-full items-center gap-3 rounded-default border border-border bg-muted/50 py-2.5 pl-3 pr-11">
          <app-glyph name="changeVote" size="sm" />
          <p class="text-sm text-foreground text-balance">
            <span class="font-semibold">{{ t('changeVote.hintTitle') }}</span>
            <app-coin-text [text]="t('changeVote.hintText', { price })" />
          </p>
          <button
            hlmBtn
            variant="ghost"
            size="icon"
            class="absolute right-1.5 top-1/2 size-8 -translate-y-1/2 rounded-full focus-visible:ring-2 focus-visible:ring-ring"
            (click)="dismissHint()"
            [attr.aria-label]="t('groupDetail.dismissAlert')"
          >
            <ng-icon hlm name="lucideX" class="size-4!" />
          </button>
        </div>
      }
      <hlm-dialog class="contents" [state]="editing() ? 'open' : 'closed'" (stateChanged)="onStateChanged($event)">
        <!-- Small and grey under the results, so it reads as part of this question; outline
             because it is a purchase (§14) -->
        <button
          hlmBtn
          hlmDialogTrigger
          variant="outline"
          size="sm"
          class="-mt-1 mb-1 h-8 w-fit gap-1.5 self-end rounded-full px-3 text-xs text-muted-foreground transition-colors hover:text-foreground focus-visible:ring-2 focus-visible:ring-ring"
          (click)="start()"
        >
          <app-glyph name="changeVote" size="xs" bare />
          @if (owned() > 0) {
            {{ t('changeVote.actionOwned', { count: owned() }) }}
          } @else {
            <app-coin-text [text]="t('changeVote.action', { price })" />
          }
        </button>

        <hlm-dialog-content *hlmDialogPortal="let ctx" class="flex max-h-[85dvh] w-[95vw] max-w-md flex-col gap-4 overflow-y-auto">
          <hlm-dialog-header>
            <h3 hlmDialogTitle class="font-display text-xl italic text-foreground">{{ t('changeVote.title') }}</h3>
            <p hlmDialogDescription>{{ t('changeVote.text') }}</p>
          </hlm-dialog-header>

          <app-vote-form class="contents" [question]="question()" [members]="members()" [showQuestion]="false" (vote)="submit($event)" />

          @if (busy()) {
            <div class="flex items-center gap-2 text-sm text-muted-foreground">
              <hlm-spinner class="size-4" />
              {{ t('changeVote.sending') }}
            </div>
          }
          @if (error(); as message) {
            <p class="text-sm text-destructive" aria-live="polite">
              {{ message }}
              @if (missingCoins() > 0) {
                · <app-coin-text [text]="t('streakFreeze.notEnough', { count: missingCoins() })" />
              }
            </p>
          }

          <button
            hlmBtn
            variant="ghost"
            class="self-center focus-visible:ring-2 focus-visible:ring-ring"
            [disabled]="busy()"
            (click)="cancel()"
          >
            {{ t('changeVote.cancel') }}
          </button>
        </hlm-dialog-content>
      </hlm-dialog>
    }
  `,
})
export class ChangeVoteComponent {
  private readonly dailyService = inject(DailyService);
  private readonly authService = inject(AuthService);
  private readonly hints = inject(OneTimeHintsService);
  protected readonly t = inject(I18nService).t;
  protected readonly price = VOTE_CHANGE_PRICE;

  readonly groupId = input.required<number>();
  readonly question = input.required<QuestionToVote>();
  readonly members = input.required<GroupMember[]>();
  // null = today's question.
  readonly flashId = input<number | null>(null);
  readonly canChange = input(false);

  // The fresh results (daily) or flash state, for the parent to merge into daily/current.
  readonly changed = output<ChangeVoteResponse>();

  protected readonly editing = signal(false);
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly hintPending = signal(this.hints.isPending('changeVote'));

  protected readonly owned = computed(
    () => (this.authService.currentUser()?.inventory ?? EMPTY_INVENTORY).voteChange,
  );
  protected readonly missingCoins = computed(() =>
    this.owned() > 0 ? 0 : Math.max(0, this.price - (this.authService.currentUser()?.coins ?? 0)),
  );

  dismissHint(): void {
    this.hints.dismiss('changeVote');
    this.hintPending.set(false);
  }

  start(): void {
    this.dismissHint();
    this.error.set(null);
    this.editing.set(true);
  }

  onStateChanged(state: string): void {
    // Closing the window (outside tap, Esc) is the same as cancelling.
    if (state !== 'open') this.cancel();
  }

  cancel(): void {
    this.editing.set(false);
    this.error.set(null);
  }

  submit(dto: CreateVote): void {
    if (this.busy()) return;
    this.busy.set(true);
    this.error.set(null);
    this.dailyService.changeVote(this.groupId(), dto, this.flashId(), this.owned() > 0).subscribe({
      next: (res) => {
        this.busy.set(false);
        this.editing.set(false);
        this.authService.patchCurrentUser({ coins: res.balance, inventory: res.inventory });
        toast.success(this.t('changeVote.done'));
        this.changed.emit(res);
      },
      error: (err) => {
        this.busy.set(false);
        this.error.set(apiErrorMessage(err, this.t('changeVote.failed')));
      },
    });
  }
}
