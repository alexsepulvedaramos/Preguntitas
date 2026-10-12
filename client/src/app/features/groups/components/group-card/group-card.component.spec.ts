import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { GroupCardComponent } from './group-card.component';
import { GroupResponse } from '../../models/group.models';
import { I18nService } from '../../../../core/i18n/i18n.service';

const group: GroupResponse = {
  id: 1,
  name: 'Los de siempre',
  description: '',
  invitationCode: 'ABC123',
  creatorUsername: 'ana',
  dailyQuestionTime: '21:00:00',
};

// The status line under the group name on the group list.
describe('GroupCardComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GroupCardComponent],
      providers: [provideRouter([])],
    }).compileComponents();
    // The copy comes from the i18n dictionary (rama 23): assert against the Spanish one.
    await TestBed.inject(I18nService).use('es', false);
  });

  async function renderText(overrides: Partial<GroupResponse>): Promise<string> {
    const fixture = TestBed.createComponent(GroupCardComponent);
    fixture.componentRef.setInput('group', { ...group, ...overrides });
    await fixture.whenStable();
    return (fixture.nativeElement as HTMLElement).textContent!.replace(/\s+/g, ' ');
  }

  it('asks to vote and says which streak is at stake', async () => {
    const text = await renderText({ dailyStatus: 'voting', myStreak: 5 });

    expect(text).toContain('Vota ahora');
    expect(text).toContain('No pierdas tu racha de 5 días');
  });

  it('uses the singular for a one-day streak', async () => {
    expect(await renderText({ dailyStatus: 'voting', myStreak: 1 })).toContain('No pierdas tu racha de 1 día');
  });

  it('shows the plain vote pill without a streak', async () => {
    const text = await renderText({ dailyStatus: 'voting', myStreak: 0 });

    expect(text).toContain('Vota ahora');
    expect(text).not.toContain('racha');
  });

  it('tells the selector it is their turn', async () => {
    expect(await renderText({ dailyStatus: 'selector' })).toContain('¡Te toca elegir!');
  });

  it('shows when the next question arrives once you have voted', async () => {
    expect(await renderText({ dailyStatus: 'results' })).toContain('Próx. a las 21:00');
  });

  it('explains the start gate before the group has two members', async () => {
    expect(await renderText({ dailyStatus: 'no_question' })).toContain('Invita a alguien para empezar');
  });
});
