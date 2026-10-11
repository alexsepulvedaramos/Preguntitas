import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideArrowLeft } from '@ng-icons/lucide';

import { HlmBadgeImports } from '@spartan-ng/helm/badge';
import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';

import { RELEASES, compareVersions } from '../../core/releases/releases';
import { ReleaseNotesService } from '../../core/services/release-notes.service';
import { InfoPageNav } from './info-page-nav';

const DATE_FORMAT = new Intl.DateTimeFormat('es-ES', { day: 'numeric', month: 'long', year: 'numeric' });

// "Novedades" (rama 26): every release, newest first, written for players.
@Component({
  selector: 'app-releases-page',
  imports: [RouterLink, NgIcon, HlmBadgeImports, HlmButtonImports, HlmCardImports],
  providers: [provideIcons({ lucideArrowLeft })],
  templateUrl: './releases-page.component.html',
})
export class ReleasesPageComponent implements OnInit {
  protected readonly nav = inject(InfoPageNav);
  private readonly releaseNotes = inject(ReleaseNotesService);

  // Releases this device hadn't seen when the page opened (only the latest one for a
  // device that never opened the notes), captured before marking everything as seen.
  private readonly seenBefore = this.releaseNotes.seenVersion();

  protected readonly releases = RELEASES.map(release => ({
    ...release,
    displayDate: formatDate(release.date),
    isNew: this.seenBefore
      ? compareVersions(release.version, this.seenBefore) > 0
      : release === RELEASES[0],
  }));

  ngOnInit(): void {
    this.releaseNotes.markSeen();
  }
}

function formatDate(isoDate: string): string {
  const [year, month, day] = isoDate.split('-').map(Number);
  return DATE_FORMAT.format(new Date(year, month - 1, day));
}
