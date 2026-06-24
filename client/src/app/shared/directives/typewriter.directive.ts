import { Directive, ElementRef, Input, OnChanges, OnDestroy, OnInit } from '@angular/core';

const CHAR_DELAY_MS = 28;

@Directive({
  selector: '[appTypewriter]',
  standalone: true,
})
export class TypewriterDirective implements OnInit, OnChanges, OnDestroy {
  @Input('appTypewriter') text = '';

  private intervalId?: ReturnType<typeof setInterval>;
  private currentIndex = 0;

  constructor(private readonly el: ElementRef<HTMLElement>) {}

  ngOnInit() {
    this.start();
  }

  ngOnChanges() {
    this.start();
  }

  ngOnDestroy() {
    this.stop();
  }

  private start() {
    this.stop();
    this.currentIndex = 0;
    this.el.nativeElement.textContent = '';

    if (!this.text) return;

    this.intervalId = setInterval(() => {
      if (this.currentIndex < this.text.length) {
        this.el.nativeElement.textContent += this.text[this.currentIndex];
        this.currentIndex++;
      } else {
        this.stop();
      }
    }, CHAR_DELAY_MS);
  }

  private stop() {
    if (this.intervalId !== undefined) {
      clearInterval(this.intervalId);
      this.intervalId = undefined;
    }
  }
}
