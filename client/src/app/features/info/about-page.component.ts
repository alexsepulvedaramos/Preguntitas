import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideArrowLeft } from '@ng-icons/lucide';

import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';

import { QuestionType } from '../../core/enums/question-type.enum';
import { QUESTION_TYPE_BADGE_CLASS } from '../../core/constants/question-type-colors';
import { QUESTION_TYPE_LABELS } from '../../core/constants/question-type-labels';
import { FRAME_STREAK, STREAK_TIERS } from '../../core/constants/streak-tiers';
import { APP_VERSION } from '../../core/releases/releases';
import { UserAvatarComponent } from '../../shared/components/user-avatar/user-avatar.component';
import { ResultsShowcaseComponent } from '../auth/components/showcase/results-showcase.component';
import { InfoPageNav } from './info-page-nav';

interface TypeExample {
  type: QuestionType;
  how: string;
  example: string;
}

// "Cómo funciona" (rama 26): the game explained for players and context-free visitors,
// with live examples built from the real components (result bars, streak frames).
@Component({
  selector: 'app-about-page',
  imports: [
    RouterLink,
    NgIcon,
    HlmButtonImports,
    HlmCardImports,
    UserAvatarComponent,
    ResultsShowcaseComponent,
  ],
  providers: [provideIcons({ lucideArrowLeft })],
  templateUrl: './about-page.component.html',
})
export class AboutPageComponent {
  protected readonly nav = inject(InfoPageNav);

  protected readonly version = APP_VERSION;
  protected readonly frameStreak = FRAME_STREAK;
  protected readonly typeLabels = QUESTION_TYPE_LABELS;
  protected readonly typeBadge = QUESTION_TYPE_BADGE_CLASS;

  protected readonly steps = [
    {
      emoji: '🕘',
      title: 'Sale la pregunta',
      text: 'Todos los días a la misma hora, la que haya puesto el admin del grupo. El juego arranca en cuanto el grupo tiene dos personas.',
    },
    {
      emoji: '🗳️',
      title: '24 horas para votar',
      text: 'Un voto por persona y sin vuelta atrás. Al votar ves los resultados, y no son secretos: se ve quién ha votado qué.',
    },
    {
      emoji: '🎲',
      title: 'Alguien elige la de mañana',
      text: 'Cada día le toca a una persona distinta. Puede escoger una de las preguntas del grupo, una de los packs o crear una nueva. Si no elige, la app pone una por ella.',
    },
    {
      emoji: '💬',
      title: 'Se lía el debate',
      text: 'Cada pregunta tiene su chat. Y en el historial quedan todos los días anteriores con sus resultados.',
    },
  ];

  protected readonly types: TypeExample[] = [
    {
      type: QuestionType.Superlative,
      how: 'Se vota a una persona del grupo.',
      example: '¿Quién llegaría tarde a su propia boda?',
    },
    {
      type: QuestionType.SecretPairing,
      how: 'Se eligen dos personas del grupo.',
      example: '¿Qué dos personas formarían el mejor dúo cómico?',
    },
    {
      type: QuestionType.Deathmatch,
      how: 'Equipos del grupo frente a frente: gana el bando más votado.',
      example: 'Apocalipsis zombi: ¿qué equipo sobrevive más días?',
    },
    {
      type: QuestionType.Scale,
      how: 'Una nota del 1 al 10, sobre ti, sobre alguien o sobre lo que sea.',
      example: 'Del 1 al 10, ¿cómo de bien conduces?',
    },
    {
      type: QuestionType.CustomPoll,
      how: 'Opciones a elegir, a veces con hueco para escribir la tuya.',
      example: '¿Tortilla con o sin cebolla?',
    },
    {
      type: QuestionType.OpenText,
      how: 'Cada uno escribe su respuesta, en una frase.',
      example: 'Top 3 snacks para una noche de peli.',
    },
  ];

  // Fictional members wearing each frame at the first day of its tier.
  private readonly sampleNames = ['ana', 'bea', 'carlos', 'dani', 'eva', 'fer', 'gus'];
  protected readonly tiers = STREAK_TIERS.map((tier, i) => ({
    ...tier,
    username: this.sampleNames[i],
    maxDays: STREAK_TIERS[i + 1] ? STREAK_TIERS[i + 1].minDays - 1 : null,
  }));
}
