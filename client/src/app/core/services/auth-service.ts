import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { finalize, tap } from 'rxjs/operators';

import { AuthResponse } from '../../features/auth/models/auth-response.interface';
import { LoginRequest } from '../../features/auth/models/login-request.interface';
import { Observable, throwError } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  public isAuthenticated = signal<boolean>(false);

  private readonly ACCESS_TOKEN_KEY = 'access_token';
  private readonly REFRESH_TOKEN_KEY = 'refresh_token'

  constructor() {
    this.checkInitialState();
  }

  private checkInitialState(): void {
    const token = this.getAccessToken();

    if (token && !this.isTokenExpired(token)) {
      this.isAuthenticated.set(true);
    } else {
      this.clearStorage();
    }
  }

  public login(credentials: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>('/api/auth/login', credentials).pipe(
      tap(response => {
        this.saveTokens(response.accessToken, response.refreshToken);
        this.isAuthenticated.set(true);
      })
    );
  }

  public refreshToken(): Observable<AuthResponse> {
    const refresh = this.getRefreshToken();

    if (!refresh) {
      this.logout();
      return throwError(() => new Error('No refresh token available'));
    }

    return this.http.post<AuthResponse>('/api/auth/refresh', { refreshToken: refresh }).pipe(
      tap(response => {
        // Update storage with the new token pair
        this.saveTokens(response.accessToken, response.refreshToken);
      })
    );
  }

  public logout(): void {
    const refresh = this.getRefreshToken();

    if (refresh) {
      // Notify the backend to revoke the refresh token in the database
      this.http.post('/api/auth/logout', { refreshToken: refresh }).pipe(
        finalize(() => {
          this.clearStorage();
          this.isAuthenticated.set(false);
        })
      ).subscribe();
    } else {
      // If there is no token, just clear the local state
      this.clearStorage();
      this.isAuthenticated.set(false);
    }
  }

  private saveTokens(access: string, refresh: string): void {
    localStorage.setItem(this.ACCESS_TOKEN_KEY, access);
    localStorage.setItem(this.REFRESH_TOKEN_KEY, refresh);
  }

  public getAccessToken(): string | null {
    return localStorage.getItem(this.ACCESS_TOKEN_KEY);
  }

  private getRefreshToken(): string | null {
    return localStorage.getItem(this.REFRESH_TOKEN_KEY);
  }

  private clearStorage(): void {
    localStorage.removeItem(this.ACCESS_TOKEN_KEY);
    localStorage.removeItem(this.REFRESH_TOKEN_KEY);
  }

  private isTokenExpired(token: string): boolean {
    if (!token) return true;

    try {
      // Extract the payload and fix Base64Url characters to standard Base64
      let payloadBase64Url = token.split('.')[1];
      let base64 = payloadBase64Url.replace(/-/g, '+').replace(/_/g, '/');

      // Pad the string with '=' to make its length a multiple of 4
      const pad = base64.length % 4;
      if (pad) {
        if (pad === 1) throw new Error('InvalidLengthError');
        base64 += new Array(5 - pad).join('=');
      }

      const decodedPayload = JSON.parse(window.atob(base64));

      if (!decodedPayload.exp) return false;

      const expirationDate = decodedPayload.exp * 1000;
      return Date.now() >= expirationDate;
    } catch {
      return true; // Consider expired if parsing fails
    }
  }
}