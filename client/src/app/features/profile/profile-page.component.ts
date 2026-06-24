import {
  Component, inject, signal, computed, OnInit
} from '@angular/core';
import { Router } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { NgIcon, provideIcons } from '@ng-icons/core';
import {
  lucideArrowLeft, lucideCheck, lucideEye, lucideEyeOff
} from '@ng-icons/lucide';

import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmInputImports } from '@spartan-ng/helm/input';
import { HlmLabelImports } from '@spartan-ng/helm/label';
import { HlmDrawerImports } from '@spartan-ng/helm/drawer';
import { HlmSeparatorImports } from '@spartan-ng/helm/separator';

import { AuthService } from '../../core/auth/auth.service';
import { UserService } from '../../core/services/user.service';
import { UserAvatarComponent } from '../../shared/components/user-avatar/user-avatar.component';
import { AvatarPickerComponent } from './components/avatar-picker/avatar-picker.component';

const FRAME_COLORS = [
  { label: 'Morado', value: '#8b5cf6' },
  { label: 'Azul', value: '#3b82f6' },
  { label: 'Verde', value: '#22c55e' },
  { label: 'Amarillo', value: '#eab308' },
  { label: 'Naranja', value: '#f97316' },
  { label: 'Rojo', value: '#ef4444' },
  { label: 'Rosa', value: '#ec4899' },
  { label: 'Ninguno', value: null },
];

@Component({
  selector: 'app-profile-page',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    HlmButtonImports,
    HlmInputImports,
    HlmLabelImports,
    HlmDrawerImports,
    HlmSeparatorImports,
    NgIcon,
    UserAvatarComponent,
    AvatarPickerComponent,
  ],
  providers: [provideIcons({ lucideArrowLeft, lucideCheck, lucideEye, lucideEyeOff })],
  templateUrl: './profile-page.component.html',
})
export class ProfilePageComponent implements OnInit {
  protected readonly authService = inject(AuthService);
  private readonly userService = inject(UserService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  protected readonly frameColors = FRAME_COLORS;
  protected readonly isSavingProfile = signal(false);
  protected readonly isSavingPassword = signal(false);
  protected readonly isSavingFrame = signal(false);
  protected readonly profileSuccess = signal(false);
  protected readonly passwordSuccess = signal(false);
  protected readonly profileError = signal<string | null>(null);
  protected readonly passwordError = signal<string | null>(null);
  protected readonly showCurrentPassword = signal(false);
  protected readonly showNewPassword = signal(false);

  protected readonly user = computed(() => this.authService.currentUser());

  protected readonly profileForm = this.fb.group({
    username: ['', [Validators.required, Validators.minLength(3)]],
    email: ['', [Validators.required, Validators.email]],
  });

  protected readonly passwordForm = this.fb.group({
    currentPassword: ['', Validators.required],
    newPassword: ['', [Validators.required, Validators.minLength(8)]],
    confirmPassword: ['', Validators.required],
  });

  ngOnInit(): void {
    const user = this.user();
    if (user) {
      this.profileForm.patchValue({ username: user.username, email: user.email });
    }
  }

  goBack(): void {
    this.router.navigate(['/groups']);
  }

  selectFrame(color: string | null): void {
    this.isSavingFrame.set(true);
    this.userService.updateProfile({ frameColor: color ?? '' }).subscribe({
      next: profile => {
        this.authService.patchCurrentUser({ frameColor: profile.frameColor });
        this.isSavingFrame.set(false);
      },
      error: () => this.isSavingFrame.set(false),
    });
  }

  saveProfile(): void {
    if (this.profileForm.invalid) return;
    const { username, email } = this.profileForm.value;
    this.isSavingProfile.set(true);
    this.profileError.set(null);
    this.profileSuccess.set(false);

    this.userService.updateProfile({ username: username!, email: email! }).subscribe({
      next: profile => {
        this.authService.patchCurrentUser({ username: profile.username, email: profile.email });
        this.profileForm.patchValue({ username: profile.username, email: profile.email });
        this.isSavingProfile.set(false);
        this.profileSuccess.set(true);
        setTimeout(() => this.profileSuccess.set(false), 3000);
      },
      error: err => {
        const msg = err.status === 409 ? 'El nombre de usuario o email ya está en uso.' : 'Error al guardar.';
        this.profileError.set(msg);
        this.isSavingProfile.set(false);
      },
    });
  }

  changePassword(): void {
    if (this.passwordForm.invalid) return;
    const { currentPassword, newPassword, confirmPassword } = this.passwordForm.value;

    if (newPassword !== confirmPassword) {
      this.passwordError.set('Las contraseñas no coinciden.');
      return;
    }

    this.isSavingPassword.set(true);
    this.passwordError.set(null);
    this.passwordSuccess.set(false);

    this.userService.changePassword({ currentPassword: currentPassword!, newPassword: newPassword! }).subscribe({
      next: () => {
        this.passwordForm.reset();
        this.isSavingPassword.set(false);
        this.passwordSuccess.set(true);
        setTimeout(() => this.passwordSuccess.set(false), 3000);
      },
      error: err => {
        const msg = err.status === 400 && err.error?.includes?.('incorrect')
          ? 'La contraseña actual es incorrecta.'
          : 'Error al cambiar la contraseña.';
        this.passwordError.set(msg);
        this.isSavingPassword.set(false);
      },
    });
  }
}
