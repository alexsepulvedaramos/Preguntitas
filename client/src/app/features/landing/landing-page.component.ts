import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideArrowRight } from '@ng-icons/lucide';

import { HlmButtonImports } from '@spartan-ng/helm/button';

import { FRAME_STREAK } from '../../core/constants/streak-tiers';
import { LogoWordmarkComponent } from '../../core/layout/logo/logo-wordmark.component';
import { ThemeComponent } from '../../core/layout/theme/theme.component';
import { APP_VERSION } from '../../core/releases/releases';
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
    UserAvatarComponent,
    ResultsShowcaseComponent,
  ],
  providers: [provideIcons({ lucideArrowRight })],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './landing-page.component.html',
})
export class LandingPageComponent {
  protected readonly version = APP_VERSION;
  protected readonly frameStreak = FRAME_STREAK;

  protected readonly steps = [
    {
      emoji: '👥',
      title: 'Crea tu grupo',
      text: 'Ponle nombre, elige a qué hora sale la pregunta y comparte el enlace. En cuanto sois dos, empieza el juego.',
    },
    {
      emoji: '❓',
      title: 'Una pregunta al día',
      text: '¿Quién llegaría tarde a su propia boda? Cada día a una persona le toca elegir la de mañana: de los packs o una vuestra.',
    },
    {
      emoji: '🗳️',
      title: 'Votad y que se líe',
      text: 'Los resultados no son secretos: se ve quién ha votado a quién. Y cada pregunta tiene su chat para el debate.',
    },
  ];

  // Fictional members wearing a few of the streak frames (see /about for all of them).
  protected readonly frames = [
    { username: 'ana', streak: 5, label: '5 días' },
    { username: 'carlos', streak: 30, label: '30 días' },
    { username: 'eva', streak: 100, label: '100 días', crown: true },
    { username: 'gus', streak: 365, label: '1 año' },
  ];
}
