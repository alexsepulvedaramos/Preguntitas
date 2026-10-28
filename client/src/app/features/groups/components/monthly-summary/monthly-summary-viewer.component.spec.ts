import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { MonthlySummaryViewerComponent } from './monthly-summary-viewer.component';
import { MonthlySummary } from '../../../../core/models/monthly-summary.model';
import { GroupStreaksService } from '../../services/group-streaks.service';

const ana = { userId: 1, username: 'ana', avatarUrl: null };
const bea = { userId: 2, username: 'bea', avatarUrl: null };

const summary = (withMatch: boolean): MonthlySummary => ({
  month: '2026-10',
  questions: 2,
  participation: 50,
  awards: [{ key: 'superlative', value: 2, winners: [{ person: bea, questionText: null }] }],
  members: [
    { person: ana, answered: 2, eligible: 2, percent: 100, bestRun: 2 },
    { person: bea, answered: 1, eligible: 2, percent: 50, bestRun: 1 },
  ],
  myMatch: withMatch ? { people: [bea], count: 3 } : null,
});

describe('MonthlySummaryViewerComponent', () => {
  let fixture: ComponentFixture<MonthlySummaryViewerComponent>;
  let http: HttpTestingController;

  const open = async (withMatch = true) => {
    fixture.detectChanges();
    http.expectOne((r) => r.url.endsWith('/groups/7/monthly-summaries/2026-10')).flush(summary(withMatch));
    await fixture.whenStable();
    fixture.detectChanges();
  };

  const kinds = () => (fixture.componentInstance as unknown as { slides: () => { kind: string }[] }).slides().map((s) => s.kind);
  const index = () => (fixture.componentInstance as unknown as { index: () => number }).index();

  beforeEach(async () => {
    localStorage.clear();
    await TestBed.configureTestingModule({
      imports: [MonthlySummaryViewerComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        // The award slides look up the winners' plates in the group's member list (rama 60).
        { provide: GroupStreaksService, useValue: { ensure: () => {}, lookFor: () => null, streakFor: () => null } },
      ],
    }).compileComponents();

    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(MonthlySummaryViewerComponent);
    fixture.componentRef.setInput('groupId', 7);
    fixture.componentRef.setInput('groupName', 'Los de siempre');
    fixture.componentRef.setInput('months', [{ month: '2026-10', questions: 2 }]);
    fixture.componentRef.setInput('initialMonth', '2026-10');
  });

  afterEach(() => http.verify());

  it('builds one slide per award, then the match and the members', async () => {
    await open();
    expect(kinds()).toEqual(['intro', 'participation', 'award', 'match', 'members']);
  });

  it('skips the match slide when there is no match', async () => {
    await open(false);
    expect(kinds()).not.toContain('match');
  });

  it('marks the month as seen once it loads', async () => {
    await open();
    expect(localStorage.getItem('vp_summary_seen_7')).toBe('2026-10');
  });

  it('moves forward and back without leaving the story', async () => {
    await open();
    const viewer = fixture.componentInstance;
    viewer.previous();
    expect(index()).toBe(0);
    for (let i = 0; i < 10; i++) viewer.next();
    expect(index()).toBe(kinds().length - 1);
    viewer.previous();
    expect(index()).toBe(kinds().length - 2);
  });

  it('emits closed on close', async () => {
    await open();
    let closed = false;
    fixture.componentInstance.closed.subscribe(() => (closed = true));
    fixture.componentInstance.close();
    expect(closed).toBe(true);
  });
});
