import { Component, computed, inject, input, signal } from '@angular/core';
import { toast } from '@spartan-ng/brain/sonner';

import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmDialogImports } from '@spartan-ng/helm/dialog';
import { HlmInputImports } from '@spartan-ng/helm/input';
import { HlmPopoverImports } from '@spartan-ng/helm/popover';
import { HlmSpinnerImports } from '@spartan-ng/helm/spinner';

import { GroupMember } from '../../models/group.models';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { UserAvatarComponent } from '../../../../shared/components/user-avatar/user-avatar.component';
import { MemberCardService } from '../../../../core/services/member-card.service';
import { DailyService } from '../../../../core/services/daily.service';
import { AuthService } from '../../../../core/auth/auth.service';
import { NudgeStatus } from '../../../../core/models/daily.model';
import { EMPTY_INVENTORY, NUDGE_MESSAGE_MAX, NUDGE_PRICE } from '../../../../core/models/coin.model';
import { GlyphComponent } from '../../../../shared/components/glyph/glyph.component';
import { CoinTextComponent } from '../../../../shared/components/coin/coin-text.component';
import { apiErrorMessage } from '../../../../shared/utils/api-error';

const STACKED_AVATARS = 4;

// "3/6 han votado" (rama 36): who has voted today's question — never what they answered.
// A small avatar stack; tapping it lists who voted and who is still missing. Rama 43: next to
// each member still missing, "Avisar" reminds them by push (shown only when it can reach them),
// with an optional message.
@Component({
  selector: 'app-today-voters',
  imports: [
    HlmButtonImports,
    HlmDialogImports,
    HlmInputImports,
    HlmPopoverImports,
    HlmSpinnerImports,
    UserAvatarComponent,
    GlyphComponent,
    CoinTextComponent,
  ],
  template: `
    <hlm-popover [sideOffset]="6" align="end">
      <button
        hlmPopoverTrigger
        type="button"
        class="flex items-center gap-1.5 rounded-full py-0.5 pl-1 pr-1.5 text-xs text-muted-foreground transition-colors hover:bg-muted focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
        [attr.aria-label]="t('groupDetail.votersAria', { count: voted().length, total: total() })"
        (click)="loadTargets()"
      >
        @if (stacked().length > 0) {
          <span class="flex -space-x-1.5" aria-hidden="true">
            @for (m of stacked(); track m.id) {
              <app-user-avatar [avatarUrl]="m.avatarUrl" [username]="m.username" size="xs" />
            }
          </span>
        }
        <span class="tabular-nums">{{ t('groupDetail.votersShort', { count: voted().length, total: total() }) }}</span>
      </button>

      <hlm-popover-content *hlmPopoverPortal="let ctx" class="w-72 gap-3 p-3">
        @if (voted().length > 0) {
          <div class="flex flex-col gap-2">
            <p class="text-xs font-medium text-muted-foreground">{{ t('groupDetail.votersDone') }}</p>
            @for (m of voted(); track m.id) {
              <button
                type="button"
                class="flex items-center gap-2 rounded-default text-left text-sm text-foreground hover:bg-muted focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                (click)="openCard(m.id); ctx.close()"
              >
                <app-user-avatar [avatarUrl]="m.avatarUrl" [username]="m.username" [frameColor]="m.frameColor" [streak]="m.currentStreak" [crown]="m.hasCrown" size="xs" />
                <span class="truncate">{{ m.username }}</span>
              </button>
            }
          </div>
        }
        @if (pending().length > 0) {
          <div class="flex flex-col gap-2">
            <p class="text-xs font-medium text-muted-foreground">{{ t('groupDetail.votersPending') }}</p>
            @for (m of pending(); track m.id) {
              <div class="flex items-center gap-2">
                <button
                  type="button"
                  class="flex min-w-0 flex-1 items-center gap-2 rounded-default text-left text-sm text-muted-foreground hover:bg-muted focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                  (click)="openCard(m.id); ctx.close()"
                >
                  <app-user-avatar [avatarUrl]="m.avatarUrl" [username]="m.username" [frameColor]="m.frameColor" [streak]="m.currentStreak" [crown]="m.hasCrown" size="xs" />
                  <span class="truncate">{{ m.username }}</span>
                </button>
                <!-- Only where a nudge can go out now; nothing otherwise, to keep the list calm -->
                @if (statusOf(m.id) === 'available') {
                  <button
                    hlmBtn
                    size="sm"
                    variant="outline"
                    class="h-8 shrink-0 gap-1 px-2.5 focus-visible:ring-2 focus-visible:ring-ring"
                    [attr.aria-label]="t('nudge.actionAria', { name: m.username })"
                    (click)="openNudge(m); ctx.close()"
                  >
                    <app-glyph name="nudge" size="xs" bare />
                    {{ t('nudge.action') }}
                  </button>
                }
              </div>
            }
          </div>
        }
      </hlm-popover-content>
    </hlm-popover>

    <hlm-dialog [state]="nudgeTarget() ? 'open' : 'closed'" (closed)="closeNudge()">
      <hlm-dialog-content *hlmDialogPortal class="w-md max-w-[95vw]">
        @if (nudgeTarget(); as target) {
          <hlm-dialog-header>
            <h3 hlmDialogTitle class="flex items-center gap-2 font-display text-xl italic text-foreground">
              <app-glyph name="nudge" size="sm" />
              {{ t('nudge.title', { name: target.username }) }}
            </h3>
            <p hlmDialogDescription>{{ t('nudge.text') }}</p>
          </hlm-dialog-header>

          <div class="flex flex-col gap-1.5 py-2">
            <label for="nudge-message" class="text-sm font-medium text-foreground">{{ t('nudge.messageLabel') }}</label>
            <input
              hlmInput
              id="nudge-message"
              class="w-full"
              [maxLength]="maxLength"
              [placeholder]="t('nudge.placeholder')"
              [value]="message()"
              (input)="message.set($any($event.target).value)"
              (keydown.enter)="sendNudge()"
            />
            <p class="self-end text-2xs tabular-nums text-muted-foreground">{{ message().length }}/{{ maxLength }}</p>
            @if (nudgeError(); as error) {
              <p class="text-sm text-destructive" aria-live="polite">
                {{ error }}
                @if (missingCoins() > 0) {
                  · <app-coin-text [text]="t('streakFreeze.notEnough', { count: missingCoins() })" />
                }
              </p>
            }
          </div>

          <hlm-dialog-footer>
            <button
              hlmBtn
              class="gap-2 focus-visible:ring-2 focus-visible:ring-ring"
              [disabled]="sending()"
              (click)="sendNudge()"
            >
              @if (sending()) {
                <hlm-spinner class="size-4" />
              }
              @if (ownedNudges() > 0) {
                {{ t('nudge.sendOwned', { count: ownedNudges() }) }}
              } @else {
                <app-coin-text [text]="t('nudge.send', { price: nudgePrice })" />
              }
            </button>
          </hlm-dialog-footer>
        }
      </hlm-dialog-content>
    </hlm-dialog>
  `,
})
export class TodayVotersComponent {
  private readonly memberCard = inject(MemberCardService);
  private readonly dailyService = inject(DailyService);
  private readonly authService = inject(AuthService);
  protected readonly t = inject(I18nService).t;
  protected readonly nudgePrice = NUDGE_PRICE;
  protected readonly maxLength = NUDGE_MESSAGE_MAX;

  readonly groupId = input.required<number>();
  readonly members = input.required<GroupMember[]>();
  readonly voterIds = input<number[]>([]);
  // The group's size from daily/current, in case the member list is still loading.
  readonly memberCount = input(0);
  // The flash question this row belongs to; null = today's question (rama 43, for nudges).
  readonly flashId = input<number | null>(null);

  protected readonly voted = computed(() => {
    const ids = new Set(this.voterIds());
    return this.members().filter((m) => ids.has(m.id));
  });
  protected readonly pending = computed(() => {
    const ids = new Set(this.voterIds());
    return this.members().filter((m) => !ids.has(m.id));
  });
  protected readonly stacked = computed(() => this.voted().slice(0, STACKED_AVATARS));
  protected readonly total = computed(() => Math.max(this.memberCount(), this.members().length));

  // ── Toque / nudge (rama 43) ──
  // Loaded when the list opens; until then nobody shows a nudge button.
  private readonly targets = signal<ReadonlyMap<number, NudgeStatus>>(new Map());
  protected readonly nudgeTarget = signal<GroupMember | null>(null);
  protected readonly message = signal('');
  protected readonly sending = signal(false);
  protected readonly nudgeError = signal<string | null>(null);
  protected readonly ownedNudges = computed(
    () => (this.authService.currentUser()?.inventory ?? EMPTY_INVENTORY).nudge,
  );
  protected readonly missingCoins = computed(() =>
    this.ownedNudges() > 0 ? 0 : Math.max(0, NUDGE_PRICE - (this.authService.currentUser()?.coins ?? 0)),
  );

  openCard(userId: number): void {
    this.memberCard.open(this.groupId(), userId);
  }

  statusOf(userId: number): NudgeStatus | null {
    return this.targets().get(userId) ?? null;
  }

  loadTargets(): void {
    if (this.pending().length === 0) return;
    this.dailyService.getNudgeTargets(this.groupId(), this.flashId()).subscribe({
      next: (targets) => this.targets.set(new Map(targets.map((t) => [t.userId, t.status]))),
      error: () => this.targets.set(new Map()),
    });
  }

  openNudge(member: GroupMember): void {
    this.message.set('');
    this.nudgeError.set(null);
    this.nudgeTarget.set(member);
  }

  closeNudge(): void {
    if (!this.sending()) this.nudgeTarget.set(null);
  }

  sendNudge(): void {
    const target = this.nudgeTarget();
    if (!target || this.sending()) return;
    this.sending.set(true);
    this.nudgeError.set(null);
    this.dailyService
      .sendNudge(this.groupId(), {
        targetUserId: target.id,
        flashId: this.flashId(),
        message: this.message().trim() || null,
        useOwned: this.ownedNudges() > 0,
      })
      .subscribe({
        next: (res) => {
          this.sending.set(false);
          this.authService.patchCurrentUser({ coins: res.balance, inventory: res.inventory });
          this.targets.update((m) => new Map(m).set(target.id, 'recent'));
          this.nudgeTarget.set(null);
          toast.success(this.t('nudge.sent', { name: target.username }));
        },
        error: (err) => {
          this.sending.set(false);
          this.nudgeError.set(apiErrorMessage(err, this.t('nudge.failed')));
        },
      });
  }
}
