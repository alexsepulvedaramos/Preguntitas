import { Directive, booleanAttribute, input, output, signal } from '@angular/core';

const HOLD_MS = 420;
const MOVE_TOLERANCE_PX = 10;

// Press and hold on touch screens (rama 56: reacting to a written answer without a button
// per answer). A finger that moves is scrolling, not holding. With a mouse there is nothing
// to scroll past, so a plain click counts — unless it ended a text selection.
@Directive({
  selector: '[appLongPress]',
  exportAs: 'appLongPress',
  host: {
    '(pointerdown)': 'onDown($event)',
    '(pointermove)': 'onMove($event)',
    '(pointerup)': 'cancel()',
    '(pointercancel)': 'cancel()',
    '(pointerleave)': 'cancel()',
    '(contextmenu)': 'onContextMenu($event)',
    '(click)': 'onClick($event)',
  },
})
export class LongPressDirective {
  readonly longPressDisabled = input(false, { transform: booleanAttribute });
  readonly longPress = output<void>();

  // True while a finger is down and the hold hasn't fired yet, for the pressed look.
  readonly pressing = signal(false);

  private timer: ReturnType<typeof setTimeout> | null = null;
  private start = { x: 0, y: 0 };
  private touch = false;

  protected onDown(event: PointerEvent) {
    this.touch = event.pointerType !== 'mouse';
    if (this.longPressDisabled() || !this.touch) return;
    this.start = { x: event.clientX, y: event.clientY };
    this.pressing.set(true);
    this.timer = setTimeout(() => {
      this.timer = null;
      this.pressing.set(false);
      // A short buzz where the device has one, like the system's own long press.
      navigator.vibrate?.(10);
      this.longPress.emit();
      // After emitting: what the listener does may itself click something.
      this.swallowNextClick();
    }, HOLD_MS);
  }

  protected onMove(event: PointerEvent) {
    if (!this.timer) return;
    if (Math.hypot(event.clientX - this.start.x, event.clientY - this.start.y) > MOVE_TOLERANCE_PX) this.cancel();
  }

  protected cancel() {
    if (this.timer) clearTimeout(this.timer);
    this.timer = null;
    this.pressing.set(false);
  }

  // Lifting the finger after a hold can still produce a click. What the hold opened (a
  // popover) would take it for a tap outside and close at once, so that one click is dropped.
  private swallowNextClick() {
    const swallow = (event: Event) => {
      event.stopPropagation();
      event.preventDefault();
    };
    const stop = () => document.removeEventListener('click', swallow, true);
    document.addEventListener('click', swallow, { capture: true, once: true });
    const release = () => setTimeout(stop, 350);
    document.addEventListener('pointerup', release, { capture: true, once: true });
    document.addEventListener('pointercancel', release, { capture: true, once: true });
  }

  // The system's own long-press menu would cover ours.
  protected onContextMenu(event: Event) {
    if (this.touch && !this.longPressDisabled()) event.preventDefault();
  }

  protected onClick(event: MouseEvent) {
    if (this.longPressDisabled() || this.touch || event.detail === 0) return;
    if (!window.getSelection()?.isCollapsed) return;
    this.longPress.emit();
  }
}
