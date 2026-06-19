import { Component, inject } from '@angular/core';
import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';

import { GroupsService } from '../../services/groups.service';
import { CreateGroupDialogComponent } from "../create-group-dialog/create-group-dialog.component";
import { JoinGroupDialogComponent } from '../join-group-dialog/join-group-dialog.component';

@Component({
  selector: 'app-group-list',
  imports: [
    HlmButtonImports,
    HlmCardImports,
    CreateGroupDialogComponent,
    JoinGroupDialogComponent
  ],
  templateUrl: "./group-list.component.html",
  styleUrl: "./group-list.component.css"
})
export class GroupListComponent {
  private groupsService = inject(GroupsService);

  public readonly groups = this.groupsService.groups;

  ngOnInit() {
    this.groupsService.loadGroups();
  }
}