import { inject } from '@angular/core';
import { CanActivateFn, Router, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { map, tap } from 'rxjs';
import { AuthService } from './auth.service';
import { UserService } from '../services/user.service';

export const authGuard: CanActivateFn = (route: ActivatedRouteSnapshot, state: RouterStateSnapshot) => {
    const authService = inject(AuthService);
    const userService = inject(UserService);
    const router = inject(Router);

    return authService.verifySession().pipe(
        tap(isAuthenticated => {
            if (isAuthenticated && !authService.profileRefreshed) {
                authService.markProfileRefreshed();
                userService.getProfile().subscribe(profile => {
                    authService.patchCurrentUser({
                        avatarUrl: profile.avatarUrl,
                        frameColor: profile.frameColor,
                    });
                });
            }
        }),
        map(isAuthenticated => {
            if (isAuthenticated) return true;
            return router.createUrlTree(['/auth/login'], {
                queryParams: { returnUrl: state.url }
            });
        })
    );
};