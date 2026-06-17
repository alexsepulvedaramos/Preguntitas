import { Injectable, signal, inject, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { finalize, tap } from 'rxjs/operators';

import { AuthResponse } from '../../features/auth/models/auth-response.interface';
import { LoginRequest } from '../../features/auth/models/login-request.interface';
import { Observable, throwError } from 'rxjs';
import { User } from '../models/user.model';
import { RegisterRequest } from '../../features/auth/models/register-request.interface';
import { environment } from '../../../environments/environment.development';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = `${environment.apiUrl}/auth`;
  private http = inject(HttpClient);

  public currentUser = signal<User | null>(null);
  public isAuthenticated = computed(() => this.currentUser() !== null);

  private readonly ACCESS_TOKEN_KEY = 'access_token';
  private readonly REFRESH_TOKEN_KEY = 'refresh_token'

  constructor() {
    this.checkInitialState();
  }

  private checkInitialState(): void {
    const token = this.getAccessToken();

    if (token && !this.isTokenExpired(token)) {
      // Decode the JWT to rebuild the user state on page reload
      const user = this.extractUserFromToken(token);
      this.currentUser.set(user);
    } else {
      this.clearStorage();
      this.currentUser.set(null);
    }
  }

  public login(credentials: LoginRequest) {
    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, credentials).pipe(
      tap(response => {
        this.saveTokens(response.accessToken, response.refreshToken);

        // Update the state immediately after login
        const user = this.extractUserFromToken(response.accessToken);
        this.currentUser.set(user);
      })
    );
  }

  public register(credentials: RegisterRequest) {
    return this.http.post<AuthResponse>(`${this.apiUrl}/register`, credentials).pipe(
      tap(response => {
        this.saveTokens(response.accessToken, response.refreshToken);

        // Update the state immediately after registration
        const user = this.extractUserFromToken(response.accessToken);
        this.currentUser.set(user);
      })
    );
  }

  public refreshToken(): Observable<AuthResponse> {
    const refresh = this.getRefreshToken();

    if (!refresh) {
      this.logout();
      return throwError(() => new Error('No refresh token available'));
    }

    return this.http.post<AuthResponse>(`${this.apiUrl}/refresh`, { refreshToken: refresh }).pipe(
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
      this.http.post(`${this.apiUrl}/logout`, { refreshToken: refresh }).pipe(
        finalize(() => {
          this.clearStorage();
          this.currentUser.set(null);
        })
      ).subscribe();
    } else {
      // If there is no token, just clear the local state
      this.clearStorage();
      this.currentUser.set(null);
    }
  }

  public checkUsernameExists(username: string) {
    return this.http.get<{ exists: boolean }>(`${this.apiUrl}/check-username?username=${username}`);
  }

  public checkEmailExists(email: string) {
    return this.http.get<{ exists: boolean }>(`${this.apiUrl}/check-email?email=${email}`);
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

  private extractUserFromToken(token: string): User | null {
    try {
      let payloadBase64Url = token.split('.')[1];
      let base64 = payloadBase64Url.replace(/-/g, '+').replace(/_/g, '/');

      const pad = base64.length % 4;
      if (pad) {
        if (pad === 1) throw new Error('InvalidLengthError');
        base64 += new Array(5 - pad).join('=');
      }

      const decoded = JSON.parse(window.atob(base64));

      // Map standard JWT claims to your User model. 
      return {
        id: decoded.sub || decoded.nameid || '',
        username: decoded.name || decoded.unique_name || '',
        avatarUrl: decoded.avatar || null
      };
    } catch {
      return null;
    }
  }
}