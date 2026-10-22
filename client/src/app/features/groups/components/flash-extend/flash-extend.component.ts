import { Component, computed, inject, input, output, signal } from '@angular/core';
import { toast } from '@spartan-ng/brain/sonner';

import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmSpinnerImports } from '@spartan-ng/helm/spinner';
import { HlmAlertDialogImports } from '@spartan-ng/helm/alert-dialog';

import { FlashState } from '../../../../core/models/daily.model';
import { FLASH_EXTENSION_PRICE } from '../../../../core/models/coin.model';
import { DailyService } from '../../../../core/services/daily.service';
import { AuthService } from '../../../../core/auth/auth.service';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { GlyphComponent } from '../../../../shared/components/glyph/glyph.component';
import { CoinTextComponent } from '../../../../shared/components/coin/coin-text.component';
import { apiErrorMessage } from '../../../../shared/utils/api-error';

// "+24 h" on an open flash (rama 50, §17.15 item 6): any member, once per flash, for
// FLASH_EXTENSION_PRICE. Sits on the flash card's time-left line; once bought it says who
// extended it (just "Ampliada" on an anonymous flash).
@Component({
  selector: 'app-flash-extend',
  imports: [HlmButtonImports, HlmSpinnerImports, HlmAlertDialogImports, GlyphComponent, CoinTextComponent],
  template: `
    @if (flash().extended) {
      <span class="inline-flex items-center gap-1 text-xs text-muted-foreground">
        <app-glyph name="extend" size="xs" bare />
        {{ flash().extendedBy ? t('flash.extendedBy', { name: flash().extendedBy!.username }) : t('flash.extended') }}
      </span>
    } @else {
      <hlm-alert-dialog #dlg="hlmAlertDialog">
        <button
          type="button"
          hlmBtn
          variant="outline"
          size="xs"
          class="gap-1 rounded-full transition-colors focus-visible:ring-2 focus-visible:ring-ring"
          [disabled]="busy()"
          [attr.aria-label]="t('flash.extendLabel', { price })"
          (click)="dlg.open()"
        >
          @if (busy()) {
            <hlm-spinner class="size-3" />
          } @else {
            <app-glyph name="extend" size="xs" bare />
          }
          <app-coin-text [text]="t('flash.extend', { price })" />
        </button>
        <ng-template hlmAlertDialogPortal>
          <hlm-alert-dialog-content>
            <hlm-alert-dialog-header>
              <h3 hlmAlertDialogTitle>
                <app-coin-text [text]="t('flash.extendConfirmTitle', { price })" />
              </h3>
              <p hlmAlertDialogDescription>{{ t('flash.extendConfirmText') }}</p>
            </hlm-alert-dialog-header>
            @if (missing() > 0) {
              <p class="text-sm text-muted-foreground">
                <app-coin-text [text]="t('flash.extendMissing', { count: missing() })" />
              </p>
            }
            <hlm-alert-dialog-footer>
              <button hlmAlertDialogCancel>{{ t('common.cancel') }}</button>
              <button hlmAlertDialogAction class="disabled:opacity-60" [disabled]="missing() > 0" (click)="dlg.close(); extend()">
                {{ t('flash.extendConfirm') }}
              </button>
            </hlm-alert-dialog-footer>
          </hlm-alert-dialog-content>
        </ng-template>
      </hlm-alert-dialog>
    }
  `,
})
export class FlashExtendComponent {
  private readonly dailyService = inject(DailyService);
  private readonly authService = inject(AuthService);
  protected readonly t = inject(I18nService).t;
  protected readonly price = FLASH_EXTENSION_PRICE;

  public readonly groupId = input.required<number>();
  public readonly flash = input.required<FlashState>();
  public readonly extended = output<FlashState>();

  protected readonly busy = signal(false);
  protected readonly missing = computed(() =>
    Math.max(0, FLASH_EXTENSION_PRICE - (this.authService.currentUser()?.coins ?? 0)),
  );

  extend() {
    this.busy.set(true);
    this.dailyService.extendFlash(this.groupId(), this.flash().id).subscribe({
      next: (res) => {
        this.busy.set(false);
        this.authService.patchCurrentUser({ coins: res.balance });
        toast.success(this.t('flash.extendedToast'));
        this.extended.emit(res.flash);
      },
      error: (err) => {
        this.busy.set(false);
        toast.error(apiErrorMessage(err, this.t('flash.extendFailed')));
      },
    });
  }
}
