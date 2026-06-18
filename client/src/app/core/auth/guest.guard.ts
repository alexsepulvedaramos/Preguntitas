import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { map } from 'rxjs';
import { AuthService } from './auth.service';

export const guestGuard: CanActivateFn = () => {
    const authService = inject(AuthService);
    const router = inject(Router);

    return authService.verifySession().pipe(
        map(isAuthenticated => {
            if (isAuthenticated) {
                // Session is valid or successfully refreshed, redirect to the main app
                return router.createUrlTree(['/questions']);
            }

            // No valid session and refresh failed, allow access to public routes
            return true;
        })
    );
};