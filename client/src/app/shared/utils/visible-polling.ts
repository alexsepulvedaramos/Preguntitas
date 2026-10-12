import { EMPTY, Observable, defer, fromEvent, merge, of, timer } from 'rxjs';
import { distinctUntilChanged, map, switchMap } from 'rxjs/operators';

// Emits every `intervalMs` while the page is visible and nothing while it is hidden
// (live refresh by polling, spec §10). When the page becomes visible again it emits
// immediately, then resumes the interval. If the page starts visible, the first tick
// comes after one full interval — the caller already did its initial load.
export function visiblePolling(intervalMs: number, doc: Document = document): Observable<void> {
  const isVisible = () => doc.visibilityState === 'visible';

  return defer(() => {
    let first = true;
    return merge(of(isVisible()), fromEvent(doc, 'visibilitychange').pipe(map(isVisible))).pipe(
      distinctUntilChanged(),
      switchMap((visible) => {
        const initial = first;
        first = false;
        return visible ? timer(initial ? intervalMs : 0, intervalMs).pipe(map(() => undefined)) : EMPTY;
      }),
    );
  });
}
