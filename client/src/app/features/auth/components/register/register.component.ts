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
import { ResultsShowcaseComponent } from '../showcase/results-showcase.component';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { TranslationKey } from '../../../../core/i18n/translation.types';

@Component({
  selector: 'app-register',
  imports: [ReactiveFormsModule, RouterLink, HlmCardImports, HlmFieldImports, HlmInputImports, HlmButtonImports, ResultsShowcaseComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './register.component.html',
  styleUrl: './register.component.css',
})
export class RegisterComponent {
  private readonly _fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private readonly i18n = inject(I18nService);
  protected readonly t = this.i18n.t;

  protected readonly serverError = signal<TranslationKey | null>(null);

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
        password: this.form.value.password ?? '',
        preferredLanguage: this.i18n.lang(),
      };

      this.serverError.set(null);
      this.authService.register(credentials).subscribe({
        next: () => {
          const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') || '/groups';
          this.router.navigateByUrl(returnUrl);
        },
        error: (err) => {
          this.serverError.set(err.status === 409 ? 'auth.register.taken' : 'auth.register.failed');
        }
      });
    }
  }
}
