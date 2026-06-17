import { AbstractControl, AsyncValidatorFn, ValidationErrors, ValidatorFn } from '@angular/forms';
import { Observable, timer, of } from 'rxjs';
import { map, switchMap, catchError } from 'rxjs/operators';
import { AuthService } from '../../../core/auth/auth.service';

export class AuthValidators {
    // Returns an async validator function
    static usernameExistsValidator(authService: AuthService): AsyncValidatorFn {
        return (control: AbstractControl): Observable<ValidationErrors | null> => {
            if (!control.value) {
                return of(null);
            }

            // Debounce the HTTP call by 1000ms
            return timer(1000).pipe(
                switchMap(() => authService.checkUsernameExists(control.value)),
                map(response => (response.exists ? { usernameTaken: true } : null)),
                catchError(() => of(null))
            );
        };
    }

    static emailExistsValidator(authService: AuthService): AsyncValidatorFn {
        return (control: AbstractControl): Observable<ValidationErrors | null> => {
            if (!control.value) {
                return of(null);
            }

            // Debounce the HTTP call by 1000ms
            return timer(1000).pipe(
                switchMap(() => authService.checkEmailExists(control.value)),
                map(response => (response.exists ? { emailTaken: true } : null)),
                catchError(() => of(null))
            );
        }
    }

    static passwordMatch(): ValidatorFn {
        return (group: AbstractControl): ValidationErrors | null => {
            const password = group.get('password')?.value;
            const confirm = group.get('confirmPassword')?.value;
            return password === confirm ? null : { passwordMismatch: true };
        };
    }
}