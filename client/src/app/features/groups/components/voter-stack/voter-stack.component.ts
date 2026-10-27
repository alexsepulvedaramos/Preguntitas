import { Component, computed, inject, input } from '@angular/core';

import { Voter } from '../../../../core/models/result.model';
import { UserAvatarComponent } from '../../../../shared/components/user-avatar/user-avatar.component';
import { GroupStreaksService } from '../../services/group-streaks.service';

// Most voter faces shown before collapsing into "+n".
const MAX_FACES = 4;

// Who voted an option, as a small stack of faces: all of them up to 4; from 5 on, three and a
// "+n" chip. Shared by the result bars and the Deathmatch result (1.32.2). Decorative: the
// caller's control carries the accessible text.
@Component({
  selector: 'app-voter-stack',
  imports: [UserAvatarComponent],
  host: { class: 'flex items-center', 'aria-hidden': 'true' },
  template: `
    <!-- Each face sits over the one on its left, frame included: explicit stacking order -->
    @for (voter of shownVoters(); track voter.id; let first = $first; let i = $index) {
      <app-user-avatar
        [avatarUrl]="voter.avatarUrl"
        [username]="voter.username"
        [frameColor]="voter.frameColor"
        [streak]="groupStreaks.streakFor(voter.id)"
        [crown]="groupStreaks.hasCrown(voter.id)"
        [look]="groupStreaks.lookFor(voter.id)"
        effects="ring"
        size="xs"
        class="relative inline-block rounded-full ring-2 ring-background"
        [class.-ml-1]="!first"
        [style.z-index]="i + 1"
      />
    }
    @if (extraVoters() > 0) {
      <span
        class="relative -ml-1 flex size-6 items-center justify-center rounded-full bg-muted text-2xs font-semibold text-muted-foreground ring-2 ring-background"
        [style.z-index]="shownVoters().length + 1"
      >
        +{{ extraVoters() }}
      </span>
    }
  `,
})
export class VoterStackComponent {
  public readonly voters = input.required<Voter[]>();

  protected readonly groupStreaks = inject(GroupStreaksService);

  public readonly shownVoters = computed(() => {
    const voters = this.voters();
    return voters.length <= MAX_FACES ? voters : voters.slice(0, MAX_FACES - 1);
  });
  public readonly extraVoters = computed(() => this.voters().length - this.shownVoters().length);
}

// Faces (and "+n" chip) a stack of these voters shows; the bars use it to keep their label clear.
export function voterStackChips(voters: Voter[]): number {
  return voters.length <= MAX_FACES ? voters.length : MAX_FACES;
}
