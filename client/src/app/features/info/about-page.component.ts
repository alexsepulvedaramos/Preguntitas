import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { provideIcons } from '@ng-icons/core';
import { lucideArrowLeft } from '@ng-icons/lucide';

import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';

import { QuestionType } from '../../core/enums/question-type.enum';
import { QUESTION_TYPE_LABELS } from '../../core/constants/question-type-labels';
import { FRAME_STREAK, STREAK_MATERIAL_KEYS, STREAK_TIERS } from '../../core/constants/streak-tiers';
import { I18nService } from '../../core/i18n/i18n.service';
import { TranslationKey } from '../../core/i18n/translation.types';
import { APP_VERSION } from '../../core/releases/releases';
import { UserAvatarComponent } from '../../shared/components/user-avatar/user-avatar.component';
import { ResultsShowcaseComponent } from '../auth/components/showcase/results-showcase.component';
import { InfoPageNav } from './info-page-nav';
import { GlyphComponent } from '../../shared/components/glyph/glyph.component';
import { GlyphName } from '../../shared/components/glyph/glyphs';
import { QuestionTypeChipComponent } from '../../shared/components/question-type-chip/question-type-chip.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';

interface TypeExample {
  type: QuestionType;
  how: TranslationKey;
  example: TranslationKey;
}

// "Cómo funciona" (rama 26): the game explained for players and context-free visitors,
// with live examples built from the real components (result bars, streak frames).
@Component({
  selector: 'app-about-page',
  imports: [PageHeaderComponent, QuestionTypeChipComponent, 
    GlyphComponent,
    RouterLink,
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
  protected readonly t = inject(I18nService).t;
  protected readonly materialKeys = STREAK_MATERIAL_KEYS;

  protected readonly version = APP_VERSION;
  protected readonly frameStreak = FRAME_STREAK;
  protected readonly typeLabels = QUESTION_TYPE_LABELS;

  protected readonly steps: { glyph: GlyphName; title: TranslationKey; text: TranslationKey }[] = [
    { glyph: 'clock', title: 'about.step1Title', text: 'about.step1Text' },
    { glyph: 'vote', title: 'about.step2Title', text: 'about.step2Text' },
    { glyph: 'dice', title: 'about.step3Title', text: 'about.step3Text' },
    { glyph: 'chat', title: 'about.step4Title', text: 'about.step4Text' },
  ];

  protected readonly types: TypeExample[] = [
    { type: QuestionType.Superlative, how: 'about.types.superlativeHow', example: 'about.types.superlativeExample' },
    { type: QuestionType.SecretPairing, how: 'about.types.secretPairingHow', example: 'about.types.secretPairingExample' },
    { type: QuestionType.Deathmatch, how: 'about.types.deathmatchHow', example: 'about.types.deathmatchExample' },
    { type: QuestionType.Scale, how: 'about.types.scaleHow', example: 'about.types.scaleExample' },
    { type: QuestionType.CustomPoll, how: 'about.types.customPollHow', example: 'about.types.customPollExample' },
    { type: QuestionType.OpenText, how: 'about.types.openTextHow', example: 'about.types.openTextExample' },
  ];

  // Fictional members wearing each frame at the first day of its tier.
  private readonly sampleNames = ['ana', 'bea', 'carlos', 'dani', 'eva', 'fer', 'gus'];
  protected readonly tiers = STREAK_TIERS.map((tier, i) => ({
    ...tier,
    username: this.sampleNames[i],
    maxDays: STREAK_TIERS[i + 1] ? STREAK_TIERS[i + 1].minDays - 1 : null,
  }));
}
