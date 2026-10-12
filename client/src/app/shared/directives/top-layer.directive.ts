import { AfterViewInit, Directive, ElementRef, OnDestroy, inject } from '@angular/core';

/**
 * Keeps an element (the toaster) above dialogs and drawers. Angular CDK opens overlays as
 * native popovers, in the browser's top layer, where no z-index can reach them — so a toast
 * fired from a drawer showed up behind it. This makes the host a manual popover too and
 * re-opens it whenever a child is added: the most recently opened popover paints on top.
 * The utility classes undo the UA popover box so the host stays an invisible, 0×0 anchor
 * for its own fixed-position children.
 */
@Directive({
  selector: '[appTopLayer]',
  standalone: true,
  host: {
    popover: 'manual',
    class: 'inset-auto m-0 size-0 overflow-visible border-0 bg-transparent p-0',
  },
})
export class TopLayerDirective implements AfterViewInit, OnDestroy {
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;
  private observer?: MutationObserver;

  ngAfterViewInit(): void {
    if (typeof this.host.showPopover !== 'function') return; // older browsers: plain stacking
    this.raise();
    this.observer = new MutationObserver((mutations) => {
      if (mutations.some((m) => m.addedNodes.length > 0)) this.raise();
    });
    this.observer.observe(this.host, { childList: true, subtree: true });
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
  }

  private raise(): void {
    try {
      if (this.host.matches(':popover-open')) this.host.hidePopover();
      this.host.showPopover();
    } catch {
      // Detached or not supported: nothing to raise.
    }
  }
}
