import { Component, computed, inject, input } from '@angular/core';

import { AuthService } from '../../../../core/auth/auth.service';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { GlyphComponent } from '../../../../shared/components/glyph/glyph.component';
import { GroupMember } from '../../models/group.models';

// The two sides of a 1-vs-1 Deathmatch, or null for any other team set-up (§17.15 item 6).
export function duelSides(teams: number[][] | null | undefined): [number, number] | null {
  if (!teams || teams.length !== 2 || teams[0].length !== 1 || teams[1].length !== 1) return null;
  const [first, second] = [teams[0][0], teams[1][0]];
  return first === second ? null : [first, second];
}

// Free duels (§17.15 item 6, rama 47): shown before sending a 1-vs-1 Deathmatch as a flash or
// as the selector's daily pick, so nobody starts a duel — and a push to the two sides — by
// accident. Renders nothing for any other team set-up.
@Component({
  selector: 'app-duel-notice',
  imports: [GlyphComponent],
  template: `
    @if (text(); as text) {
      <p
        class="flex items-start gap-2 rounded-default border border-border bg-secondary/60 px-3 py-2 text-sm text-foreground"
        aria-live="polite"
      >
        <app-glyph name="versus" size="xs" class="mt-0.5 shrink-0" />
        <span>{{ text }}</span>
      </p>
    }
  `,
})
export class DuelNoticeComponent {
  private readonly authService = inject(AuthService);
  private readonly t = inject(I18nService).t;

  readonly teams = input<number[][] | null | undefined>(null);
  readonly members = input.required<GroupMember[]>();

  protected readonly text = computed(() => {
    const sides = duelSides(this.teams());
    if (!sides) return null;
    const me = Number(this.authService.currentUser()?.id);
    const name = (id: number) => this.members().find((m) => m.id === id)?.username ?? '';
    if (sides.includes(me)) {
      const opponent = sides[0] === me ? sides[1] : sides[0];
      return this.t('duel.challenge', { name: name(opponent) });
    }
    return this.t('duel.between', { first: name(sides[0]), second: name(sides[1]) });
  });
}
