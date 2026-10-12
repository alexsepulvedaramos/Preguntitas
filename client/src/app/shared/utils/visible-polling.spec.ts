import { visiblePolling } from './visible-polling';

describe('visiblePolling', () => {
  let state: DocumentVisibilityState;
  let ticks: number;

  const setVisibility = (value: DocumentVisibilityState) => {
    state = value;
    document.dispatchEvent(new Event('visibilitychange'));
  };

  beforeEach(() => {
    vi.useFakeTimers();
    state = 'visible';
    ticks = 0;
    vi.spyOn(document, 'visibilityState', 'get').mockImplementation(() => state);
  });

  afterEach(() => {
    vi.useRealTimers();
    vi.restoreAllMocks();
  });

  it('ticks every interval while visible, not at subscription time', () => {
    const sub = visiblePolling(1000).subscribe(() => ticks++);
    expect(ticks).toBe(0);
    vi.advanceTimersByTime(3000);
    expect(ticks).toBe(3);
    sub.unsubscribe();
  });

  it('stays silent while hidden', () => {
    const sub = visiblePolling(1000).subscribe(() => ticks++);
    setVisibility('hidden');
    vi.advanceTimersByTime(10_000);
    expect(ticks).toBe(0);
    sub.unsubscribe();
  });

  it('refreshes immediately when visible again, then resumes the interval', () => {
    const sub = visiblePolling(1000).subscribe(() => ticks++);
    setVisibility('hidden');
    vi.advanceTimersByTime(5000);
    setVisibility('visible');
    vi.advanceTimersByTime(0);
    expect(ticks).toBe(1);
    vi.advanceTimersByTime(2000);
    expect(ticks).toBe(3);
    sub.unsubscribe();
  });

  it('stops after unsubscribe', () => {
    const sub = visiblePolling(1000).subscribe(() => ticks++);
    sub.unsubscribe();
    vi.advanceTimersByTime(5000);
    expect(ticks).toBe(0);
  });
});
