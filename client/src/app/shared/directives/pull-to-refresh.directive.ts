import { Directive, OnDestroy, OnInit, effect, inject, input, output } from '@angular/core';
import { DOCUMENT } from '@angular/common';

@Directive({
  selector: '[appPullToRefresh]',
  standalone: true,
})
export class PullToRefreshDirective implements OnInit, OnDestroy {
  readonly isRefreshing = input<boolean>(false);
  readonly pullRefresh = output<void>();

  private readonly THRESHOLD = 72; // px of resistance-adjusted pull to fire
  private readonly MAX_VISUAL = 96;

  private startY = 0;
  private currentPull = 0;
  private active = false;
  private wasRefreshing = false;
  private indicator: HTMLElement | null = null;

  private readonly doc = inject(DOCUMENT);

  private readonly boundStart = (e: TouchEvent) => this.onStart(e);
  private readonly boundMove = (e: TouchEvent) => this.onMove(e);
  private readonly boundEnd = () => this.onEnd();

  constructor() {
    // Hide indicator when the parent signals that refreshing finished
    effect(() => {
      const refreshing = this.isRefreshing();
      if (this.wasRefreshing && !refreshing) this.hideIndicator();
      this.wasRefreshing = refreshing;
    });
  }

  ngOnInit() {
    this.buildIndicator();
    const win = this.doc.defaultView!;
    win.addEventListener('touchstart', this.boundStart, { passive: true });
    win.addEventListener('touchmove', this.boundMove, { passive: false });
    win.addEventListener('touchend', this.boundEnd);
  }

  ngOnDestroy() {
    const win = this.doc.defaultView!;
    win.removeEventListener('touchstart', this.boundStart);
    win.removeEventListener('touchmove', this.boundMove);
    win.removeEventListener('touchend', this.boundEnd);
    this.indicator?.remove();
  }

  // ── Touch handlers ────────────────────────────────────────────────────────

  private onStart(e: TouchEvent) {
    if (this.belongsElsewhere(e.target)) return;
    if ((this.doc.defaultView?.scrollY ?? 0) === 0) {
      this.startY = e.touches[0].clientY;
      this.active = true;
    }
  }

  private onMove(e: TouchEvent) {
    if (!this.active) return;
    const delta = e.touches[0].clientY - this.startY;
    if (delta <= 0) { this.currentPull = 0; this.setProgress(0); return; }

    // Prevent native scroll bounce while pulling
    if ((this.doc.defaultView?.scrollY ?? 0) === 0 && delta > 0) e.preventDefault();

    // Apply resistance so it feels like pulling a rubber band
    this.currentPull = Math.min(this.MAX_VISUAL, Math.sqrt(delta) * 5);
    this.setProgress(this.currentPull);
  }

  private onEnd() {
    if (!this.active) return;
    this.active = false;

    if (this.currentPull >= this.THRESHOLD) {
      this.showSpinner();
      this.pullRefresh.emit();
    } else {
      this.hideIndicator();
    }
    this.currentPull = 0;
  }

  // The gesture only belongs to the page itself. Touches inside overlays (drawers, dialogs,
  // the member card, the question picker, the streak celebration) or inside any inner list
  // that is scrolled away from its top are left alone — otherwise swiping a drawer down
  // reloads the page and a scrolled list can't scroll back up.
  private belongsElsewhere(target: EventTarget | null): boolean {
    if (this.doc.documentElement.classList.contains('cdk-global-scrollblock')) return true;
    if (!(target instanceof Element)) return false;
    if (target.closest('.cdk-overlay-container, [role="dialog"], [data-vaul-drawer-direction]')) return true;

    const win = this.doc.defaultView;
    for (let el: Element | null = target; el && el !== this.doc.body; el = el.parentElement) {
      if (el.scrollTop > 0 && el.scrollHeight > el.clientHeight) {
        const overflowY = win?.getComputedStyle(el).overflowY;
        if (overflowY === 'auto' || overflowY === 'scroll') return true;
      }
    }
    return false;
  }

  // ── Indicator DOM ─────────────────────────────────────────────────────────

  private buildIndicator() {
    const el = this.doc.createElement('div');
    el.setAttribute('aria-hidden', 'true');
    Object.assign(el.style, {
      position: 'fixed',
      top: '56px', // below the sticky header
      left: '50%',
      transform: 'translateX(-50%) translateY(-64px)',
      zIndex: '9999',
      width: '36px',
      height: '36px',
      borderRadius: '50%',
      background: 'var(--card)',
      border: '1px solid var(--border)',
      boxShadow: '0 2px 8px rgba(0,0,0,0.12)',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      opacity: '0',
      transition: 'opacity 0.15s ease',
      pointerEvents: 'none',
    });
    el.innerHTML = this.arrowSvg(false);
    this.doc.body.appendChild(el);
    this.indicator = el;
  }

  private setProgress(pull: number) {
    if (!this.indicator) return;
    const progress = Math.min(1, pull / this.THRESHOLD);
    const offsetY = pull * 0.75; // how far it peeks out below the header
    this.indicator.style.transform = `translateX(-50%) translateY(${offsetY - 64}px)`;
    this.indicator.style.opacity = String(Math.min(1, progress * 1.5));
    // Flip arrow to upward when threshold reached
    const svg = this.indicator.querySelector<SVGElement>('svg');
    if (svg) {
      svg.style.transform = `rotate(${progress >= 1 ? 180 : 0}deg)`;
      svg.style.transition = 'transform 0.2s ease';
      svg.style.color = progress >= 1 ? 'var(--primary)' : 'var(--muted-foreground)';
    }
  }

  private showSpinner() {
    if (!this.indicator) return;
    this.indicator.style.transform = 'translateX(-50%) translateY(0)';
    this.indicator.style.opacity = '1';
    this.indicator.innerHTML = this.spinnerSvg();
  }

  private hideIndicator() {
    if (!this.indicator) return;
    this.indicator.style.opacity = '0';
    // Restore arrow after the fade-out
    setTimeout(() => {
      if (this.indicator) {
        this.indicator.style.transform = 'translateX(-50%) translateY(-64px)';
        this.indicator.innerHTML = this.arrowSvg(false);
      }
    }, 200);
  }

  // ── SVG helpers ───────────────────────────────────────────────────────────

  private arrowSvg(flipped: boolean) {
    return `<svg width="16" height="16" viewBox="0 0 24 24" fill="none"
      stroke="var(--muted-foreground)" stroke-width="2.5"
      stroke-linecap="round" stroke-linejoin="round"
      style="transition:transform 0.2s ease;transform:rotate(${flipped ? 180 : 0}deg)">
      <path d="M12 5v14"/><path d="m19 12-7 7-7-7"/>
    </svg>`;
  }

  private spinnerSvg() {
    // Inject keyframe once
    const styleId = 'ptr-spin-kf';
    if (!this.doc.getElementById(styleId)) {
      const s = this.doc.createElement('style');
      s.id = styleId;
      s.textContent = '@keyframes ptr-spin{to{transform:rotate(360deg)}}';
      this.doc.head.appendChild(s);
    }
    return `<svg width="16" height="16" viewBox="0 0 24 24" fill="none"
      stroke="var(--primary)" stroke-width="2.5"
      stroke-linecap="round" stroke-linejoin="round"
      style="animation:ptr-spin 0.75s linear infinite">
      <path d="M21 12a9 9 0 1 1-6.219-8.56"/>
    </svg>`;
  }
}
