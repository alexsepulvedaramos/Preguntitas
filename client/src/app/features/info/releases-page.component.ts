import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideChevronDown } from '@ng-icons/lucide';
import { HlmIconImports } from '@spartan-ng/helm/icon';

import { HlmBadgeImports } from '@spartan-ng/helm/badge';
import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';

import { LATEST_GAME_RELEASE, RELEASES, compareVersions, isGame, isPolish } from '../../core/releases/releases';
import { ReleaseNotesService } from '../../core/services/release-notes.service';
import { InfoPageNav } from './info-page-nav';
import { I18nService } from '../../core/i18n/i18n.service';
import { GlyphComponent } from '../../shared/components/glyph/glyph.component';
import { CoinTextComponent } from '../../shared/components/coin/coin-text.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';

// "Novedades" (rama 26): every release, newest first, written for players.
@Component({
  selector: 'app-releases-page',
  imports: [PageHeaderComponent, CoinTextComponent, GlyphComponent, RouterLink, HlmBadgeImports, HlmButtonImports, HlmCardImports, HlmIconImports, NgIcon],
  providers: [provideIcons({ lucideChevronDown })],
  templateUrl: './releases-page.component.html',
})
export class ReleasesPageComponent implements OnInit {
  protected readonly nav = inject(InfoPageNav);
  private readonly releaseNotes = inject(ReleaseNotesService);
  private readonly i18n = inject(I18nService);
  protected readonly t = this.i18n.t;

  // Releases this device hadn't seen when the page opened (only the latest one for a
  // device that never opened the notes), captured before marking everything as seen.
  private readonly seenBefore = this.releaseNotes.seenVersion();

  // Re-derived when the language changes: text and date format follow the UI language.
  protected readonly releases = computed(() => {
    const lang = this.i18n.lang();
    const dateFormat = new Intl.DateTimeFormat(this.i18n.locale(), { day: 'numeric', month: 'long', year: 'numeric' });
    return RELEASES.map(release => ({
      version: release.version,
      date: release.date,
      title: release.title[lang],
      items: release.items.filter(isGame).map(item => ({ glyph: item.glyph, title: item.title[lang], text: item.text[lang] })),
      polish: release.items.filter(isPolish).map(item => item.text[lang]),
      displayDate: formatDate(release.date, dateFormat),
      // Only releases with a game item are «Nuevo»; polish-only ones stay quiet (spec §14).
      isNew: release.items.some(isGame) && (this.seenBefore
        ? compareVersions(release.version, this.seenBefore) > 0
        : release === LATEST_GAME_RELEASE),
    }));
  });

  // Cards and polish folds the player opened or closed by hand; the rest follow the default
  // (a card is open when it's the latest game release or new to this device; polish folds
  // start closed).
  private readonly toggled = signal<ReadonlyMap<string, boolean>>(new Map());

  protected isOpen(release: { version: string; isNew: boolean }): boolean {
    return this.toggled().get(release.version) ?? (release.isNew || release.version === LATEST_GAME_RELEASE.version);
  }

  protected isPolishOpen(release: { version: string }): boolean {
    return this.toggled().get('polish-' + release.version) ?? false;
  }

  protected toggle(release: { version: string; isNew: boolean }): void {
    const open = this.isOpen(release);
    this.toggled.update((m) => new Map(m).set(release.version, !open));
  }

  protected togglePolish(release: { version: string }): void {
    const open = this.isPolishOpen(release);
    this.toggled.update((m) => new Map(m).set('polish-' + release.version, !open));
  }

  ngOnInit(): void {
    this.releaseNotes.markSeen();
  }
}

function formatDate(isoDate: string, format: Intl.DateTimeFormat): string {
  const [year, month, day] = isoDate.split('-').map(Number);
  return format.format(new Date(year, month - 1, day));
}
