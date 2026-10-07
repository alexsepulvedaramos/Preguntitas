import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthService } from './auth.service';
import { LoginRequest } from '../../features/auth/models/login-request.interface';
import { AuthResponse } from '../../features/auth/models/auth-response.interface';
import { environment } from '../../../environments/environment';

const AUTH_API = `${environment.apiUrl}/auth`;

// Builds an unsigned JWT-shaped token; only the payload matters to the client
const makeToken = (expSecondsFromNow: number, username = 'testuser') => {
  const payload = btoa(JSON.stringify({
    nameid: '1',
    unique_name: username,
    email: 'test@test.com',
    exp: Math.floor(Date.now() / 1000) + expSecondsFromNow,
  })).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
  return `header.${payload}.signature`;
};

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    // Clear localStorage before each test
    localStorage.clear();

    TestBed.configureTestingModule({
      providers: [
        AuthService,
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([{ path: '**', children: [] }])
      ]
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    // Ensure no outstanding requests remain
    httpMock.verify();
  });

  it('should save tokens and set authenticated state on login', () => {
    const LOGIN_TOKEN = makeToken(900);
    const mockCredentials: LoginRequest = { identifier: 'test@test.com', password: 'password' };
    const mockResponse: AuthResponse = { userId: 1, username: 'testuser', email: 'test@test.com', accessToken: LOGIN_TOKEN, accessTokenExpiresAt: new Date().toISOString(), refreshToken: 'refresh-456', refreshTokenExpiresAt: new Date().toISOString(), avatarUrl: null };

    service.login(mockCredentials).subscribe();

    // Intercept the HTTP request
    const req = httpMock.expectOne(`${AUTH_API}/login`);
    expect(req.request.method).toBe('POST');

    // Simulate server response
    req.flush(mockResponse);

    // Verify side effects
    expect(localStorage.getItem('access_token')).toBe(LOGIN_TOKEN);
    expect(localStorage.getItem('refresh_token')).toBe('refresh-456');
    expect(service.isAuthenticated()).toBeTruthy();
  });

  it('should clear tokens and set unauthenticated state on logout', () => {
    // Setup initial state manually
    localStorage.setItem('access_token', 'token');
    localStorage.setItem('refresh_token', 'refresh');
    service.logout();
    httpMock.expectOne(`${AUTH_API}/logout`).flush(null, { status: 204, statusText: 'No Content' });

    // Verify side effects
    expect(localStorage.getItem('access_token')).toBeNull();
    expect(localStorage.getItem('refresh_token')).toBeNull();
    expect(service.isAuthenticated()).toBeFalsy();
  });

  describe('session refresh', () => {
    const refreshResponse = (accessToken: string, refreshToken: string): AuthResponse => ({
      userId: 1, username: 'testuser', email: 'test@test.com', avatarUrl: null,
      accessToken, accessTokenExpiresAt: new Date().toISOString(),
      refreshToken, refreshTokenExpiresAt: new Date().toISOString(),
    });

    const flushMicrotasks = () => new Promise(resolve => setTimeout(resolve));

    beforeEach(() => {
      localStorage.setItem('access_token', makeToken(-60));
      localStorage.setItem('refresh_token', 'refresh-old');
    });

    it('rotates and stores the new tokens on success', async () => {
      const fresh = makeToken(900);
      const result = firstValueFrom(service.verifySession());

      const req = httpMock.expectOne(`${AUTH_API}/refresh`);
      expect(req.request.body).toEqual({ refreshToken: 'refresh-old' });
      req.flush(refreshResponse(fresh, 'refresh-new'));

      expect(await result).toBe(true);
      expect(localStorage.getItem('access_token')).toBe(fresh);
      expect(localStorage.getItem('refresh_token')).toBe('refresh-new');
      expect(service.currentUser()?.username).toBe('testuser');
    });

    it('keeps the session when the refresh fails transiently (offline / backend waking up)', async () => {
      const result = firstValueFrom(service.verifySession());

      httpMock.expectOne(`${AUTH_API}/refresh`)
        .flush('Service Unavailable', { status: 503, statusText: 'Service Unavailable' });

      expect(await result).toBe(true);
      expect(localStorage.getItem('refresh_token')).toBe('refresh-old');
      expect(service.currentUser()?.username).toBe('testuser');
    });

    it('keeps the session on a network error', async () => {
      const result = firstValueFrom(service.verifySession());

      httpMock.expectOne(`${AUTH_API}/refresh`).error(new ProgressEvent('error'));

      expect(await result).toBe(true);
      expect(localStorage.getItem('refresh_token')).toBe('refresh-old');
    });

    it('ends the session locally when the server rejects the refresh token', async () => {
      const result = firstValueFrom(service.verifySession());

      httpMock.expectOne(`${AUTH_API}/refresh`)
        .flush('Invalid or expired refresh token.', { status: 401, statusText: 'Unauthorized' });

      expect(await result).toBe(false);
      expect(localStorage.getItem('access_token')).toBeNull();
      expect(localStorage.getItem('refresh_token')).toBeNull();
      expect(service.isAuthenticated()).toBe(false);
      // No /logout call: it could revoke a token another tab has just obtained
      httpMock.expectNone(`${AUTH_API}/logout`);
    });

    it('retries with the newer refresh token when another tab rotated it mid-flight', async () => {
      const fresh = makeToken(900);
      const result = firstValueFrom(service.refreshToken());

      const first = httpMock.expectOne(`${AUTH_API}/refresh`);
      // Another tab finishes its own refresh while ours is in flight
      localStorage.setItem('refresh_token', 'refresh-from-other-tab');
      first.flush('Invalid or expired refresh token.', { status: 401, statusText: 'Unauthorized' });
      await flushMicrotasks();

      const retry = httpMock.expectOne(`${AUTH_API}/refresh`);
      expect(retry.request.body).toEqual({ refreshToken: 'refresh-from-other-tab' });
      retry.flush(refreshResponse(fresh, 'refresh-newest'));

      expect(await result).toBe(fresh);
      expect(localStorage.getItem('refresh_token')).toBe('refresh-newest');
    });

    it('sends a single refresh request for concurrent callers', async () => {
      const fresh = makeToken(900);
      const a = firstValueFrom(service.refreshToken());
      const b = firstValueFrom(service.refreshToken());

      httpMock.expectOne(`${AUTH_API}/refresh`).flush(refreshResponse(fresh, 'refresh-new'));

      expect(await a).toBe(fresh);
      expect(await b).toBe(fresh);
    });
  });
});