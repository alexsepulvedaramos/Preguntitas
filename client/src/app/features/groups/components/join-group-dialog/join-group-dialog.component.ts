import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { toast } from '@spartan-ng/brain/sonner';

import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmDialogImports } from '@spartan-ng/helm/dialog';
import { HlmFieldImports } from '@spartan-ng/helm/field';
import { HlmInputImports } from '@spartan-ng/helm/input';

import { GroupsService } from '../../services/groups.service';
import { JoinGroupRequest } from '../../models/group.models';

@Component({
  selector: 'app-join-group-dialog',
  imports: [
    ReactiveFormsModule,
    HlmDialogImports,
    HlmFieldImports,
    HlmInputImports,
    HlmButtonImports
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './join-group-dialog.component.html',
})

export class JoinGroupDialogComponent {
  private readonly fb = inject(FormBuilder);
  private groupsService = inject(GroupsService);

  readonly form = this.fb.nonNullable.group({
    invitationCode: ['', [Validators.required, Validators.minLength(6), Validators.maxLength(6)]],
  });

  joinGroup(ctx: { close: () => void }) {
    if (this.form.invalid) return;

    const request: JoinGroupRequest = this.form.getRawValue();

    this.groupsService.joinGroup(request).subscribe({
      next: ({ alreadyMember }) => {
        this.form.reset();
        ctx.close();
        if (alreadyMember) toast.info('Ya eres miembro de este grupo.');
      },
      error: (err) => {
        const msg = err.error?.message || 'Código de invitación inválido o grupo no encontrado.';
        this.form.controls.invitationCode.setErrors({ serverError: msg });
      }
    });
  }
}