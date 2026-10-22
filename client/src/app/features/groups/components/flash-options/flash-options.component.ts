import { Component, computed, inject, model } from '@angular/core';

import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmSwitchImports } from '@spartan-ng/helm/switch';
import { HlmDatePickerImports } from '@spartan-ng/helm/date-picker';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideCalendar } from '@ng-icons/lucide';

import {
  FLASH_ANONYMOUS_AUTHOR_PRICE,
  FLASH_ANONYMOUS_VOTES_PRICE,
  FLASH_CAPSULE_MAX_DAYS,
  FLASH_CAPSULE_MIN_DAYS,
  FLASH_CAPSULE_PRICE,
} from '../../../../core/models/coin.model';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { TranslationKey } from '../../../../core/i18n/translation.types';
import { provideCalendarI18n } from '../../../../core/i18n/calendar-i18n';
import { GlyphComponent } from '../../../../shared/components/glyph/glyph.component';
import { GlyphName } from '../../../../shared/components/glyph/glyphs';
import { CoinTextComponent } from '../../../../shared/components/coin/coin-text.component';
import { FoldSectionComponent } from '../fold-section/fold-section.component';

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

const DATE_FORMAT: Intl.DateTimeFormatOptions = { day: 'numeric', month: 'short', year: 'numeric' };

// Flash extras (rama 50, §17.15 item 6), folded under «Extras» in the flash form: anonymous
// author, anonymous votes and time capsule, combinable, each with its price. The fold's
// summary lists what's on. The capsule opens on any day from tomorrow to a year ahead: three
// shortcuts plus a date picker.
@Component({
  selector: 'app-flash-options',
  imports: [
    HlmButtonImports,
    HlmSwitchImports,
    HlmDatePickerImports,
    NgIcon,
    GlyphComponent,
    CoinTextComponent,
    FoldSectionComponent,
  ],
  providers: [provideIcons({ lucideCalendar }), provideCalendarI18n()],
  template: `
    <app-fold-section [title]="t('flash.extrasTitle')" [summary]="summary()">
      @for (extra of extras; track extra.glyph) {
        <div class="flex items-start gap-3">
          <app-glyph [name]="extra.glyph" size="sm" />
          <span class="flex min-w-0 flex-1 flex-col gap-0.5">
            <app-coin-text class="text-sm font-semibold text-foreground" [text]="t(extra.title, { price: extra.price })" />
            <span class="text-xs text-muted-foreground">{{ t(extra.text) }}</span>
          </span>
          <hlm-switch [checked]="isOn(extra.glyph)" (checkedChange)="set(extra.glyph, $event)" [aria-label]="t(extra.label)" />
        </div>
      }

      @if (capsuleOpensOn() !== null) {
        <div class="flex flex-wrap gap-2 pl-11">
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
          <hlm-date-picker
            [date]="pickerDate()"
            [autoCloseOnSelect]="true"
            [min]="minDate"
            [max]="maxDate"
            [formatDate]="formatPicked"
            captionLayout="dropdown"
            (dateChange)="onPick($event)"
          >
            <hlm-date-picker-trigger class="w-full" variant="outline">
              <ng-icon hlm name="lucideCalendar" size="sm" class="mr-2 shrink-0" />
              {{ openingLabel() }}
            </hlm-date-picker-trigger>
          </hlm-date-picker>
        </div>
      }
    </app-fold-section>
  `,
})
export class FlashOptionsComponent {
  private readonly i18n = inject(I18nService);
  protected readonly t = this.i18n.t;

  public readonly anonymousAuthor = model(false);
  public readonly anonymousVotes = model(false);
  // Local YYYY-MM-DD the capsule opens on; null = no capsule.
  public readonly capsuleOpensOn = model<string | null>(null);

  protected readonly extras: {
    glyph: GlyphName;
    title: TranslationKey;
    text: TranslationKey;
    label: TranslationKey;
    price: number;
  }[] = [
    { glyph: 'anonymous', title: 'flash.anonymousAuthor', text: 'flash.anonymousAuthorText', label: 'flash.anonymousAuthorLabel', price: FLASH_ANONYMOUS_AUTHOR_PRICE },
    { glyph: 'secretVotes', title: 'flash.anonymousVotes', text: 'flash.anonymousVotesText', label: 'flash.anonymousVotesLabel', price: FLASH_ANONYMOUS_VOTES_PRICE },
    { glyph: 'capsule', title: 'flash.capsule', text: 'flash.capsuleText', label: 'flash.capsuleLabel', price: FLASH_CAPSULE_PRICE },
  ];

  protected readonly minDate = addDays(FLASH_CAPSULE_MIN_DAYS);
  protected readonly maxDate = addDays(FLASH_CAPSULE_MAX_DAYS);

  protected readonly shortcuts: { label: TranslationKey; date: string }[] = [
    { label: 'flash.capsuleOneMonth', date: toLocalDateString(addMonths(1)) },
    { label: 'flash.capsuleSixMonths', date: toLocalDateString(addMonths(6)) },
    { label: 'flash.capsuleOneYear', date: toLocalDateString(addDays(FLASH_CAPSULE_MAX_DAYS)) },
  ];

  // The picker's own label follows the app language, not the browser's (rama 50 fix).
  protected readonly formatPicked = (date: Date) => this.i18n.formatDate(date, DATE_FORMAT);

  protected readonly pickerDate = computed(() => {
    const value = this.capsuleOpensOn();
    if (!value) return undefined;
    const [y, m, d] = value.split('-').map(Number);
    return new Date(y, m - 1, d, 12);
  });

  protected readonly openingLabel = computed(() => {
    const date = this.pickerDate();
    return date ? this.formatPicked(date) : '';
  });

  // What's on, for the folded row: «Autor anónimo, Cápsula 22 nov 2026».
  protected readonly summary = computed(() => {
    const on: string[] = [];
    if (this.anonymousAuthor()) on.push(this.t('flash.anonymousAuthorLabel'));
    if (this.anonymousVotes()) on.push(this.t('flash.anonymousVotesLabel'));
    if (this.capsuleOpensOn()) on.push(`${this.t('flash.capsuleLabel')} ${this.openingLabel()}`);
    return on.join(', ');
  });

  isOn(extra: GlyphName): boolean {
    if (extra === 'anonymous') return this.anonymousAuthor();
    if (extra === 'secretVotes') return this.anonymousVotes();
    return this.capsuleOpensOn() !== null;
  }

  set(extra: GlyphName, on: boolean) {
    if (extra === 'anonymous') this.anonymousAuthor.set(on);
    else if (extra === 'secretVotes') this.anonymousVotes.set(on);
    else this.capsuleOpensOn.set(on ? this.shortcuts[0].date : null);
  }

  onPick(date: Date | undefined) {
    if (date) this.capsuleOpensOn.set(toLocalDateString(date));
  }
}
