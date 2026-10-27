import { FINISH_KEYS, finishEvolves, iconList, renderFinish } from './ring-finishes';
import { renderStreakFrame } from './streak-frame-renderer';

describe('ring finishes (rama 57)', () => {
  const blur = 'url(#b)';

  it('renders every finish on every level, full and light, with and without the crown', () => {
    for (const key of FINISH_KEYS)
      for (let level = 0; level <= 7; level++)
        for (const lite of [false, true])
          for (const crown of [false, true]) {
            const fx = renderFinish(key, level, lite, crown, blur);
            expect(fx.front + fx.layers).not.toContain('undefined');
            expect(fx.front + fx.layers).not.toContain('NaN');
          }
  });

  it('grows with the tier, except the rare finishes, which look the same on every tier', () => {
    for (const key of FINISH_KEYS) {
      const markup = (level: number) => renderFinish(key, level, false, false, blur);
      const size = (level: number) => markup(level).front.length + markup(level).layers.length;
      if (finishEvolves(key)) expect(size(7)).toBeGreaterThan(size(0));
      else expect(markup(7)).toEqual(markup(0));
    }
  });

  it('brings the game icons one by one from bronze, the question mark last and in the middle', () => {
    expect(iconList(2, false)).toEqual([]);
    expect(iconList(3, false).map((i) => i.kind)).toEqual(['flame']);
    const amethyst = iconList(7, false);
    expect(amethyst.map((i) => i.kind)).toEqual(['heart', 'flame', 'question', 'coin', 'bolt']);
    expect(amethyst.find((i) => i.kind === 'question')!.ang).toBe(0);
    expect(amethyst.some((i) => (i.kind as string) === 'crown')).toBe(false);
  });

  it('keeps the icons symmetric and clear of the crown', () => {
    for (let level = 0; level <= 7; level++) {
      const angles = iconList(level, true).map((i) => i.ang);
      expect(angles.length % 2).toBe(0);
      expect(angles.reduce((a, b) => a + b, 0)).toBe(0);
      for (const a of angles) expect(Math.abs(a)).toBeGreaterThanOrEqual(34);
      expect(iconList(level, true).some((i) => i.kind === 'question')).toBe(false);
    }
  });

  it('draws one crown: the finish’s own instead of the plain gold one', () => {
    const plain = renderStreakFrame({ tier: 'gold', crown: true, lite: false, ribbon: null, uid: 'a' });
    expect(plain).toContain('url(#acr)');
    const styled = renderStreakFrame({ tier: 'gold', finish: 'ring-flare', crown: true, lite: false, ribbon: null, uid: 'a' });
    expect(styled).not.toContain('url(#acr)');
    expect(styled).toContain('vp-frame__fx');
    const rare = renderStreakFrame({ tier: 'gold', finish: 'ring-volt', crown: true, lite: false, ribbon: null, uid: 'a' });
    expect(rare).toContain('url(#acr)');
  });

  it('keeps the finish’s crown where only the tier ring is drawn, except for the rare finishes', () => {
    const ringOnly = renderStreakFrame({ tier: 'gold', crownFinish: 'ring-witch', crown: true, lite: true, ribbon: null, uid: 'a' });
    expect(ringOnly).not.toContain('url(#acr)');
    expect(ringOnly).toContain('vp-frame__fx');
    const rare = renderStreakFrame({ tier: 'gold', crownFinish: 'ring-volt', crown: true, lite: true, ribbon: null, uid: 'a' });
    expect(rare).toContain('url(#acr)');
    expect(rare).not.toContain('vp-frame__fx');
  });

  it('puts a finish on a neutral ring before the first tier', () => {
    expect(renderStreakFrame({ tier: null, crown: false, lite: true, ribbon: null, uid: 'a' })).not.toContain('stroke-width="5"');
    const early = renderStreakFrame({ tier: null, finish: 'ring-orbit', crown: false, lite: true, ribbon: null, uid: 'a' });
    expect(early).toContain('stroke-width="5"');
    expect(early).toContain('vp-frame__fx');
  });
});
