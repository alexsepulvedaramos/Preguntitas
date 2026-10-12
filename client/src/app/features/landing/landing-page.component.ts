import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideArrowRight } from '@ng-icons/lucide';

import { HlmButtonImports } from '@spartan-ng/helm/button';

import { FRAME_STREAK } from '../../core/constants/streak-tiers';
import { LogoWordmarkComponent } from '../../core/layout/logo/logo-wordmark.component';
import { ThemeComponent } from '../../core/layout/theme/theme.component';
import { APP_VERSION } from '../../core/releases/releases';
import { I18nService } from '../../core/i18n/i18n.service';
import { TranslationKey } from '../../core/i18n/translation.types';
import { LanguagePickerComponent } from '../../shared/components/language-picker/language-picker.component';
import { UserAvatarComponent } from '../../shared/components/user-avatar/user-avatar.component';
import { ResultsShowcaseComponent } from '../auth/components/showcase/results-showcase.component';

// Public landing at `/` (rama 21): what a context-free visitor (recruiter, friend of a
// friend) sees before signing up. Guests only — `guestGuard` sends signed-in users to
// /groups. Own light layout instead of the app shell; the detail lives in /about.
@Component({
  selector: 'app-landing-page',
  imports: [
    RouterLink,
    NgIcon,
    HlmButtonImports,
    LogoWordmarkComponent,
    ThemeComponent,
    LanguagePickerComponent,
    UserAvatarComponent,
    ResultsShowcaseComponent,
  ],
  providers: [provideIcons({ lucideArrowRight })],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './landing-page.component.html',
})
export class LandingPageComponent {
  protected readonly t = inject(I18nService).t;
  protected readonly version = APP_VERSION;
  protected readonly frameStreak = FRAME_STREAK;

  protected readonly steps: { emoji: string; title: TranslationKey; text: TranslationKey }[] = [
    { emoji: '👥', title: 'landing.step1Title', text: 'landing.step1Text' },
    { emoji: '❓', title: 'landing.step2Title', text: 'landing.step2Text' },
    { emoji: '🗳️', title: 'landing.step3Title', text: 'landing.step3Text' },
  ];

  // Fictional members wearing a few of the streak frames (see /about for all of them).
  // The last one shows "1 year" instead of a day count.
  protected readonly frames = [
    { username: 'ana', streak: 5 },
    { username: 'carlos', streak: 30 },
    { username: 'eva', streak: 100, crown: true },
    { username: 'gus', streak: 365, oneYear: true },
  ];
}
