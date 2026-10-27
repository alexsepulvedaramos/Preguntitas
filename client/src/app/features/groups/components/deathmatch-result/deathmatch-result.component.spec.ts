import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';

import { DeathmatchResultComponent } from './deathmatch-result.component';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { QuestionType } from '../../../../core/enums/question-type.enum';
import { OptionResult, QuestionResult } from '../../../../core/models/result.model';
import { User } from '../../../../core/models/user.model';

const person = (id: number, username: string) => ({ id, username }) as User;
const team = (id: number, members: User[], voters: User[], percentage: number): OptionResult => ({
  id,
  displayText: members.map((m) => m.username).join(' y '),
  teamMembers: members,
  voteCount: voters.length,
  voters,
  percentage,
});
const [ana, bea, carlos, dani, eva, fran] = ['Ana', 'Bea', 'Carlos', 'Dani', 'Eva', 'Fran'].map((n, i) => person(i + 1, n));

const result = (results: OptionResult[]) =>
  ({ id: 1, text: '¿Quién gana?', type: QuestionType.Deathmatch, results, totalVotes: 0, freeTextResponses: [] }) as unknown as QuestionResult;

// 1.32.2: the Deathmatch result shows who voted each team again, and opens the votes summary.
describe('DeathmatchResultComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DeathmatchResultComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    await TestBed.inject(I18nService).use('es', false);
  });

  async function render(results: OptionResult[]): Promise<HTMLElement> {
    const fixture = TestBed.createComponent(DeathmatchResultComponent);
    fixture.componentRef.setInput('result', result(results));
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  it('shows who voted each side of a face-to-face result, and makes it tappable', async () => {
    const el = await render([team(1, [ana], [carlos, dani, eva, fran, bea], 83), team(2, [bea], [ana], 17)]);
    const stacks = el.querySelectorAll('app-voter-stack');
    expect(stacks.length).toBe(2);
    // Five voters: three faces and "+2".
    expect(stacks[0].querySelectorAll('app-user-avatar').length).toBe(3);
    expect(stacks[0].textContent).toContain('+2');
    expect(stacks[1].querySelectorAll('app-user-avatar').length).toBe(1);
    expect(el.querySelector('button[hlmDialogTrigger]')).not.toBeNull();
  });

  it('shows the voters in each ranked row', async () => {
    const el = await render([
      team(1, [ana], [bea, carlos], 50),
      team(2, [bea], [ana], 25),
      team(3, [carlos], [dani], 25),
      team(4, [dani], [], 0),
    ]);
    expect(el.querySelectorAll('li app-voter-stack').length).toBe(3);
    expect(el.querySelectorAll('li')[3].textContent).toContain('0 votos');
  });

  it('keeps anonymous votes faceless and not tappable', async () => {
    const anonymous = (o: OptionResult) => ({ ...o, voters: [] });
    const el = await render([anonymous(team(1, [ana], [bea, carlos], 67)), anonymous(team(2, [bea], [ana], 33))]);
    expect(el.querySelector('app-voter-stack')).toBeNull();
    expect(el.querySelector('button[hlmDialogTrigger]')).toBeNull();
    expect(el.textContent).toContain('2 votos');
  });
});
