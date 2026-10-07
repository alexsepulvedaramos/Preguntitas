import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { authInterceptor } from './auth.interceptor';
import { environment } from '../../../environments/environment';

const API = environment.apiUrl;

describe('authInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    localStorage.setItem('access_token', 'expired-access');
    localStorage.setItem('refresh_token', 'refresh-old');

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        provideRouter([{ path: '**', children: [] }]),
      ],
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('refreshes on 401 and retries the request with the new access token', async () => {
    const result = firstValueFrom(http.get<{ ok: boolean }>(`${API}/groups`));

    httpMock.expectOne(`${API}/groups`).flush(null, { status: 401, statusText: 'Unauthorized' });
    httpMock.expectOne(`${API}/auth/refresh`).flush({ accessToken: 'new-access', refreshToken: 'refresh-new' });
    await new Promise(resolve => setTimeout(resolve));

    const retry = httpMock.expectOne(`${API}/groups`);
    expect(retry.request.headers.get('Authorization')).toBe('Bearer new-access');
    retry.flush({ ok: true });

    expect(await result).toEqual({ ok: true });
  });

  it('propagates a transient refresh failure without dropping the session', async () => {
    const result = firstValueFrom(http.get(`${API}/groups`));

    httpMock.expectOne(`${API}/groups`).flush(null, { status: 401, statusText: 'Unauthorized' });
    httpMock.expectOne(`${API}/auth/refresh`).flush(null, { status: 502, statusText: 'Bad Gateway' });

    await expect(result).rejects.toMatchObject({ status: 502 });
    expect(localStorage.getItem('refresh_token')).toBe('refresh-old');
  });
});
