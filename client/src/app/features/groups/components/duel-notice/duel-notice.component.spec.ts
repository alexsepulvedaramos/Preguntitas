import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { DuelNoticeComponent, duelSides } from './duel-notice.component';
import { AuthService } from '../../../../core/auth/auth.service';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { GroupMember } from '../../models/group.models';

const member = (id: number, username: string) => ({ id, username }) as GroupMember;
const members = [member(1, 'Ana'), member(2, 'Bea'), member(3, 'Carlos')];

// Free duels (rama 47, §17.15 item 6): the notice shown before sending a 1-vs-1 Deathmatch.
describe('DuelNoticeComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DuelNoticeComponent],
      // Signed in as Ana.
      providers: [{ provide: AuthService, useValue: { currentUser: signal({ id: '1' }) } }],
    }).compileComponents();
    await TestBed.inject(I18nService).use('es', false);
  });

  async function renderText(teams: number[][]): Promise<string> {
    const fixture = TestBed.createComponent(DuelNoticeComponent);
    fixture.componentRef.setInput('teams', teams);
    fixture.componentRef.setInput('members', members);
    await fixture.whenStable();
    return (fixture.nativeElement as HTMLElement).textContent!.replace(/\s+/g, ' ').trim();
  }

  it('only treats two teams of one member each as a duel', () => {
    expect(duelSides([[1], [2]])).toEqual([1, 2]);
    expect(duelSides([[1, 3], [2]])).toBeNull();
    expect(duelSides([[1], [2], [3]])).toBeNull();
    expect(duelSides([[1], []])).toBeNull();
    expect(duelSides(null)).toBeNull();
  });

  it('tells you who you are challenging when you are one side', async () => {
    expect(await renderText([[2], [1]])).toBe('Retas a Bea y le avisamos.');
  });

  it('names both sides when you put two others to fight', async () => {
    expect(await renderText([[2], [3]])).toBe('Esto iniciará un duelo entre Bea y Carlos y les avisaremos.');
  });

  it('shows nothing for a team Deathmatch', async () => {
    expect(await renderText([[1, 2], [3]])).toBe('');
  });
});
