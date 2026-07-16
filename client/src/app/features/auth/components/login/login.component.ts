import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { HlmFieldImports } from '@spartan-ng/helm/field';
import { HlmInputImports } from '@spartan-ng/helm/input';

import { LoginRequest } from '../../models/login-request.interface';
import { AuthService } from '../../../../core/auth/auth.service';
import { RotatingQuestionComponent } from '../showcase/rotating-question.component';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink, HlmCardImports, HlmFieldImports, HlmInputImports, HlmButtonImports, RotatingQuestionComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './login.component.html',
})
export class LoginComponent {
  private readonly _fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  public readonly loginError = signal<string | null>(null);
  public readonly submitting = signal(false);

  public form = this._fb.group({
    identifier: ['', [Validators.required]],
    password: ['', [Validators.required, Validators.minLength(8)]],
  });

  public login() {
    if (this.form.valid) {
      this.loginError.set(null);
      this.submitting.set(true);

      const credentials: LoginRequest = {
        identifier: this.form.value.identifier ?? '',
        password: this.form.value.password ?? ''
      };

      this.authService.login(credentials).subscribe({
        next: () => {
          const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') || '/groups';
          this.router.navigateByUrl(returnUrl);
        },
        error: (err) => {
          this.submitting.set(false);
          if (err.status === 401) {
            this.loginError.set('Correo, nombre de usuario o contraseña incorrectos.');
          } else {
            this.loginError.set('No se ha podido iniciar sesión. Inténtalo de nuevo.');
          }
        }
      });
    }
  }
}
