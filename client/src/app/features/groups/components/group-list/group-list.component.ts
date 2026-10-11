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

  // Empty-state walkthrough (rama 21): makes the ≥2-member start gate (§4.3) explicit.
  protected readonly startSteps = [
    {
      title: 'Crea un grupo o únete a uno',
      text: 'Con los botones de aquí abajo. Para unirte necesitas el código o el enlace de un amigo.',
    },
    {
      title: 'Invita al menos a una persona',
      text: 'El juego arranca en cuanto el grupo tiene dos miembros. Comparte el enlace desde el grupo.',
    },
    {
      title: 'Una pregunta cada día',
      text: 'A la hora que elija el grupo. Votáis, veis quién ha votado qué y alguien elige la de mañana.',
    },
  ];

  ngOnInit() {
    this.groupsService.loadGroups();
  }
}