import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { environment } from '../../../environments/environment';
import { AuthService } from '../auth/auth.service';
import { SeenService } from './seen.service';

const URL = `${environment.apiUrl}/users/me/seen`;

describe('SeenService', () => {
  const currentUser = signal<{ id: string } | null>(null);
  let http: HttpTestingController;

  function start(): SeenService {
    const service = TestBed.inject(SeenService);
    TestBed.tick();
    return service;
  }

  beforeEach(() => {
    localStorage.clear();
    currentUser.set(null);
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), { provide: AuthService, useValue: { currentUser } }],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it("answers with the account's marks once the server has replied", () => {
    currentUser.set({ id: '7' });
    const service = start();
    expect(service.ready()).toBe(false);

    http.expectOne({ method: 'GET', url: URL }).flush({ release: '1.34.0', 'hint:flashButton': '1' });

    expect(service.ready()).toBe(true);
    expect(service.get('release')).toBe('1.34.0');
    expect(service.get('hint:changeVote')).toBeNull();
  });

  it('hands over what the device remembered on its own, once', () => {
    localStorage.setItem('vp_release_seen', '1.33.0');
    localStorage.setItem('vp_hints_seen', JSON.stringify(['changeVote']));
    localStorage.setItem('vp_summary_seen_12', '2026-10');
    currentUser.set({ id: '7' });
    const service = start();

    const upload = http.expectOne({ method: 'PUT', url: URL });
    expect(upload.request.body).toEqual({
      marks: { release: '1.33.0', 'hint:changeVote': '1', 'summary:12': '2026-10' },
    });
    // The account had already seen a newer release elsewhere: the server's answer wins.
    upload.flush({ release: '1.34.0', 'hint:changeVote': '1', 'summary:12': '2026-10' });

    expect(service.get('release')).toBe('1.34.0');
    expect(localStorage.getItem('vp_release_seen')).toBeNull();
    expect(localStorage.getItem('vp_summary_seen_12')).toBeNull();
  });

  it('stores a new mark at once and sends it to the server', () => {
    currentUser.set({ id: '7' });
    const service = start();
    http.expectOne({ method: 'GET', url: URL }).flush({});

    service.set('hint:reactionHold', '1');
    expect(service.get('hint:reactionHold')).toBe('1');

    const save = http.expectOne({ method: 'PUT', url: URL });
    expect(save.request.body).toEqual({ marks: { 'hint:reactionHold': '1' } });
    save.flush({ 'hint:reactionHold': '1' });
  });

  it('keeps working offline and retries what was not saved', () => {
    currentUser.set({ id: '7' });
    const service = start();
    http.expectOne({ method: 'GET', url: URL }).error(new ProgressEvent('error'));
    expect(service.ready()).toBe(true);

    service.set('release', '1.34.1');
    http.expectOne({ method: 'PUT', url: URL }).error(new ProgressEvent('error'));
    expect(service.get('release')).toBe('1.34.1');

    service.set('hint:changeVote', '1');
    expect(http.expectOne({ method: 'PUT', url: URL }).request.body).toEqual({
      marks: { release: '1.34.1', 'hint:changeVote': '1' },
    });
  });

  it("never shows one account's marks to another", () => {
    currentUser.set({ id: '7' });
    const service = start();
    http.expectOne({ method: 'GET', url: URL }).flush({ release: '1.34.0' });

    currentUser.set({ id: '8' });
    TestBed.tick();
    expect(service.get('release')).toBeNull();
    http.expectOne({ method: 'GET', url: URL }).flush({});
  });
});
