import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';

import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideUser, lucidePlus } from '@ng-icons/lucide';
import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';

import { GroupsService } from '../../services/groups.service';
import { GroupResponse } from '../../models/group.models';

@Component({
  selector: 'app-group-list',
  imports: [
    HlmButtonImports,
    HlmCardImports,
    NgIcon
  ],
  providers: [provideIcons({ lucideUser, lucidePlus })],
  templateUrl: "./group-list.component.html",
  styleUrl: "./group-list.component.css"
})
export class GroupListComponent {
  private groupsService = inject(GroupsService);

  public groups = toSignal(
    this.groupsService.getUserGroups(),
    { initialValue: [] as GroupResponse[] }
  );
}