import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmDialogImports } from '@spartan-ng/helm/dialog';
import { HlmFieldImports } from '@spartan-ng/helm/field';
import { HlmInputImports } from '@spartan-ng/helm/input';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucidePlus } from '@ng-icons/lucide';

import { GroupsService } from '../../services/groups.service';
import { CreateGroupRequest } from '../../models/group.models';

export enum CreateGroupStep {
  Name,
  Description,
  DailyTime,
}

@Component({
  selector: 'app-create-group-dialog',
  imports: [
    ReactiveFormsModule,
    HlmDialogImports,
    HlmFieldImports,
    HlmInputImports,
    HlmButtonImports,
    NgIcon
  ],
  providers: [provideIcons({ lucidePlus })],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './create-group-dialog.component.html',
})
export class CreateGroupDialogComponent {
  private readonly fb = inject(FormBuilder);
  private groupsService = inject(GroupsService);

  readonly CreateGroupStep = CreateGroupStep;

  readonly step = signal(CreateGroupStep.Name);

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.minLength(3)]],
    description: [''],
    dailyQuestionTime: ['17:00', Validators.required],
  });

  next() {
    if (this.step() === CreateGroupStep.Name && this.form.controls.name.invalid) {
      this.form.controls.name.markAsTouched();
      return;
    }

    this.step.update((s) => s + 1);
  }

  back() {
    this.step.update((s) => s - 1);
  }

  // Receive the dialog context to close it programmatically
  createGroup(ctx: { close: () => void }) {
    if (this.form.invalid) return;

    const request: CreateGroupRequest = this.form.getRawValue();

    this.groupsService.createGroup(request).subscribe({
      next: () => {
        this.form.reset();
        this.step.set(CreateGroupStep.Name);

        // Close the dialog using the portal context
        ctx.close();
      },
      error: (err) => {
        // Handle API errors appropriately
        console.error('Failed to create group:', err);
      }
    });
  }
}