import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { AuthService } from './auth-service';
import { LoginRequest } from '../../features/auth/models/login-request.interface';
import { AuthResponse } from '../../features/auth/models/auth-response.interface';

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
        provideHttpClientTesting()
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
    const mockCredentials: LoginRequest = { identifier: 'test@test.com', password: 'password' };
    const mockResponse: AuthResponse = { userId: 1, username: 'testuser', email: 'test@test.com', accessToken: 'access-123', accessTokenExpiresAt: new Date(), refreshToken: 'refresh-456', refreshTokenExpiresAt: new Date(), avatarUrl: null };

    service.login(mockCredentials).subscribe();

    // Intercept the HTTP request
    const req = httpMock.expectOne('/api/auth/login');
    expect(req.request.method).toBe('POST');

    // Simulate server response
    req.flush(mockResponse);

    // Verify side effects
    expect(localStorage.getItem('access_token')).toBe('access-123');
    expect(localStorage.getItem('refresh_token')).toBe('refresh-456');
    expect(service.isAuthenticated()).toBeTruthy();
  });

  it('should clear tokens and set unauthenticated state on logout', () => {
    // Setup initial state manually
    localStorage.setItem('access_token', 'token');
    localStorage.setItem('refresh_token', 'refresh');
    service.isAuthenticated.set(true);

    service.logout();

    // Verify side effects
    expect(localStorage.getItem('access_token')).toBeNull();
    expect(localStorage.getItem('refresh_token')).toBeNull();
    expect(service.isAuthenticated()).toBeFalsy();
  });
});