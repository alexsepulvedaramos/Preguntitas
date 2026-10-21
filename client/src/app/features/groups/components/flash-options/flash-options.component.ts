import { Component, computed, inject, model } from '@angular/core';

import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmSwitchImports } from '@spartan-ng/helm/switch';
import { HlmDatePickerImports } from '@spartan-ng/helm/date-picker';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideCalendar } from '@ng-icons/lucide';

import {
  FLASH_ANONYMOUS_PRICE,
  FLASH_CAPSULE_MAX_DAYS,
  FLASH_CAPSULE_MIN_DAYS,
  FLASH_CAPSULE_PRICE,
} from '../../../../core/models/coin.model';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { TranslationKey } from '../../../../core/i18n/translation.types';
import { provideCalendarI18n } from '../../../../core/i18n/calendar-i18n';
import { GlyphComponent } from '../../../../shared/components/glyph/glyph.component';
import { CoinTextComponent } from '../../../../shared/components/coin/coin-text.component';

// A local calendar day as the API wants it (YYYY-MM-DD).
export function toLocalDateString(date: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}

function addDays(days: number): Date {
  const d = new Date();
  d.setHours(12, 0, 0, 0);
  d.setDate(d.getDate() + days);
  return d;
}

function addMonths(months: number): Date {
  const d = new Date();
  d.setHours(12, 0, 0, 0);
  d.setMonth(d.getMonth() + months);
  return d;
}

// Flash add-ons (rama 50, §17.15 item 6), in the flash form: anonymous and time capsule,
// combinable, each with its price. The capsule opens on any day from tomorrow to a year
// ahead: three shortcuts plus a date picker.
@Component({
  selector: 'app-flash-options',
  imports: [HlmButtonImports, HlmSwitchImports, HlmDatePickerImports, NgIcon, GlyphComponent, CoinTextComponent],
  providers: [provideIcons({ lucideCalendar }), provideCalendarI18n()],
  template: `
    <div class="flex flex-col gap-2">
      <p class="text-sm font-semibold text-foreground">{{ t('flash.extrasTitle') }}</p>

      <div class="flex items-start gap-3 rounded-default border border-border p-3">
        <app-glyph name="anonymous" size="sm" />
        <span class="flex min-w-0 flex-1 flex-col gap-0.5">
          <app-coin-text class="text-sm font-semibold text-foreground" [text]="t('flash.anonymous', { price: anonymousPrice })" />
          <span class="text-xs text-muted-foreground">{{ t('flash.anonymousText') }}</span>
        </span>
        <hlm-switch [checked]="anonymous()" (checkedChange)="anonymous.set($event)" [aria-label]="t('flash.anonymousLabel')" />
      </div>

      <div class="flex flex-col gap-3 rounded-default border border-border p-3">
        <div class="flex items-start gap-3">
          <app-glyph name="capsule" size="sm" />
          <span class="flex min-w-0 flex-1 flex-col gap-0.5">
            <app-coin-text class="text-sm font-semibold text-foreground" [text]="t('flash.capsule', { price: capsulePrice })" />
            <span class="text-xs text-muted-foreground">{{ t('flash.capsuleText') }}</span>
          </span>
          <hlm-switch [checked]="capsuleOpensOn() !== null" (checkedChange)="toggleCapsule($event)" [aria-label]="t('flash.capsuleLabel')" />
        </div>

        @if (capsuleOpensOn() !== null) {
          <div class="flex flex-wrap gap-2">
            @for (shortcut of shortcuts; track shortcut.label) {
              <button
                type="button"
                hlmBtn
                variant="outline"
                size="sm"
                class="rounded-full transition-colors focus-visible:ring-2 focus-visible:ring-ring"
                [class]="capsuleOpensOn() === shortcut.date ? 'border-accent bg-accent/15' : ''"
                [attr.aria-pressed]="capsuleOpensOn() === shortcut.date"
                (click)="capsuleOpensOn.set(shortcut.date)"
              >
                {{ t(shortcut.label) }}
              </button>
            }
          </div>
          <hlm-date-picker
            [date]="pickerDate()"
            [autoCloseOnSelect]="true"
            [min]="minDate"
            [max]="maxDate"
            captionLayout="dropdown"
            (dateChange)="onPick($event)"
          >
            <hlm-date-picker-trigger class="w-full" variant="outline">
              <ng-icon hlm name="lucideCalendar" size="sm" class="mr-2 shrink-0" />
              {{ t('flash.capsuleOpensOn', { date: openingLabel() }) }}
            </hlm-date-picker-trigger>
          </hlm-date-picker>
        }
      </div>
    </div>
  `,
})
export class FlashOptionsComponent {
  private readonly i18n = inject(I18nService);
  protected readonly t = this.i18n.t;
  protected readonly anonymousPrice = FLASH_ANONYMOUS_PRICE;
  protected readonly capsulePrice = FLASH_CAPSULE_PRICE;

  public readonly anonymous = model(false);
  // Local YYYY-MM-DD the capsule opens on; null = no capsule.
  public readonly capsuleOpensOn = model<string | null>(null);

  protected readonly minDate = addDays(FLASH_CAPSULE_MIN_DAYS);
  protected readonly maxDate = addDays(FLASH_CAPSULE_MAX_DAYS);

  protected readonly shortcuts: { label: TranslationKey; date: string }[] = [
    { label: 'flash.capsuleOneMonth', date: toLocalDateString(addMonths(1)) },
    { label: 'flash.capsuleSixMonths', date: toLocalDateString(addMonths(6)) },
    { label: 'flash.capsuleOneYear', date: toLocalDateString(addDays(FLASH_CAPSULE_MAX_DAYS)) },
  ];

  protected readonly pickerDate = computed(() => {
    const value = this.capsuleOpensOn();
    if (!value) return undefined;
    const [y, m, d] = value.split('-').map(Number);
    return new Date(y, m - 1, d, 12);
  });

  protected readonly openingLabel = computed(() => {
    const date = this.pickerDate();
    return date ? this.i18n.formatDate(date, { day: 'numeric', month: 'long', year: 'numeric' }) : '';
  });

  toggleCapsule(on: boolean) {
    this.capsuleOpensOn.set(on ? this.shortcuts[0].date : null);
  }

  onPick(date: Date | undefined) {
    if (date) this.capsuleOpensOn.set(toLocalDateString(date));
  }
}
