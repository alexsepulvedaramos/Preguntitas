import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  ValidatorFn,
  Validators,
} from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { HlmFieldImports } from '@spartan-ng/helm/field';
import { HlmInputImports } from '@spartan-ng/helm/input';

import { AuthService } from '../../../../core/auth/auth.service';
import { RegisterRequest } from '../../models/register-request.interface';
import { AuthValidators } from '../../validators/auth.validators';

@Component({
  selector: 'app-register',
  imports: [ReactiveFormsModule, RouterLink, HlmCardImports, HlmFieldImports, HlmInputImports, HlmButtonImports],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './register.component.html',
  styleUrl: './register.component.css',
})
export class RegisterComponent {
  private readonly _fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  protected readonly serverError = signal<string | null>(null);

  public form = this._fb.group(
    {
      username: ['', [Validators.required, Validators.minLength(3)], [AuthValidators.usernameExistsValidator(this.authService)]],
      email: ['', [Validators.required, Validators.email], [AuthValidators.emailExistsValidator(this.authService)]],
      password: ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', [Validators.required]],
    },
    { validators: AuthValidators.passwordMatch() },
  );

  public register() {
    if (this.form.valid) {
      let credentials: RegisterRequest = {
        username: this.form.value.username ?? '',
        email: this.form.value.email ?? '',
        password: this.form.value.password ?? ''
      };

      this.serverError.set(null);
      this.authService.register(credentials).subscribe({
        next: () => {
          const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') || '/groups';
          this.router.navigateByUrl(returnUrl);
        },
        error: (err) => {
          if (err.status === 409) {
            this.serverError.set('Este nombre de usuario o email ya está registrado.');
          } else {
            this.serverError.set('No se ha podido completar el registro. Inténtalo de nuevo.');
          }
        }
      });
    }
  }
}
