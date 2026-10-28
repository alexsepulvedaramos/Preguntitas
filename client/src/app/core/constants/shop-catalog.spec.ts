import { COSMETICS, CosmeticKind, cosmeticByKey, frameColorItemKey, frameColorOf } from './shop-catalog';
import { FRAME_COLOR_KEYS } from './streak-tiers';
import { FINISH_KEYS } from '../../shared/components/streak-frame/ring-finishes';
import { isPlate } from '../../shared/components/name-plate/plate-art';
import { es } from '../i18n/translations/es';

describe('shop catalog (rama 42, rama 57)', () => {
  const ofKind = (kind: CosmeticKind) => COSMETICS.filter((c) => c.kind === kind).map((c) => c.key);

  it('has unique keys, each with a Spanish name', () => {
    const keys = COSMETICS.map((c) => c.key);
    expect(new Set(keys).size).toBe(keys.length);
    for (const key of keys) expect((es.shop.items as Record<string, string>)[key]).toBeTruthy();
  });

  it('has art for every ring finish it sells', () => {
    for (const key of ofKind(CosmeticKind.RingSkin)) expect(FINISH_KEYS as readonly string[]).toContain(key);
  });

  it('sells every frame colour, and maps it back to its key', () => {
    expect(ofKind(CosmeticKind.FrameColor)).toEqual(FRAME_COLOR_KEYS.map(frameColorItemKey));
    expect(frameColorOf('frame-purple')).toBe('purple');
    expect(frameColorOf('frame-gold')).toBeNull();
    expect(frameColorOf('ring-neon')).toBeNull();
  });

  it('marks the Halloween frame and its plate as seasonal', () => {
    expect(COSMETICS.filter((c) => c.seasonal).map((c) => c.key)).toEqual(['ring-witch', 'plate-witch']);
  });

  it('sells a plate for every ring finish and frame colour, at the same price, each with its art (rama 60)', () => {
    const plates = COSMETICS.filter((c) => c.kind === CosmeticKind.Plate);
    const sources = COSMETICS.filter((c) => c.kind === CosmeticKind.RingSkin || c.kind === CosmeticKind.FrameColor);
    expect(plates.length).toBe(sources.length);
    for (const source of sources) {
      const plate = cosmeticByKey(`plate-${source.key.replace(/^(ring|frame)-/, '')}`);
      expect(plate?.price).toBe(source.price);
      expect(plate?.rarity).toBe(source.rarity);
      expect(isPlate(plate?.key)).toBe(true);
    }
  });

  it('no longer sells retired cosmetics, but still names them for the ledger (§17.15)', () => {
    for (const kind of [CosmeticKind.Badge, CosmeticKind.Title, CosmeticKind.Accessory, CosmeticKind.NameColor, CosmeticKind.Theme])
      expect(ofKind(kind)).toEqual([]);
    expect(cosmeticByKey('acc-halo')?.name).toBe('shop.items.acc-halo');
    expect(cosmeticByKey('color-mint')?.name).toBe('shop.items.color-mint');
    expect(cosmeticByKey('theme-ocean')?.name).toBe('shop.items.theme-ocean');
    for (const key of ['color-mint', 'theme-ocean', 'badge-frog']) expect((es.shop.items as Record<string, string>)[key]).toBeTruthy();
  });
});
