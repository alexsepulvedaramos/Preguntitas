import { Component, computed, effect, input, model, signal } from '@angular/core';
import {
  CdkDrag,
  CdkDropList,
  CdkDropListGroup,
  CdkDragDrop,
  moveItemInArray,
  transferArrayItem,
} from '@angular/cdk/drag-drop';

import { HlmButtonImports } from '@spartan-ng/helm/button';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucidePlus, lucideX } from '@ng-icons/lucide';

import { GroupMember } from '../../models/group.models';

const MIN_TEAMS = 2;
const MAX_TEAMS = 4;
const MAX_MEMBERS_PER_TEAM = 4;

// Deathmatch team builder (spec §5.2, §11): drag member cards from the "sin asignar"
// pool into 2-4 team drop zones (1-4 members each, no repeats). Writes the resulting
// id lists back to the `teams` model.
@Component({
  selector: 'app-deathmatch-create',
  imports: [HlmButtonImports, NgIcon, CdkDropListGroup, CdkDropList, CdkDrag],
  providers: [provideIcons({ lucidePlus, lucideX })],
  templateUrl: './deathmatch-create.component.html',
})
export class DeathmatchCreateComponent {
  public readonly members = input.required<GroupMember[]>();
  public readonly teams = model<number[][]>([[], []]);
  public readonly showErrors = input(false);

  // Display state holds member objects; the model holds plain id lists. Seeded once from
  // the members input (any ids already in `teams` start out assigned).
  protected readonly unassigned = signal<GroupMember[]>([]);
  protected readonly teamGroups = signal<GroupMember[][]>([[], []]);
  private seeded = false;

  protected readonly canAddTeam = computed(() => this.teamGroups().length < MAX_TEAMS);
  protected readonly canRemoveTeam = computed(() => this.teamGroups().length > MIN_TEAMS);
  // True once at least one member has been dragged into a team — used to show live
  // validation without immediately marking every fresh form as invalid.
  protected readonly hasInteracted = computed(() => this.teamGroups().some((t) => t.length > 0));

  // Connected drop-list ids so every list can receive from every other (CdkDropListGroup
  // also auto-connects, but explicit ids keep team count dynamic and predictable).
  protected readonly dropListIds = computed(() => [
    'dm-unassigned',
    ...this.teamGroups().map((_, i) => `dm-team-${i}`),
  ]);

  constructor() {
    effect(() => {
      const members = this.members();
      if (this.seeded || members.length === 0) return;
      this.seeded = true;

      const assigned = new Set(this.teams().flat());
      const existing = this.teams();
      this.teamGroups.set(
        (existing.length >= MIN_TEAMS ? existing : [[], []]).map((team) =>
          team
            .map((id) => members.find((m) => m.id === id))
            .filter((m): m is GroupMember => !!m)
        )
      );
      this.unassigned.set(members.filter((m) => !assigned.has(m.id)));
    });
  }

  drop(event: CdkDragDrop<GroupMember[]>) {
    if (event.previousContainer === event.container) {
      moveItemInArray(event.container.data, event.previousIndex, event.currentIndex);
    } else {
      // Reject drops that would exceed a team's capacity (the pool has no limit).
      const intoTeam = event.container.id !== 'dm-unassigned';
      if (intoTeam && event.container.data.length >= MAX_MEMBERS_PER_TEAM) return;

      transferArrayItem(
        event.previousContainer.data,
        event.container.data,
        event.previousIndex,
        event.currentIndex
      );
    }

    // Trigger change detection for the signals and sync the id lists to the model.
    this.unassigned.set([...this.unassigned()]);
    this.teamGroups.set(this.teamGroups().map((t) => [...t]));
    this.syncModel();
  }

  addTeam() {
    if (!this.canAddTeam()) return;
    this.teamGroups.set([...this.teamGroups(), []]);
    this.syncModel();
  }

  removeTeam(index: number) {
    if (!this.canRemoveTeam()) return;
    const groups = this.teamGroups();
    // Return the removed team's members to the pool.
    this.unassigned.set([...this.unassigned(), ...groups[index]]);
    this.teamGroups.set(groups.filter((_, i) => i !== index));
    this.syncModel();
  }

  private syncModel() {
    this.teams.set(this.teamGroups().map((team) => team.map((m) => m.id)));
  }
}
