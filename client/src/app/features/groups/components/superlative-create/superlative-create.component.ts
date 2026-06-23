import { Component, computed, inject, input, model } from '@angular/core';

import { AuthService } from '../../../../core/auth/auth.service';
import { GroupMember } from '../../models/group.models';
import { voteOptionClass } from '../vote/vote-option.util';

// Superlative create fields (spec §5.1, §9): who is excluded from being picked, and
// whether voters may answer "Nadie". The creator can never blacklist themselves.
@Component({
  selector: 'app-superlative-create',
  imports: [],
  templateUrl: './superlative-create.component.html',
})
export class SuperlativeCreateComponent {
  private readonly authService = inject(AuthService);

  public readonly members = input.required<GroupMember[]>();
  public readonly allowNobody = model(false);
  public readonly blacklistedUserIds = model<number[]>([]);

  protected readonly candidates = computed(() => {
    const currentUserId = Number(this.authService.currentUser()?.id);
    return this.members().filter((m) => m.id !== currentUserId);
  });

  protected optionClass(id: number): string {
    return voteOptionClass(this.blacklistedUserIds().includes(id));
  }

  toggle(id: number) {
    const current = this.blacklistedUserIds();
    this.blacklistedUserIds.set(
      current.includes(id) ? current.filter((x) => x !== id) : [...current, id]
    );
  }

  toggleAllowNobody() {
    this.allowNobody.set(!this.allowNobody());
  }
}
