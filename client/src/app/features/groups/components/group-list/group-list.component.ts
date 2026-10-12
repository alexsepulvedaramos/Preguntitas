import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';

import { HlmSkeletonImports } from '@spartan-ng/helm/skeleton';

import { GroupsService } from '../../services/groups.service';
import { CreateGroupDialogComponent } from "../create-group-dialog/create-group-dialog.component";
import { JoinGroupDialogComponent } from '../join-group-dialog/join-group-dialog.component';
import { GroupCardComponent } from "../group-card/group-card.component";
import { I18nService } from '../../../../core/i18n/i18n.service';
import { TranslationKey } from '../../../../core/i18n/translation.types';

@Component({
  selector: 'app-group-list',
  imports: [
    RouterLink,
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
  protected readonly t = inject(I18nService).t;

  public readonly groups = this.groupsService.groups;
  public readonly loading = this.groupsService.loading;
  public readonly error = this.groupsService.error;

  // Empty-state walkthrough (rama 21): makes the ≥2-member start gate (§4.3) explicit.
  protected readonly startSteps: { title: TranslationKey; text: TranslationKey }[] = [
    { title: 'groups.list.step1Title', text: 'groups.list.step1Text' },
    { title: 'groups.list.step2Title', text: 'groups.list.step2Text' },
    { title: 'groups.list.step3Title', text: 'groups.list.step3Text' },
  ];

  ngOnInit() {
    this.groupsService.loadGroups();
  }
}