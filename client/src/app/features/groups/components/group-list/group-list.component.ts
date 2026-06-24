import { Component, inject } from '@angular/core';

import { HlmSkeletonImports } from '@spartan-ng/helm/skeleton';

import { GroupsService } from '../../services/groups.service';
import { CreateGroupDialogComponent } from "../create-group-dialog/create-group-dialog.component";
import { JoinGroupDialogComponent } from '../join-group-dialog/join-group-dialog.component';
import { GroupCardComponent } from "../group-card/group-card.component";

@Component({
  selector: 'app-group-list',
  imports: [
    HlmSkeletonImports,
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
  public readonly loading = this.groupsService.loading;
  public readonly error = this.groupsService.error;

  ngOnInit() {
    this.groupsService.loadGroups();
  }
}