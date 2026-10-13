import {
  ACCESSORY_ART,
  BADGE_EMOJI,
  COSMETICS,
  CosmeticKind,
  RING_SKIN_KEYS,
  SENIORITY_STAGES,
  THEME_SWATCHES,
  nameColorClass,
  ringSkinLook,
  seniorityStage,
  themeAttr,
} from './shop-catalog';
import { es } from '../i18n/translations/es';

describe('shop catalog (rama 42)', () => {
  const ofKind = (kind: CosmeticKind) => COSMETICS.filter((c) => c.kind === kind).map((c) => c.key);

  it('has unique keys, each with a Spanish name', () => {
    const keys = COSMETICS.map((c) => c.key);
    expect(new Set(keys).size).toBe(keys.length);
    for (const key of keys) expect((es.shop.items as Record<string, string>)[key]).toBeTruthy();
  });

  it('has art for every ring skin, accessory, badge and theme', () => {
    expect(RING_SKIN_KEYS.sort()).toEqual(ofKind(CosmeticKind.RingSkin).sort());
    expect(Object.keys(ACCESSORY_ART).sort()).toEqual(ofKind(CosmeticKind.Accessory).sort());
    expect(Object.keys(BADGE_EMOJI).sort()).toEqual(ofKind(CosmeticKind.Badge).sort());
    expect(Object.keys(THEME_SWATCHES).sort()).toEqual(ofKind(CosmeticKind.Theme).sort());
    for (const key of ofKind(CosmeticKind.NameColor)) expect(nameColorClass(key)).toMatch(/^vp-name--/);
  });

  it('keeps a ring skin humble on the first tier and grows it with the streak', () => {
    for (const skin of RING_SKIN_KEYS) {
      const wood = ringSkinLook(skin, 'wood')!;
      const amethyst = ringSkinLook(skin, 'amethyst')!;
      expect(wood.aura).toBeUndefined();
      expect(wood.sheen).toBeUndefined();
      expect(amethyst.aura).toBeTruthy();
      expect(amethyst.rays).toBe(true);
    }
    expect(ringSkinLook('ring-unknown', 'gold')).toBeNull();
  });

  it('picks the seniority stage by days in the group', () => {
    const daysAgo = (days: number) => new Date(Date.now() - days * 86_400_000).toISOString();
    expect(seniorityStage(daysAgo(10))).toBeNull();
    expect(seniorityStage(daysAgo(40))).toBe(SENIORITY_STAGES[0]);
    expect(seniorityStage(daysAgo(400))).toBe(SENIORITY_STAGES[2]);
    expect(seniorityStage(daysAgo(800))).toBe(SENIORITY_STAGES[3]);
    expect(seniorityStage(null)).toBeNull();
  });

  it('maps a theme key to its data-vp-theme value', () => {
    expect(themeAttr('theme-ocean')).toBe('ocean');
    expect(themeAttr(null)).toBeNull();
    expect(themeAttr('ring-neon')).toBeNull();
  });
});
