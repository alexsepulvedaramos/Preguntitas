import { HttpErrorResponse, HttpHandlerFn, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { throwError, BehaviorSubject, Observable } from 'rxjs';
import { catchError, filter, switchMap, take } from 'rxjs';
import { AuthService } from './auth.service';

// State variables declared outside to maintain state across all HTTP requests
let isRefreshing = false;
let refreshTokenSubject: BehaviorSubject<string | null> = new BehaviorSubject<string | null>(null);

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
            // Intercept 401 Unauthorized errors
            if (error instanceof HttpErrorResponse && error.status === 401) {
                return handle401Error(authReq, next, authService);
            }

            // Propagate any other errors
            return throwError(() => error);
        })
    );
};

// Helper function to manage the token refresh concurrency
const handle401Error = (req: HttpRequest<any>, next: HttpHandlerFn, authService: AuthService): Observable<any> => {
    if (!isRefreshing) {
        // Scenario A: No refresh is currently in progress
        isRefreshing = true;
        // Reset the subject so incoming requests will pause and wait
        refreshTokenSubject.next(null);

        return authService.refreshToken().pipe(
            switchMap((response) => {
                isRefreshing = false;
                // Emit the new token, which unlocks all paused requests
                refreshTokenSubject.next(response.accessToken);

                // Retry the original request with the new access token
                const clonedRequest = req.clone({
                    setHeaders: { Authorization: `Bearer ${response.accessToken}` }
                });
                return next(clonedRequest);
            }),
            catchError((error) => {
                // If the refresh token itself fails or is expired, clear state and throw
                isRefreshing = false;
                authService.logout();
                return throwError(() => error);
            })
        );
    } else {
        // Scenario B: A refresh is already in progress
        // Subscribe to the subject and wait for a non-null token
        return refreshTokenSubject.pipe(
            filter((token) => token !== null),
            take(1),
            switchMap((token) => {
                // Once the new token is emitted, clone and retry the request
                const clonedRequest = req.clone({
                    setHeaders: { Authorization: `Bearer ${token}` }
                });
                return next(clonedRequest);
            })
        );
    }
};