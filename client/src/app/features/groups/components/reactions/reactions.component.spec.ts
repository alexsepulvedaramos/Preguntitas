import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { of } from 'rxjs';

import { ReactionsComponent } from './reactions.component';
import { AuthService } from '../../../../core/auth/auth.service';
import { DailyService } from '../../../../core/services/daily.service';
import { Reaction } from '../../../../core/models/reaction.model';

describe('ReactionsComponent', () => {
  const setReaction = vi.fn();

  const create = (authorId: number, reactions: Reaction[]) => {
    TestBed.configureTestingModule({
      imports: [ReactionsComponent],
      providers: [
        { provide: DailyService, useValue: { setReaction } },
        // The id comes from the JWT claims as a string, as in production
        { provide: AuthService, useValue: { currentUser: signal({ id: '1', username: 'ana' }) } },
      ],
    });
    const fixture = TestBed.createComponent(ReactionsComponent);
    fixture.componentRef.setInput('groupId', 7);
    fixture.componentRef.setInput('target', { kind: 'answers', id: 42 });
    fixture.componentRef.setInput('authorId', authorId);
    fixture.componentRef.setInput('reactions', reactions);
    fixture.detectChanges();
    return fixture;
  };

  beforeEach(() => setReaction.mockReset());

  it('groups the counts by emoji and marks yours', () => {
    const fixture = create(2, [
      { emoji: 'fire', userId: 3, username: 'carlos' },
      { emoji: 'fire', userId: 1, username: 'ana' },
      { emoji: 'laugh', userId: 4, username: 'dani' },
    ]);
    const groups = (fixture.componentInstance as any).groups();
    expect(groups).toEqual([
      { key: 'laugh', emoji: '😂', count: 1, mine: false },
      { key: 'fire', emoji: '🔥', count: 2, mine: true },
    ]);
  });

  it('another emoji replaces yours; your own one removes it', () => {
    const fixture = create(2, [{ emoji: 'fire', userId: 1, username: 'ana' }]);
    const component = fixture.componentInstance as any;

    setReaction.mockReturnValue(of([{ emoji: 'upside', userId: 1, username: 'ana' }]));
    component.pick('upside');
    expect(setReaction).toHaveBeenLastCalledWith(7, { kind: 'answers', id: 42 }, 'upside');
    expect(component.mine()).toBe('upside');

    setReaction.mockReturnValue(of([]));
    component.pick('upside');
    expect(setReaction).toHaveBeenLastCalledWith(7, { kind: 'answers', id: 42 }, null);
    expect(component.mine()).toBeNull();
  });

  it('shows nothing on your own answer until someone reacts', () => {
    const fixture = create(1, []);
    expect(fixture.nativeElement.querySelector('button')).toBeNull();

    fixture.componentRef.setInput('reactions', [{ emoji: 'love', userId: 2, username: 'bea' }]);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('button')).not.toBeNull();
    expect((fixture.componentInstance as any).canReact()).toBe(false);
  });
});
