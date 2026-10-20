import {
  AfterViewInit, Component, DestroyRef, ElementRef, OnInit, computed, effect, inject, input, output, signal, viewChild,
} from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmSpinnerImports } from '@spartan-ng/helm/spinner';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideX } from '@ng-icons/lucide';

import { I18nService } from '../../../../core/i18n/i18n.service';
import { TranslationKey } from '../../../../core/i18n/translation.types';
import {
  MonthlySummary, MonthlySummaryListItem, SummaryAward, SummaryAwardKey,
} from '../../../../core/models/monthly-summary.model';
import { MonthlySummaryService } from '../../../../core/services/monthly-summary.service';
import { UserAvatarComponent } from '../../../../shared/components/user-avatar/user-avatar.component';
import { GlyphComponent } from '../../../../shared/components/glyph/glyph.component';
import { GlyphName } from '../../../../shared/components/glyph/glyphs';

type Slide =
  | { kind: 'intro' }
  | { kind: 'participation' }
  | { kind: 'award'; award: SummaryAward }
  | { kind: 'match' }
  | { kind: 'members' };

export const AWARD_GLYPH: Record<SummaryAwardKey, GlyphName> = {
  superlative: 'crown',
  selector: 'target',
  guesser: 'guess',
  consistent: 'calendar',
  starAuthor: 'author',
};

// Story-style monthly summary (§17.12): full screen, tap the right side (or →) to advance,
// the left side (or ←) to go back, Esc to close. With prefers-reduced-motion the slides are
// static cards stacked in a scroll instead.
@Component({
  selector: 'app-monthly-summary-viewer',
  imports: [GlyphComponent, NgTemplateOutlet, HlmButtonImports, HlmSpinnerImports, NgIcon, UserAvatarComponent],
  providers: [provideIcons({ lucideX })],
  templateUrl: './monthly-summary-viewer.component.html',
  host: {
    '(document:keydown.escape)': 'close()',
    '(document:keydown.arrowright)': 'next()',
    '(document:keydown.arrowleft)': 'previous()',
  },
})
export class MonthlySummaryViewerComponent implements OnInit, AfterViewInit {
  private readonly summaries = inject(MonthlySummaryService);
  private readonly i18n = inject(I18nService);
  protected readonly t = this.i18n.t;

  readonly groupId = input.required<number>();
  readonly groupName = input<string>('');
  readonly months = input.required<MonthlySummaryListItem[]>();
  readonly initialMonth = input.required<string>();
  readonly closed = output<void>();

  private readonly root = viewChild<ElementRef<HTMLElement>>('root');
  private readonly confettiCanvas = viewChild<ElementRef<HTMLCanvasElement>>('confetti');
  private alive = true;
  private confettiPlayed = false;

  protected readonly reducedMotion =
    typeof window !== 'undefined' && !!window.matchMedia?.('(prefers-reduced-motion: reduce)').matches;

  protected readonly month = signal('');
  protected readonly summary = signal<MonthlySummary | null>(null);
  protected readonly loading = signal(true);
  protected readonly failed = signal(false);
  protected readonly index = signal(0);

  glyph(key: SummaryAwardKey): GlyphName {
    return AWARD_GLYPH[key];
  }

  protected readonly slides = computed<Slide[]>(() => {
    const s = this.summary();
    if (!s) return [];
    return [
      { kind: 'intro' },
      { kind: 'participation' },
      ...s.awards.map((award) => ({ kind: 'award' as const, award })),
      ...(s.myMatch ? [{ kind: 'match' as const }] : []),
      { kind: 'members' },
    ];
  });

  protected readonly current = computed(() => this.slides()[this.index()] ?? null);
  protected readonly onCover = computed(() => this.current()?.kind === 'intro');

  constructor() {
    inject(DestroyRef).onDestroy(() => (this.alive = false));
    // One confetti burst per opening, in the theme's own colours (lazy: not in the main bundle).
    effect(() => {
      const canvas = this.confettiCanvas()?.nativeElement;
      if (!canvas || !this.onCover() || this.confettiPlayed || this.reducedMotion) return;
      this.confettiPlayed = true;
      const css = getComputedStyle(document.documentElement);
      const colors = ['--primary', '--accent', '--sand'].map((v) => css.getPropertyValue(v).trim());
      const until = Date.now() + 4000;
      import('../../../../shared/utils/confetti').then((m) =>
        m.playCelebrationParticles(canvas, colors, true, () => this.alive && Date.now() < until),
      );
    });
  }

  protected readonly monthLabel = computed(() => this.labelFor(this.month()));
  // Inside a sentence ("Tu septiembre de 2026") the month keeps the language's own casing.
  protected readonly monthInSentence = computed(() => this.labelFor(this.month(), false));

  // Best runs worth showing: the longest ones of the month (≥ 2 in a row).
  protected readonly bestRuns = computed(() => {
    const members = this.summary()?.members ?? [];
    const top = Math.max(0, ...members.map((m) => m.bestRun));
    return top >= 2 ? { days: top, people: members.filter((m) => m.bestRun === top).map((m) => m.person) } : null;
  });

  ngOnInit(): void {
    this.open(this.initialMonth());
  }

  ngAfterViewInit(): void {
    // Focus the story itself, not the ✕, so opening it doesn't light up a focus ring (UX audit T7).
    this.root()?.nativeElement.focus();
  }

  open(month: string): void {
    this.month.set(month);
    this.index.set(0);
    this.loading.set(true);
    this.failed.set(false);
    this.summaries.get(this.groupId(), month).subscribe({
      next: (summary) => {
        this.summary.set(summary);
        this.loading.set(false);
        this.summaries.markSeen(this.groupId(), month);
      },
      error: () => {
        this.loading.set(false);
        this.failed.set(true);
      },
    });
  }

  labelFor(month: string, standalone = true): string {
    const [year, m] = month.split('-').map(Number);
    if (!year || !m) return month;
    // Only the first letter up: CSS `capitalize` turned "septiembre de 2026" into "Septiembre De 2026".
    const label = this.i18n.formatDate(new Date(year, m - 1, 1), { month: 'long', year: 'numeric' });
    return standalone ? label.charAt(0).toLocaleUpperCase() + label.slice(1) : label;
  }

  awardTitle(key: SummaryAwardKey): string {
    return this.t(`monthlySummary.awards.${key}.title` as TranslationKey);
  }

  awardValue(award: SummaryAward): string {
    return this.t(`monthlySummary.awards.${award.key}.value` as TranslationKey, { count: award.value });
  }

  names(people: { username: string }[]): string {
    return people.map((p) => p.username).join(' · ');
  }

  // Taps on the left third go back, anywhere else forward.
  onTap(event: MouseEvent): void {
    const target = event.currentTarget as HTMLElement;
    const x = event.clientX - target.getBoundingClientRect().left;
    if (x < target.clientWidth / 3) this.previous();
    else this.next();
  }

  next(): void {
    if (this.index() < this.slides().length - 1) this.index.update((i) => i + 1);
  }

  previous(): void {
    if (this.index() > 0) this.index.update((i) => i - 1);
  }

  close(): void {
    this.closed.emit();
  }
}
