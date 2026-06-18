import { inject } from '@angular/core';
import { CanActivateFn, Router, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { map } from 'rxjs';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = (route: ActivatedRouteSnapshot, state: RouterStateSnapshot) => {
    const authService = inject(AuthService);
    const router = inject(Router);

    return authService.verifySession().pipe(
        map(isAuthenticated => {
            if (isAuthenticated) {
                return true;
            }

            // Create a UrlTree to redirect to login, preserving the attempted URL
            return router.createUrlTree(['/auth/login'], {
                queryParams: { returnUrl: state.url }
            });
        })
    );
};