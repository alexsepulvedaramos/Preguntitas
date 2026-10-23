import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';

import { DAILY_POLL_INTERVAL_MS, GroupDetailComponent } from './group-detail.component';

describe('GroupDetailComponent', () => {
  let component: GroupDetailComponent;
  let fixture: ComponentFixture<GroupDetailComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GroupDetailComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(GroupDetailComponent);
    fixture.componentRef.setInput('groupId', '1');
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

describe('GroupDetailComponent live refresh', () => {
  let visibility: DocumentVisibilityState;
  let http: HttpTestingController;
  let fixture: ComponentFixture<GroupDetailComponent>;

  const daily = (status: 'voting' | 'results' | 'no_question', totalVotes = 1) => ({
    today: {
      status,
      question: { id: 5, text: 'q', type: 0 },
      userHasVoted: status === 'results',
      results: status === 'results' ? { id: 5, text: 'q', type: 0, totalVotes, results: [], freeTextResponses: [] } : null,
      closesAt: null,
      voterIds: [],
      memberCount: 2,
      guess: null,
    },
    selection: null,
    myStreak: {
      current: 0,
      best: 0,
      lostStreak: null,
      freezes: 1,
      maxFreezes: 2,
      freezeSavedStreak: null,
      freezeGiftFrom: null,
      repairableStreak: null,
      freezePrice: 100,
      repairPrice: 300,
    },
  });

  const flushInitial = (status: 'voting' | 'results' | 'no_question') => {
    http.expectOne((r) => r.url.endsWith('/daily/current')).flush(daily(status));
    http.expectOne((r) => r.url.endsWith('/members')).flush([]);
    http.expectOne((r) => /\/groups\/1$/.test(r.url)).flush({ id: 1, name: 'g' });
  };

  const currentRequests = () => http.match((r) => r.url.endsWith('/daily/current'));

  beforeEach(async () => {
    vi.useFakeTimers();
    visibility = 'visible';
    vi.spyOn(document, 'visibilityState', 'get').mockImplementation(() => visibility);

    await TestBed.configureTestingModule({
      imports: [GroupDetailComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(GroupDetailComponent);
    fixture.componentRef.setInput('groupId', '1');
  });

  afterEach(() => {
    fixture.destroy();
    vi.useRealTimers();
    vi.restoreAllMocks();
  });

  it('polls daily/current every 30 s while in results and updates the vote count', () => {
    fixture.detectChanges();
    flushInitial('results');

    vi.advanceTimersByTime(DAILY_POLL_INTERVAL_MS);
    const reqs = currentRequests();
    expect(reqs.length).toBe(1);
    reqs[0].flush(daily('results', 4));
    expect(fixture.componentInstance.daily()?.today.results?.totalVotes).toBe(4);
    expect(fixture.componentInstance.loading()).toBe(false);
  });

  it('does not poll when the group has no open question', () => {
    fixture.detectChanges();
    flushInitial('no_question');
    vi.advanceTimersByTime(DAILY_POLL_INTERVAL_MS * 3);
    expect(currentRequests().length).toBe(0);
  });

  it('does not poll while hidden, and refreshes when visible again', () => {
    fixture.detectChanges();
    flushInitial('voting');

    visibility = 'hidden';
    document.dispatchEvent(new Event('visibilitychange'));
    vi.advanceTimersByTime(DAILY_POLL_INTERVAL_MS * 3);
    expect(currentRequests().length).toBe(0);

    visibility = 'visible';
    document.dispatchEvent(new Event('visibilitychange'));
    vi.advanceTimersByTime(0);
    expect(currentRequests().length).toBe(1);
  });

  it('does not overlap requests while one is pending', () => {
    fixture.detectChanges();
    flushInitial('results');

    vi.advanceTimersByTime(DAILY_POLL_INTERVAL_MS * 3);
    expect(currentRequests().length).toBe(1);
  });

  it('keeps a local vote when a stale poll says not voted', () => {
    fixture.detectChanges();
    flushInitial('results');
    fixture.componentInstance.daily.update((d) => d && { ...d, today: { ...d.today, userHasVoted: true } });

    vi.advanceTimersByTime(DAILY_POLL_INTERVAL_MS);
    const stale = daily('results');
    stale.today.userHasVoted = false;
    currentRequests()[0].flush(stale);
    expect(fixture.componentInstance.daily()?.today.userHasVoted).toBe(true);
  });

  // ── Question deck: today's question + flash questions (rama 44) ──

  const flash = (id: number, userHasVoted = false) => ({
    id,
    question: { id: 100 + id, text: `flash ${id}`, type: 0 },
    launchedBy: { id: 9, username: 'ana', avatarUrl: null, frameColor: null },
    // Relative to now: a closesAt in the past makes the card's countdown reach zero and reload.
    activatedAt: new Date(Date.now() - 60 * 60_000).toISOString(),
    closesAt: new Date(Date.now() + 23 * 60 * 60_000).toISOString(),
    userHasVoted,
    results: userHasVoted ? { id: 100 + id, text: `flash ${id}`, type: 0, totalVotes: 1, results: [], freeTextResponses: [] } : null,
    voteCount: userHasVoted ? 1 : 0,
    voterIds: [],
    canChangeVote: false,
    anonymousAuthor: false,
    anonymousVotes: false,
    capsuleOpensAt: null as string | null,
    sealed: false,
    extended: false,
    extendedBy: null,
    isMine: false,
  });

  const flushWithFlashes = (status: 'voting' | 'results', flashes: unknown[]) => {
    http.expectOne((r) => r.url.endsWith('/daily/current')).flush({ ...daily(status), flashes });
    http.expectOne((r) => r.url.endsWith('/members')).flush([]);
    http.expectOne((r) => /\/groups\/1$/.test(r.url)).flush({ id: 1, name: 'g' });
  };

  const activeKey = () => fixture.componentInstance['activeCard']()?.key;

  it('opens on the first unanswered question of the deck', () => {
    fixture.detectChanges();
    flushWithFlashes('results', [flash(1, true), flash(2)]);

    expect(fixture.componentInstance['deck']().map((c) => c.key)).toEqual(['daily', 'flash-1', 'flash-2']);
    expect(activeKey()).toBe('flash-2');
  });

  it('stays on a flash after voting it, so its results show', () => {
    fixture.detectChanges();
    flushWithFlashes('voting', [flash(1)]);
    fixture.componentInstance.goTo('flash-1');

    fixture.componentInstance.onFlashVoted(flash(1, true) as never);
    vi.advanceTimersByTime(1500);
    expect(activeKey()).toBe('flash-1');
  });

  it('polls while a flash question is open, keeping a local flash vote', () => {
    fixture.detectChanges();
    http.expectOne((r) => r.url.endsWith('/daily/current')).flush({ ...daily('no_question'), flashes: [flash(1)] });
    http.expectOne((r) => r.url.endsWith('/members')).flush([]);
    http.expectOne((r) => /\/groups\/1$/.test(r.url)).flush({ id: 1, name: 'g' });
    fixture.componentInstance.onFlashVoted(flash(1, true) as never);

    vi.advanceTimersByTime(DAILY_POLL_INTERVAL_MS);
    currentRequests()[0].flush({ ...daily('no_question'), flashes: [flash(1)] });
    expect(fixture.componentInstance.daily()?.flashes[0].userHasVoted).toBe(true);
  });

  // ── Flash variants (rama 50) ──

  const showFlash = (state: unknown) => {
    fixture.detectChanges();
    http.expectOne((r) => r.url.endsWith('/daily/current')).flush({ ...daily('no_question'), flashes: [state] });
    http.expectOne((r) => r.url.endsWith('/members')).flush([]);
    http.expectOne((r) => /\/groups\/1$/.test(r.url)).flush({ id: 1, name: 'g' });
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  };

  it('an anonymous flash names nobody and lists no voters', () => {
    const page = showFlash({ ...flash(1), launchedBy: null, anonymousAuthor: true, anonymousVotes: true, voteCount: 2 });

    expect(page.querySelector('app-today-voters')).toBeNull();
    // Not yours and not extended: no «+24 h» (only its author can extend it)
    expect(page.querySelector('app-flash-extend')).toBeNull();
    expect(page.textContent).not.toContain('ana');
  });

  it('anonymous author alone keeps the voters row', () => {
    const page = showFlash({ ...flash(1), launchedBy: null, anonymousAuthor: true });

    expect(page.querySelector('app-today-voters')).not.toBeNull();
  });

  it('a sealed capsule you voted shows no results and no vote form', () => {
    const opensAt = new Date(Date.now() + 30 * 24 * 60 * 60_000).toISOString();
    const page = showFlash({ ...flash(1), userHasVoted: true, results: null, voteCount: 1, sealed: true, capsuleOpensAt: opensAt });

    expect(page.querySelector('app-results')).toBeNull();
    expect(page.querySelector('app-vote')).toBeNull();
  });
});
