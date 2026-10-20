import { Component, OnInit, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideArrowLeft } from '@ng-icons/lucide';

import { HlmBadgeImports } from '@spartan-ng/helm/badge';
import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';

import { RELEASES, compareVersions } from '../../core/releases/releases';
import { ReleaseNotesService } from '../../core/services/release-notes.service';
import { InfoPageNav } from './info-page-nav';
import { I18nService } from '../../core/i18n/i18n.service';
import { GlyphComponent } from '../../shared/components/glyph/glyph.component';
import { CoinTextComponent } from '../../shared/components/coin/coin-text.component';

// "Novedades" (rama 26): every release, newest first, written for players.
@Component({
  selector: 'app-releases-page',
  imports: [CoinTextComponent, GlyphComponent, RouterLink, NgIcon, HlmBadgeImports, HlmButtonImports, HlmCardImports],
  providers: [provideIcons({ lucideArrowLeft })],
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
      items: release.items.map(item => ({ glyph: item.glyph, title: item.title[lang], text: item.text[lang] })),
      displayDate: formatDate(release.date, dateFormat),
      isNew: this.seenBefore
        ? compareVersions(release.version, this.seenBefore) > 0
        : release === RELEASES[0],
    }));
  });

  ngOnInit(): void {
    this.releaseNotes.markSeen();
  }
}

function formatDate(isoDate: string, format: Intl.DateTimeFormat): string {
  const [year, month, day] = isoDate.split('-').map(Number);
  return format.format(new Date(year, month - 1, day));
}
