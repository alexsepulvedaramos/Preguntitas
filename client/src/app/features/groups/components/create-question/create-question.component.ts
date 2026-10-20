import { Component, computed, effect, inject, input, output, signal } from '@angular/core';

import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmFieldImports } from '@spartan-ng/helm/field';
import { HlmTextareaImports } from '@spartan-ng/helm/textarea';
import { HlmSpinnerImports } from '@spartan-ng/helm/spinner';
import { HlmAlertDialog, HlmAlertDialogImports } from '@spartan-ng/helm/alert-dialog';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideZap } from '@ng-icons/lucide';

import { DailyService } from '../../../../core/services/daily.service';
import { QuestionsService } from '../../../../core/services/questions.service';
import { CreateQuestion, QuestionToVote } from '../../../../core/models/question.model';
import { LaunchFlashResponse, SelectResponse } from '../../../../core/models/daily.model';
import { EMPTY_INVENTORY, FLASH_QUESTION_PRICE } from '../../../../core/models/coin.model';
import { AuthService } from '../../../../core/auth/auth.service';
import { QuestionType } from '../../../../core/enums/question-type.enum';
import { QUESTION_TYPE_GLYPH, QUESTION_TYPE_LABELS } from '../../../../core/constants/question-type-labels';
import { QUESTION_TYPE_DESCRIPTIONS } from '../../../../core/constants/question-type-descriptions';
import { GroupMember } from '../../models/group.models';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { TranslationKey } from '../../../../core/i18n/translation.types';
import { apiErrorMessage } from '../../../../shared/utils/api-error';

import { SuperlativeCreateComponent } from '../superlative-create/superlative-create.component';
import { DeathmatchCreateComponent } from '../deathmatch-create/deathmatch-create.component';
import { ScaleCreateComponent } from '../scale-create/scale-create.component';
import { CustomPollCreateComponent } from '../custom-poll-create/custom-poll-create.component';
import { GlyphComponent } from '../../../../shared/components/glyph/glyph.component';
import { CoinTextComponent } from '../../../../shared/components/coin/coin-text.component';

// Placeholder hint shown in the question text input, tailored to the selected type.
const TEXT_PLACEHOLDERS: Record<QuestionType, TranslationKey> = {
  [QuestionType.Superlative]: 'createQuestion.placeholders.superlative',
  [QuestionType.CustomPoll]: 'createQuestion.placeholders.customPoll',
  [QuestionType.Deathmatch]: 'createQuestion.placeholders.deathmatch',
  [QuestionType.Scale]: 'createQuestion.placeholders.scale',
  [QuestionType.SecretPairing]: 'createQuestion.placeholders.secretPairing',
  [QuestionType.OpenText]: 'createQuestion.placeholders.openText',
};

const TEXT_MIN_LENGTH = 3;
const TEXT_MAX_LENGTH = 200;

// Per-type question creation form (rama 8, spec §13 row 8). Owns the text + type fields
// and hosts the per-type sub-form for the structured metadata (§5). Offers three submit
// paths: select it for tomorrow (daily/select), just save it to the pool (POST
// /questions), or pay to launch it right now as a flash question (rama 44, POST flash) —
// mirrors how app-vote owns its single API call.
@Component({
  selector: 'app-create-question',
  imports: [
    CoinTextComponent,
    GlyphComponent,
    HlmButtonImports,
    HlmFieldImports,
    HlmTextareaImports,
    HlmSpinnerImports,
    HlmAlertDialogImports,
    NgIcon,
    SuperlativeCreateComponent,
    DeathmatchCreateComponent,
    ScaleCreateComponent,
    CustomPollCreateComponent,
  ],
  providers: [provideIcons({ lucideZap })],
  templateUrl: './create-question.component.html',
})
export class CreateQuestionComponent {
  private readonly dailyService = inject(DailyService);
  private readonly questionsService = inject(QuestionsService);
  private readonly authService = inject(AuthService);
  protected readonly t = inject(I18nService).t;
  protected readonly flashPrice = FLASH_QUESTION_PRICE;

  public readonly groupId = input.required<number>();
  public readonly members = input.required<GroupMember[]>();

  // Hide the "select for tomorrow" path when the current user isn't today's selector
  // (the backend rejects daily/select from anyone else) — only "save to pool" applies then.
  public readonly showSelectButton = input(true);

  // Pre-fill the form when editing an already-chosen question (Scale / CustomPoll).
  // Deathmatch uses the dedicated assign-teams step in the parent dialog instead.
  public readonly initialValue = input<QuestionToVote | null>(null);

  // When set, selectForTomorrow() updates this question's content in place instead of
  // creating a new entity — avoids duplicates when editing a pending question.
  public readonly overwriteQuestionId = input<number | null>(null);

  // Emitted after a successful select-for-tomorrow or save-to-pool
  public readonly selected = output<SelectResponse>();
  public readonly savedToPool = output<void>();
  public readonly flashLaunched = output<LaunchFlashResponse>();

  // "Enviar ahora" (§17.11): disabled, saying how much is missing, when you can't afford it.
  // Flash questions bought ahead in the shop (rama 45) launch without charging.
  protected readonly ownedFlashes = computed(
    () => (this.authService.currentUser()?.inventory ?? EMPTY_INVENTORY).flashQuestion,
  );
  protected readonly flashMissing = computed(() =>
    Math.max(0, FLASH_QUESTION_PRICE - (this.authService.currentUser()?.coins ?? 0)),
  );

  protected readonly QuestionType = QuestionType;
  protected readonly QUESTION_TYPE_LABELS = QUESTION_TYPE_LABELS;
  protected readonly QUESTION_TYPE_GLYPH = QUESTION_TYPE_GLYPH;
  protected readonly QUESTION_TYPE_DESCRIPTIONS = QUESTION_TYPE_DESCRIPTIONS;
  // Superlativo first — it's the default, and the most intuitive type to start with.
  protected readonly types = [
    QuestionType.Superlative,
    QuestionType.CustomPoll,
    QuestionType.Scale,
    QuestionType.Deathmatch,
    QuestionType.SecretPairing,
    QuestionType.OpenText,
  ];

  protected readonly text = signal('');
  protected readonly textTouched = signal(false);
  protected readonly type = signal<QuestionType>(QuestionType.Superlative);

  // Per-type state, shared via two-way bindings with the per-type sub-forms
  protected readonly allowNobody = signal(false);
  protected readonly blacklistedUserIds = signal<number[]>([]);
  protected readonly teams = signal<number[][]>([[], []]);
  protected readonly targetUserId = signal<number | null>(null);
  protected readonly options = signal<string[]>(['', '']);
  protected readonly minSelections = signal(1);
  protected readonly maxSelections = signal(1);
  protected readonly allowOther = signal(false);

  protected readonly typeTouched = signal(false);
  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);

  constructor() {
    effect(
      () => {
        const v = this.initialValue();
        if (!v) return;
        this.text.set(v.text);
        this.type.set(v.type);
        if (v.type === QuestionType.CustomPoll && v.options.length > 0) {
          this.options.set(v.options.map((o) => o.text));
          if (v.minSelections != null) this.minSelections.set(v.minSelections);
          if (v.maxSelections != null) this.maxSelections.set(v.maxSelections);
        }
      },
      { allowSignalWrites: true },
    );
  }

  protected readonly placeholder = computed(() => this.t(TEXT_PLACEHOLDERS[this.type()]));
  protected readonly description = computed(() => this.t(QUESTION_TYPE_DESCRIPTIONS[this.type()]));

  protected readonly textValid = computed(() => {
    const length = this.text().trim().length;
    return length >= TEXT_MIN_LENGTH && length <= TEXT_MAX_LENGTH;
  });

  protected readonly typeValid = computed(() => {
    switch (this.type()) {
      case QuestionType.Deathmatch:
        return (
          this.teams().length >= 2 &&
          this.teams().length <= 4 &&
          this.teams().every((team) => team.length >= 1 && team.length <= 4)
        );
      case QuestionType.CustomPoll: {
        const filled = this.options().filter((o) => o.trim().length > 0);
        return (
          filled.length === this.options().length &&
          filled.length >= 2 &&
          this.minSelections() >= 1 &&
          this.minSelections() <= this.maxSelections() &&
          this.maxSelections() <= filled.length
        );
      }
      default:
        return true;
    }
  });

  protected readonly isValid = computed(() => this.textValid() && this.typeValid());

  protected readonly typeError = computed((): TranslationKey | null => {
    if (!this.typeTouched() || this.typeValid()) return null;
    switch (this.type()) {
      case QuestionType.Deathmatch:
        return 'createQuestion.teamsEmpty';
      case QuestionType.CustomPoll: {
        const filled = this.options().filter((o) => o.trim().length > 0);
        if (filled.length < this.options().length) return 'createQuestion.fillOptions';
        if (filled.length < 2) return 'createQuestion.minOptions';
        if (this.minSelections() > this.maxSelections()) return 'createQuestion.minOverMax';
        if (this.maxSelections() > filled.length) return 'createQuestion.maxOverOptions';
        return 'createQuestion.checkPoll';
      }
      default:
        return null;
    }
  });

  onTypeChange(value: QuestionType) {
    this.type.set(value);
  }

  onTextChange(value: string) {
    this.text.set(value);
  }

  selectForTomorrow() {
    const overwriteId = this.overwriteQuestionId();
    this.submit((dto) =>
      this.dailyService
        .select(this.groupId(), {
          newQuestion: dto,
          overwriteQuestionId: overwriteId,
        })
        .subscribe({
        next: (res) => {
          this.submitting.set(false);
          this.selected.emit(res);
        },
        error: (err) => this.handleError(err),
      })
    );
  }

  saveToPool() {
    this.submit((dto) =>
      this.questionsService.create(this.groupId(), dto).subscribe({
        next: () => {
          this.submitting.set(false);
          this.savedToPool.emit();
        },
        error: (err) => this.handleError(err),
      })
    );
  }

  // The form is checked before asking: the confirmation only shows for a valid question.
  askLaunchFlash(dialog: HlmAlertDialog) {
    this.textTouched.set(true);
    this.typeTouched.set(true);
    if (this.isValid()) dialog.open();
  }

  launchFlash() {
    this.submit((dto) =>
      this.dailyService.launchFlash(this.groupId(), dto, this.ownedFlashes() > 0).subscribe({
        next: (res) => {
          this.submitting.set(false);
          this.authService.patchCurrentUser({ coins: res.balance, inventory: res.inventory });
          this.flashLaunched.emit(res);
        },
        error: (err) => this.handleError(err),
      })
    );
  }

  private submit(run: (dto: CreateQuestion) => void) {
    this.textTouched.set(true);
    this.typeTouched.set(true);
    if (!this.isValid()) return;

    this.error.set(null);
    this.submitting.set(true);
    run(this.buildDto());
  }

  private handleError(err: unknown) {
    this.submitting.set(false);
    this.error.set(apiErrorMessage(err, this.t('createQuestion.failed')));
  }

  private buildDto(): CreateQuestion {
    const type = this.type();

    return {
      text: this.text().trim(),
      type,
      allowNobody: type === QuestionType.Superlative ? this.allowNobody() : false,
      blacklistedUserIds: type === QuestionType.Superlative ? this.blacklistedUserIds() : [],
      targetUserId: type === QuestionType.Scale ? this.targetUserId() : null,
      minSelections:
        type === QuestionType.SecretPairing
          ? 2
          : type === QuestionType.CustomPoll
            ? this.minSelections()
            : null,
      maxSelections:
        type === QuestionType.SecretPairing
          ? 2
          : type === QuestionType.CustomPoll
            ? this.maxSelections()
            : null,
      allowOther: type === QuestionType.CustomPoll ? this.allowOther() : false,
      teams: type === QuestionType.Deathmatch ? this.teams() : [],
      options:
        type === QuestionType.CustomPoll
          ? this.options()
              .map((text) => text.trim())
              .filter((text) => text.length > 0)
              .map((text) => ({ text }))
          : [],
    };
  }
}
