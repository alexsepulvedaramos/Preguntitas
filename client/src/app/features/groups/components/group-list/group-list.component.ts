import { Component, inject } from '@angular/core';

import { GroupsService } from '../../services/groups.service';
import { CreateGroupDialogComponent } from "../create-group-dialog/create-group-dialog.component";
import { JoinGroupDialogComponent } from '../join-group-dialog/join-group-dialog.component';
import { GroupCardComponent } from "../group-card/group-card.component";

@Component({
  selector: 'app-group-list',
  imports: [
    CreateGroupDialogComponent,
    JoinGroupDialogComponent,
    GroupCardComponent
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