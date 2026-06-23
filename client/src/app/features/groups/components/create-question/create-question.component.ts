import { Component, computed, inject, input, output, signal } from '@angular/core';

import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmFieldImports } from '@spartan-ng/helm/field';
import { HlmInputImports } from '@spartan-ng/helm/input';
import { HlmSpinnerImports } from '@spartan-ng/helm/spinner';
import { HlmNativeSelectImports } from '@spartan-ng/helm/native-select';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideInfo } from '@ng-icons/lucide';

import { DailyService } from '../../../../core/services/daily.service';
import { QuestionsService } from '../../../../core/services/questions.service';
import { CreateQuestion } from '../../../../core/models/question.model';
import { QuestionType } from '../../../../core/enums/question-type.enum';
import { QUESTION_TYPE_LABELS } from '../../../../core/constants/question-type-labels';
import { QUESTION_TYPE_DESCRIPTIONS } from '../../../../core/constants/question-type-descriptions';
import { GroupMember } from '../../models/group.models';

import { SuperlativeCreateComponent } from '../superlative-create/superlative-create.component';
import { DeathmatchCreateComponent } from '../deathmatch-create/deathmatch-create.component';
import { ScaleCreateComponent } from '../scale-create/scale-create.component';
import { CustomPollCreateComponent } from '../custom-poll-create/custom-poll-create.component';

// Placeholder hint shown in the question text input, tailored to the selected type.
const TEXT_PLACEHOLDERS: Record<QuestionType, string> = {
  [QuestionType.Superlative]: 'p. ej. ¿Quién acabaría antes en la cárcel?',
  [QuestionType.CustomPoll]: 'p. ej. ¿A qué hora quedamos para cenar?',
  [QuestionType.Deathmatch]: 'p. ej. ¿Quién ganaría esta pelea?',
  [QuestionType.Scale]: 'p. ej. ¿Qué nota le pones a Titanic?',
  [QuestionType.SecretPairing]: 'p. ej. ¿Qué dos personas harían mejor pareja?',
  [QuestionType.OpenText]: 'p. ej. ¿Cuál es tu mejor recuerdo del viaje?',
};

const TEXT_MIN_LENGTH = 3;
const TEXT_MAX_LENGTH = 200;

// Per-type question creation form (rama 8, spec §13 row 8). Owns the text + type fields
// and hosts the per-type sub-form for the structured metadata (§5). Offers two submit
// paths: select it for tomorrow (daily/select) or just save it to the pool (POST
// /questions) — mirrors how app-vote owns its single API call.
@Component({
  selector: 'app-create-question',
  imports: [
    HlmButtonImports,
    HlmFieldImports,
    HlmInputImports,
    HlmSpinnerImports,
    HlmNativeSelectImports,
    NgIcon,
    SuperlativeCreateComponent,
    DeathmatchCreateComponent,
    ScaleCreateComponent,
    CustomPollCreateComponent,
  ],
  providers: [provideIcons({ lucideInfo })],
  templateUrl: './create-question.component.html',
})
export class CreateQuestionComponent {
  private readonly dailyService = inject(DailyService);
  private readonly questionsService = inject(QuestionsService);

  public readonly groupId = input.required<number>();
  public readonly members = input.required<GroupMember[]>();

  // Hide the "select for tomorrow" path when the current user isn't today's selector
  // (the backend rejects daily/select from anyone else) — only "save to pool" applies then.
  public readonly showSelectButton = input(true);

  // Emitted after a successful select-for-tomorrow or save-to-pool
  public readonly selected = output<void>();
  public readonly savedToPool = output<void>();

  protected readonly QuestionType = QuestionType;
  protected readonly QUESTION_TYPE_LABELS = QUESTION_TYPE_LABELS;
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
  protected readonly infoOpen = signal(false);

  // Per-type state, shared via two-way bindings with the per-type sub-forms
  protected readonly allowNobody = signal(false);
  protected readonly blacklistedUserIds = signal<number[]>([]);
  protected readonly teams = signal<number[][]>([[], []]);
  protected readonly targetUserId = signal<number | null>(null);
  protected readonly rangeMin = signal(1);
  protected readonly rangeMax = signal(10);
  protected readonly options = signal<string[]>(['', '']);
  protected readonly minSelections = signal(1);
  protected readonly maxSelections = signal(1);
  protected readonly allowOther = signal(false);

  protected readonly typeTouched = signal(false);
  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly placeholder = computed(() => TEXT_PLACEHOLDERS[this.type()]);
  protected readonly description = computed(() => QUESTION_TYPE_DESCRIPTIONS[this.type()]);

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
      case QuestionType.Scale:
        // Target person is optional (a Scale can rate any subject via the text).
        return (
          this.rangeMin() >= 0 &&
          this.rangeMax() <= 100 &&
          this.rangeMin() < this.rangeMax()
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

  protected readonly typeError = computed((): string | null => {
    if (!this.typeTouched() || this.typeValid()) return null;
    switch (this.type()) {
      case QuestionType.Deathmatch:
        return 'Todos los equipos deben tener al menos un miembro.';
      case QuestionType.Scale:
        return 'El mínimo debe ser menor que el máximo (rango 0–100).';
      case QuestionType.CustomPoll: {
        const filled = this.options().filter((o) => o.trim().length > 0);
        if (filled.length < this.options().length)
          return 'Rellena todas las opciones o elimina las vacías.';
        if (filled.length < 2) return 'Añade al menos 2 opciones.';
        if (this.minSelections() > this.maxSelections())
          return 'El mínimo de selecciones no puede superar el máximo.';
        if (this.maxSelections() > filled.length)
          return 'El máximo de selecciones no puede superar el número de opciones.';
        return 'Revisa las opciones de la encuesta.';
      }
      default:
        return null;
    }
  });

  onTypeChange(value: string | null | undefined) {
    this.type.set(Number(value) as QuestionType);
    this.infoOpen.set(false);
  }

  toggleInfo(event: Event) {
    event.stopPropagation();
    this.infoOpen.set(!this.infoOpen());
  }

  onTextChange(value: string) {
    this.text.set(value);
  }

  selectForTomorrow() {
    this.submit((dto) =>
      this.dailyService.select(this.groupId(), { newQuestion: dto }).subscribe({
        next: () => {
          this.submitting.set(false);
          this.selected.emit();
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

  private submit(run: (dto: CreateQuestion) => void) {
    this.textTouched.set(true);
    this.typeTouched.set(true);
    if (!this.isValid()) return;

    this.error.set(null);
    this.submitting.set(true);
    run(this.buildDto());
  }

  private handleError(err: { error?: string }) {
    this.submitting.set(false);
    this.error.set(err.error ?? 'No se ha podido guardar la pregunta.');
  }

  private buildDto(): CreateQuestion {
    const type = this.type();

    return {
      text: this.text().trim(),
      type,
      allowNobody: type === QuestionType.Superlative ? this.allowNobody() : false,
      blacklistedUserIds: type === QuestionType.Superlative ? this.blacklistedUserIds() : [],
      rangeMin: type === QuestionType.Scale ? this.rangeMin() : null,
      rangeMax: type === QuestionType.Scale ? this.rangeMax() : null,
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
