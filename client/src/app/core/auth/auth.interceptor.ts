import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { throwError } from 'rxjs';
import { catchError, switchMap } from 'rxjs';
import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
    const authService = inject(AuthService);
    const token = authService.getAccessToken();

    const publicRoutes = ['/api/auth/login', '/api/auth/register', '/api/auth/refresh'];
    const isPublicRoute = publicRoutes.some(route => req.url.includes(route));

    if (isPublicRoute) {
        return next(req);
    }

    let authReq = req;
    if (token) {
        authReq = req.clone({
            setHeaders: { Authorization: `Bearer ${token}` }
        });
    }

    // Pass the request and attach catchError to intercept the response
    return next(authReq).pipe(
        catchError((error) => {
            // On 401, refresh the access token and retry the request once.
            // AuthService deduplicates concurrent refreshes (in this tab and across tabs) and
            // only ends the session when the server rejects the refresh token; transient
            // failures (offline, backend redeploying) just propagate as an error.
            if (error instanceof HttpErrorResponse && error.status === 401) {
                return authService.refreshToken().pipe(
                    switchMap((accessToken) => next(req.clone({
                        setHeaders: { Authorization: `Bearer ${accessToken}` }
                    })))
                );
            }

            // Propagate any other errors
            return throwError(() => error);
        })
    );
};
