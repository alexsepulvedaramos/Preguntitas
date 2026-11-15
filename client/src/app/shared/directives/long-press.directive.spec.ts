import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { LongPressDirective } from './long-press.directive';

@Component({
  imports: [LongPressDirective],
  template: `<div appLongPress [longPressDisabled]="disabled" (longPress)="fired = fired + 1">hola</div>`,
})
class HostComponent {
  disabled = false;
  fired = 0;
}

function pointer(type: string, pointerType: string, x = 0, y = 0): Event {
  const event = new MouseEvent(type, { bubbles: true, clientX: x, clientY: y });
  Object.defineProperty(event, 'pointerType', { value: pointerType });
  return event;
}

describe('LongPressDirective', () => {
  function setup() {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    return { fixture, host: fixture.componentInstance, el: fixture.nativeElement.querySelector('div') as HTMLElement };
  }

  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  it('fires after holding a finger still', () => {
    const { host, el } = setup();
    el.dispatchEvent(pointer('pointerdown', 'touch'));
    vi.advanceTimersByTime(400);
    expect(host.fired).toBe(0);
    vi.advanceTimersByTime(50);
    expect(host.fired).toBe(1);
  });

  it('does not fire when the finger lifts early or scrolls', () => {
    const { host, el } = setup();
    el.dispatchEvent(pointer('pointerdown', 'touch'));
    el.dispatchEvent(pointer('pointerup', 'touch'));
    vi.advanceTimersByTime(600);
    el.dispatchEvent(pointer('pointerdown', 'touch'));
    el.dispatchEvent(pointer('pointermove', 'touch', 0, 30));
    vi.advanceTimersByTime(600);
    expect(host.fired).toBe(0);
  });

  it('fires on a plain mouse click, and never while disabled', () => {
    const { fixture, host, el } = setup();
    el.dispatchEvent(pointer('pointerdown', 'mouse'));
    el.dispatchEvent(new MouseEvent('click', { bubbles: true, detail: 1 }));
    expect(host.fired).toBe(1);

    host.disabled = true;
    fixture.changeDetectorRef.markForCheck();
    fixture.detectChanges();
    el.dispatchEvent(new MouseEvent('click', { bubbles: true, detail: 1 }));
    el.dispatchEvent(pointer('pointerdown', 'touch'));
    vi.advanceTimersByTime(600);
    expect(host.fired).toBe(1);
  });
});
