import { TestBed } from '@angular/core/testing';

import { InviteQrComponent } from './invite-qr.component';
import { I18nService } from '../../../../core/i18n/i18n.service';

describe('InviteQrComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [InviteQrComponent] }).compileComponents();
    await TestBed.inject(I18nService).use('es', false);
  });

  async function render(code: string) {
    const fixture = TestBed.createComponent(InviteQrComponent);
    fixture.componentRef.setInput('invitationCode', code);
    fixture.componentRef.setInput('groupName', 'Los de siempre');
    fixture.detectChanges();
    // The generator is imported lazily; wait for it to resolve and render.
    await vi.waitFor(() => {
      fixture.detectChanges();
      expect(fixture.nativeElement.querySelector('svg[role=img] path')).not.toBeNull();
    });
    return fixture.nativeElement as HTMLElement;
  }

  it('draws the code of the invite link with its quiet zone', async () => {
    const el = await render('ABC123');
    const svg = el.querySelector('svg[role=img]')!;
    const [, , w, h] = svg.getAttribute('viewBox')!.split(' ').map(Number);

    // At least a version-1 QR (21 modules) plus 4 modules of margin on each side.
    expect(w).toBe(h);
    expect(w).toBeGreaterThanOrEqual(21 + 8);
    expect(svg.getAttribute('aria-label')).toBe('Código QR para unirse a Los de siempre');
    expect(svg.querySelector('path')!.getAttribute('d')).toMatch(/^M4 4h1v1h-1z/);
  });

  it('redraws when the invite code is regenerated', async () => {
    const fixture = TestBed.createComponent(InviteQrComponent);
    fixture.componentRef.setInput('invitationCode', 'ABC123');
    fixture.componentRef.setInput('groupName', 'Los de siempre');
    const pathOf = () => fixture.nativeElement.querySelector('svg[role=img] path')?.getAttribute('d');
    await vi.waitFor(() => {
      fixture.detectChanges();
      expect(pathOf()).toBeTruthy();
    });
    const before = pathOf();

    fixture.componentRef.setInput('invitationCode', 'ZZZ999');
    await vi.waitFor(() => {
      fixture.detectChanges();
      expect(pathOf()).not.toBe(before);
    });
  });
});
