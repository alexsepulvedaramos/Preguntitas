import { Component, computed, input, model, signal } from '@angular/core';

import { HlmFieldImports } from '@spartan-ng/helm/field';
import { HlmInputImports } from '@spartan-ng/helm/input';

import { GroupMember } from '../../models/group.models';
import { voteOptionClass } from '../vote/vote-option.util';

// Scale create fields (spec §5.3, §11): who is being rated and the numeric range
// (0 <= min < max <= 100).
@Component({
  selector: 'app-scale-create',
  imports: [HlmFieldImports, HlmInputImports],
  templateUrl: './scale-create.component.html',
})
export class ScaleCreateComponent {
  public readonly members = input.required<GroupMember[]>();
  public readonly targetUserId = model<number | null>(null);
  public readonly rangeMin = model(1);
  public readonly rangeMax = model(10);

  // Whether the scale rates a specific group member. When off, it rates whatever the
  // question text describes (e.g. "¿Qué nota le pones a Titanic?").
  protected readonly aboutPerson = signal(false);

  protected readonly rangeInvalid = computed(
    () => this.rangeMin() < 0 || this.rangeMax() > 100 || this.rangeMin() >= this.rangeMax()
  );

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

  onRangeMinChange(value: string) {
    this.rangeMin.set(Number(value));
  }

  onRangeMaxChange(value: string) {
    this.rangeMax.set(Number(value));
  }
}
