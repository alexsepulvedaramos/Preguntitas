import { Component, inject, input, model, signal } from '@angular/core';

import { GroupMember } from '../../models/group.models';
import { voteOptionClass } from '../vote/vote-option.util';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { HlmCheckboxImports } from '@spartan-ng/helm/checkbox';

// Scale create fields (spec §5.3): who is being rated, if anyone. The range is always 1–10.
@Component({
  selector: 'app-scale-create',
  imports: [HlmCheckboxImports],
  templateUrl: './scale-create.component.html',
})
export class ScaleCreateComponent {
  public readonly members = input.required<GroupMember[]>();
  public readonly targetUserId = model<number | null>(null);
  protected readonly t = inject(I18nService).t;

  // Whether the scale rates a specific group member. When off, it rates whatever the
  // question text describes (e.g. "¿Qué nota le pones a Titanic?").
  protected readonly aboutPerson = signal(false);

  toggleAboutPerson() {
    const next = !this.aboutPerson();
    this.aboutPerson.set(next);
    if (!next) this.targetUserId.set(null);
  }

  optionClass(id: number): string {
    return voteOptionClass(this.targetUserId() === id);
  }

  select(id: number) {
    this.targetUserId.set(id);
  }
}
