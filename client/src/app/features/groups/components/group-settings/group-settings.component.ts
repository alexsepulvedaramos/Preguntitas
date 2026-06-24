import {
  Component,
  OnInit,
  computed,
  inject,
  input,
  signal,
} from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { toast } from '@spartan-ng/brain/sonner';

import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmInputImports } from '@spartan-ng/helm/input';
import { HlmLabelImports } from '@spartan-ng/helm/label';
import { HlmSkeletonImports } from '@spartan-ng/helm/skeleton';
import { HlmSpinnerImports } from '@spartan-ng/helm/spinner';
import { HlmAlertDialogImports } from '@spartan-ng/helm/alert-dialog';
import { NgIcon, provideIcons } from '@ng-icons/core';
import {
  lucideArrowLeft,
  lucideCopy,
  lucideRefreshCw,
  lucideShield,
  lucideUserMinus,
} from '@ng-icons/lucide';

import { GroupsService } from '../../services/groups.service';
import { GroupMember, GroupResponse, UpdateGroupRequest } from '../../models/group.models';

@Component({
  selector: 'app-group-settings',
  imports: [
    RouterLink,
    FormsModule,
    HlmButtonImports,
    HlmInputImports,
    HlmLabelImports,
    HlmSkeletonImports,
    HlmSpinnerImports,
    HlmAlertDialogImports,
    NgIcon,
  ],
  providers: [
    provideIcons({
      lucideArrowLeft,
      lucideCopy,
      lucideRefreshCw,
      lucideShield,
      lucideUserMinus,
    }),
  ],
  templateUrl: './group-settings.component.html',
})
export class GroupSettingsComponent implements OnInit {
  private readonly groupsService = inject(GroupsService);
  private readonly router = inject(Router);

  public readonly groupId = input.required<string>();
  public readonly numericGroupId = computed(() => Number(this.groupId()));

  protected readonly group = signal<GroupResponse | null>(null);
  protected readonly members = signal<GroupMember[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);

  // Edit form state
  protected readonly editName = signal('');
  protected readonly editDescription = signal('');
  protected readonly editTime = signal('');
  protected readonly saving = signal(false);

  // Admin actions state
  protected readonly kickingId = signal<number | null>(null);
  protected readonly transferringId = signal<number | null>(null);
  protected readonly regenerating = signal(false);
  protected readonly leaving = signal(false);

  protected readonly isAdmin = computed(() =>
    this.members().find((m) => m.isCurrentUser)?.isAdmin ?? false
  );

  ngOnInit() {
    this.loadAll();
  }

  protected initials(username: string): string {
    return username.slice(0, 2).toUpperCase();
  }

  protected saveInfo() {
    if (this.saving()) return;
    this.saving.set(true);

    const req: UpdateGroupRequest = {
      name: this.editName(),
      description: this.editDescription(),
      dailyQuestionTime: this.editTime(),
    };

    this.groupsService.updateGroup(this.numericGroupId(), req).subscribe({
      next: (updated) => {
        this.group.set(updated);
        this.saving.set(false);
        toast.success('Información del grupo actualizada');
      },
      error: () => {
        this.saving.set(false);
        toast.error('No se ha podido actualizar el grupo');
      },
    });
  }

  protected copyCode() {
    const code = this.group()?.invitationCode;
    if (!code) return;
    navigator.clipboard.writeText(code).then(() => {
      toast.success('Código copiado al portapapeles');
    });
  }

  protected regenerateCode() {
    if (this.regenerating()) return;
    this.regenerating.set(true);

    this.groupsService.regenerateInviteCode(this.numericGroupId()).subscribe({
      next: (updated) => {
        this.group.set(updated);
        this.regenerating.set(false);
        toast.success('Nuevo código de invitación generado');
      },
      error: () => {
        this.regenerating.set(false);
        toast.error('No se ha podido regenerar el código');
      },
    });
  }

  protected kickMember(member: GroupMember) {
    if (this.kickingId()) return;
    this.kickingId.set(member.id);

    this.groupsService.kickMember(this.numericGroupId(), member.id).subscribe({
      next: () => {
        this.members.update((list) => list.filter((m) => m.id !== member.id));
        this.kickingId.set(null);
        toast.success(`${member.username} ha sido expulsado del grupo`);
      },
      error: () => {
        this.kickingId.set(null);
        toast.error('No se ha podido expulsar al miembro');
      },
    });
  }

  protected transferAdmin(member: GroupMember) {
    if (this.transferringId()) return;
    this.transferringId.set(member.id);

    this.groupsService
      .transferAdmin(this.numericGroupId(), { newAdminId: member.id })
      .subscribe({
        next: () => {
          this.members.update((list) =>
            list.map((m) => ({ ...m, isAdmin: m.id === member.id }))
          );
          this.transferringId.set(null);
          toast.success(`${member.username} es ahora el administrador`);
        },
        error: () => {
          this.transferringId.set(null);
          toast.error('No se ha podido transferir la administración');
        },
      });
  }

  protected leaveGroup() {
    if (this.leaving()) return;
    this.leaving.set(true);

    this.groupsService.leaveGroup(this.numericGroupId()).subscribe({
      next: () => {
        this.leaving.set(false);
        this.router.navigate(['/groups']);
      },
      error: () => {
        this.leaving.set(false);
        toast.error('No se ha podido abandonar el grupo');
      },
    });
  }

  private loadAll() {
    this.loading.set(true);
    this.error.set(null);

    this.groupsService.getGroup(this.numericGroupId()).subscribe({
      next: (group) => {
        this.group.set(group);
        this.editName.set(group.name);
        this.editDescription.set(group.description);
        this.editTime.set(group.dailyQuestionTime);

        this.groupsService.getGroupMembers(this.numericGroupId()).subscribe({
          next: (members) => {
            this.members.set(members);
            this.loading.set(false);
          },
          error: () => {
            this.error.set('No se han podido cargar los miembros.');
            this.loading.set(false);
          },
        });
      },
      error: () => {
        this.error.set('No se ha podido cargar el grupo.');
        this.loading.set(false);
      },
    });
  }
}
