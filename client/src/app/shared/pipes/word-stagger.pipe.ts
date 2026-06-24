import { Pipe, PipeTransform, inject } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';

@Pipe({ name: 'wordStagger', standalone: true, pure: true })
export class WordStaggerPipe implements PipeTransform {
  private readonly sanitizer = inject(DomSanitizer);

  transform(text: string): SafeHtml {
    if (!text) return '';
    const spans = text
      .split(/\s+/)
      .map((word, i) => `<span style="--i:${i}">${this.escape(word)}</span>`)
      .join(' ');
    return this.sanitizer.bypassSecurityTrustHtml(spans);
  }

  private escape(str: string): string {
    return str.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
  }
}
