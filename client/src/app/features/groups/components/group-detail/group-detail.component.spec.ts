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
});
