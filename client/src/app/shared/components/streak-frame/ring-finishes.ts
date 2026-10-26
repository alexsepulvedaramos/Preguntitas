// Ring finishes (rama 57, spec §17.15 item 8): drawn on top of the streak ring's material, so
// the tier still shows, with effects that grow with the tier. Coordinates are the frame's:
// viewBox -100..100, avatar radius 50, rim 49–59; everything stays within r ≈ 72 except the
// icons and the crown above the head. Every value comes from these constants, never from user
// input, so the markup is safe to trust. Styling and animation live in styles-lime.css
// (.vp-fx-*); only transform and opacity animate.

type Style = Record<string, string | number>;
// SVG attributes by their short names (ATTR_NAMES), plus st: the inline style.
type Attrs = Record<string, string | number | Style | undefined>;

interface Shape {
  tag: 'circle' | 'path' | 'rect';
  attrs: Attrs;
}

interface Group {
  tr?: string;
  cls?: string;
  st?: Style;
  shapes: Shape[];
}

// A band layer drawn behind the ring (aurora, flare, the witch's spell): a conic gradient
// masked to the strip just outside the rim.
interface Layer {
  cls: string;
  st: Style;
}

export interface FinishMarkup {
  // SVG content drawn above the avatar's face.
  front: string;
  // HTML band layers behind the ring.
  layers: string;
  // The finish replaces the tier's aura (its band layers take that place).
  hideAura: boolean;
}

export const FINISH_KEYS = [
  'ring-volt',
  'ring-flare',
  'ring-blizzard',
  'ring-doodle',
  'ring-orbit',
  'ring-aurora',
  'ring-neon',
  'ring-arcade',
  'ring-witch',
] as const;
export type FinishKey = (typeof FINISH_KEYS)[number];

export function isFinish(key: string | null | undefined): key is FinishKey {
  return !!key && (FINISH_KEYS as readonly string[]).includes(key);
}

// ── Small helpers ────────────────────────────────────────────────────────────

const F = (n: number) => Math.round(n * 100) / 100;

function polar(deg: number, r: number): [number, number] {
  const a = (deg * Math.PI) / 180;
  return [F(Math.sin(a) * r), F(-Math.cos(a) * r)];
}

// Deterministic pseudo-random sequence so a finish looks the same on every render.
function seeded(seed: number): () => number {
  let s = seed;
  return () => (s = (s * 9301 + 49297) % 233280) / 233280;
}

// Short names, as in the prototypes: sw stroke-width, op opacity, da/dof dash array/offset,
// cap linecap, f filter, tr transform, cls class, st style, w/h width/height, rule fill-rule.
const ATTR_NAMES: Record<string, string> = {
  sw: 'stroke-width',
  op: 'opacity',
  da: 'stroke-dasharray',
  dof: 'stroke-dashoffset',
  cap: 'stroke-linecap',
  f: 'filter',
  tr: 'transform',
  cls: 'class',
  w: 'width',
  h: 'height',
  rule: 'fill-rule',
};

type ShapeInput = Attrs;

const circ = (o: ShapeInput): Shape => ({ tag: 'circle', attrs: { cx: 0, cy: 0, fill: 'none', ...o } });
const path = (o: ShapeInput): Shape => ({ tag: 'path', attrs: { fill: 'none', ...o } });
const rect = (o: ShapeInput): Shape => ({ tag: 'rect', attrs: o });
const group = (o: Partial<Group> = {}): Group => ({ shapes: [], ...o });

const anim = (d: number, delay: number, extra: Style = {}): Style => ({ '--d': `${F(d)}s`, 'animation-delay': `${F(delay)}s`, ...extra });

function styleText(st: Style | undefined): string {
  if (!st) return '';
  return Object.entries(st)
    .map(([k, v]) => `${k.startsWith('--') ? k : k.replace(/[A-Z]/g, (c) => '-' + c.toLowerCase())}:${v}`)
    .join(';');
}

function shapeText({ tag, attrs }: Shape): string {
  let out = `<${tag}`;
  for (const [k, v] of Object.entries(attrs)) {
    if (v === undefined || typeof v === 'object') continue;
    out += ` ${ATTR_NAMES[k] ?? k}="${v}"`;
  }
  const st = attrs['st'] as Style | undefined;
  if (st) out += ` style="${styleText(st)}"`;
  return out + '/>';
}

function groupText(g: Group): string {
  const attrs = [g.tr && `transform="${g.tr}"`, g.cls && `class="${g.cls}"`, g.st && `style="${styleText(g.st)}"`]
    .filter(Boolean)
    .join(' ');
  return `<g${attrs ? ' ' + attrs : ''}>${g.shapes.map(shapeText).join('')}</g>`;
}

function tri(a: [number, number], b: [number, number], c: [number, number]): string {
  return `M${a[0]} ${a[1]} L${b[0]} ${b[1]} L${c[0]} ${c[1]} Z`;
}

function sparkle(x: number, y: number, s: number): string {
  return `M${x} ${F(y - s)} Q${x} ${y} ${F(x + s)} ${y} Q${x} ${y} ${x} ${F(y + s)} Q${x} ${y} ${F(x - s)} ${y} Q${x} ${y} ${x} ${F(y - s)} Z`;
}

function ellipse(cx: number, cy: number, rx: number, ry: number): string {
  return `M${F(cx - rx)} ${cy} A${rx} ${ry} 0 1 0 ${F(cx + rx)} ${cy} A${rx} ${ry} 0 1 0 ${F(cx - rx)} ${cy} Z`;
}

function heartPath(cx: number, cy: number, s: number): string {
  return (
    `M${cx} ${F(cy + s * 0.9)} C${F(cx - s * 1.6)} ${F(cy - s * 0.1)} ${F(cx - s * 1.1)} ${F(cy - s * 1.5)} ${cx} ${F(cy - s * 0.6)}` +
    ` C${F(cx + s * 1.1)} ${F(cy - s * 1.5)} ${F(cx + s * 1.6)} ${F(cy - s * 0.1)} ${cx} ${F(cy + s * 0.9)} Z`
  );
}

function boltPath(cx: number, cy: number, s: number): string {
  const p = [[0.3, -1.2], [-0.7, 0.15], [0, 0.15], [-0.3, 1.2], [0.7, -0.15], [0, -0.15]];
  return 'M' + p.map((q) => `${F(cx + q[0] * s)} ${F(cy + q[1] * s)}`).join(' L') + ' Z';
}

// ── The game's icons (Neón, Arcade, Garabato) ────────────────────────────────
// The same icons at the same levels in every style: one more per level from bronze, and the
// question mark, the game itself, arrives last, on amethyst, in the middle. The crown is never
// one of them: it belongs to the group's longest streak (drawCrown).

type IconKind = 'flame' | 'coin' | 'bolt' | 'heart' | 'question' | 'crown';
type IconStyle = 'pixel' | 'neon' | 'doodle';

const ICON_ORDER: IconKind[] = ['flame', 'coin', 'bolt', 'heart', 'question'];
const ICON_DISPLAY: IconKind[] = ['heart', 'flame', 'question', 'coin', 'bolt'];
const ICON_COLORS: Record<IconKind, string> = {
  question: '#7dff7a',
  flame: '#ff9a1f',
  heart: '#ff4d6d',
  bolt: '#5fe3ff',
  coin: '#ffd400',
  crown: '#f2c14e',
};
const PIXEL_INKS: Record<string, string> = { o: '#7a5a00', '#': '#c99a00', r: '#ff3b5c' };
const PIXELS: Record<IconKind, string[]> = {
  heart: ['.XX.XX.', 'XXXXXXX', 'XXXXXXX', '.XXXXX.', '..XXX..', '...X...'],
  bolt: ['....XX.', '...XX..', '..XX...', '.XXXXX.', '...XX..', '..XX...', '.XX....'],
  crown: ['X..X..X', 'XX.X.XX', 'XXXrXXX', 'XXXXXXX', '.XXXXX.'],
  question: ['.XXXXX.', 'XX...XX', '....XX.', '...XX..', '...XX..', '.......', '...XX..'],
  flame: ['...X...', '..XX...', '..XXX.X', '.XXXXXX', 'XXX.XXX', 'XX...XX', '.XXXXX.'],
  coin: ['.#####.', '#XoooX#', '#XXXoX#', '#XXoXX#', '#XXXXX#', '#XXoXX#', '.#####.'],
};

const ICON_RADIUS = 79;
// With the crown in the middle the icons start further out, clear of its points.
const ICON_SLOTS_BESIDE_CROWN = [-34, 34, -56, 56];

function questionPath(cx: number, cy: number, s: number): string {
  const Q = (x: number, y: number) => `${F(cx + x * s)} ${F(cy + y * s)}`;
  return `M${Q(-0.75, -0.45)} Q${Q(-0.75, -1.25)} ${Q(0, -1.25)} Q${Q(0.8, -1.25)} ${Q(0.8, -0.5)} Q${Q(0.8, 0)} ${Q(0, 0.2)} L${Q(0, 0.5)} M${Q(0, 1.05)} L${Q(0, 1.1)}`;
}

function iconPath(kind: IconKind, cx: number, cy: number, s: number): string {
  const P = (x: number, y: number) => `${F(cx + x * s)} ${F(cy + y * s)}`;
  switch (kind) {
    case 'heart':
      return heartPath(cx, cy, s * 0.95);
    case 'bolt':
      return boltPath(cx, cy, s * 1.15);
    case 'crown':
      return `M${P(-1.3, 0.85)} L${P(-1.3, -0.65)} L${P(-0.62, 0)} L${P(0, -1)} L${P(0.62, 0)} L${P(1.3, -0.65)} L${P(1.3, 0.85)} Z`;
    case 'question':
      return questionPath(cx, cy, s);
    case 'flame':
      return `M${P(0, 1.15)} Q${P(-1.05, 1.15)} ${P(-0.95, 0.15)} Q${P(-0.85, -0.5)} ${P(-0.3, -1.25)} Q${P(-0.1, -0.45)} ${P(0.3, -0.3)} Q${P(0.2, -0.9)} ${P(0.5, -1.15)} Q${P(1.05, -0.3)} ${P(0.95, 0.3)} Q${P(0.85, 1.15)} ${P(0, 1.15)} Z`;
    case 'coin': {
      const r = F(1.15 * s);
      return `M${P(1.15, 0)} A${r} ${r} 0 1 0 ${P(-1.15, 0)} A${r} ${r} 0 1 0 ${P(1.15, 0)} Z ${questionPath(cx, cy, s * 0.5)}`;
    }
  }
}

export function iconList(level: number, crown: boolean): { kind: IconKind; ang: number; big: boolean }[] {
  let unlocked = ICON_ORDER.slice(0, Math.max(0, level - 2));
  // The crown takes the middle, where the question mark goes, and the icons stay symmetric
  // round it: without the question mark, an odd count drops the newest icon.
  if (crown) {
    unlocked = unlocked.filter((k) => k !== 'question');
    if (unlocked.length % 2) unlocked = unlocked.slice(0, -1);
  }
  const shown = ICON_DISPLAY.filter((k) => unlocked.includes(k));
  const n = shown.length;
  const angles = crown
    ? ICON_SLOTS_BESIDE_CROWN.slice(0, n).sort((a, b) => a - b)
    : shown.map((_, i) => (i - (n - 1) / 2) * 22);
  return shown.map((kind, i) => ({ kind, ang: angles[i], big: kind === 'question' && !crown }));
}

function pixelIcon(g: Group, rows: string[], cx0: number, cy0: number, cell: number, col: string, cls?: string, st?: Style): void {
  const w = rows[0].length;
  const h = rows.length;
  rows.forEach((row, ry) => {
    for (let cx = 0; cx < row.length; cx++) {
      const ch = row[cx];
      if (ch === '.') continue;
      g.shapes.push(
        rect({ x: F(cx0 + (cx - w / 2) * cell), y: F(cy0 - (h / 2) * cell + ry * cell), w: cell, h: cell, fill: PIXEL_INKS[ch] ?? col, stroke: '#111', sw: 0.35, cls, st })
      );
    }
  });
}

function drawIcons(out: Group[], level: number, lite: boolean, style: IconStyle, blur: string, crown: boolean): void {
  if (lite) return;
  iconList(level, crown).forEach((ic, i) => {
    const col = ICON_COLORS[ic.kind];
    const s = ic.big ? 4.6 : 3.8;
    if (style === 'pixel') {
      const g = group({ tr: `rotate(${ic.ang})` });
      pixelIcon(g, PIXELS[ic.kind], 0, -ICON_RADIUS, ic.big ? 2.3 : 1.95, col, 'vp-fx-bob', { 'animation-delay': `${-i * 0.2}s` });
      out.push(g);
    } else if (style === 'neon') {
      const d = iconPath(ic.kind, 0, -ICON_RADIUS, s);
      const g = group({ tr: `rotate(${ic.ang})`, cls: 'vp-fx-flicker', st: { 'animation-delay': `${F(-i * 1.1)}s` } });
      g.shapes.push(path({ d, stroke: col, sw: 4.5, op: 0.55, f: blur, cap: 'round' }));
      g.shapes.push(path({ d, stroke: '#ffffff', sw: 1.5, cap: 'round' }));
      g.shapes.push(path({ d, stroke: col, sw: 0.7, op: 0.9, cap: 'round' }));
      out.push(g);
    } else {
      const d = iconPath(ic.kind, 0, -ICON_RADIUS, s);
      const g = group({ tr: `rotate(${ic.ang})` });
      g.shapes.push(path({ d, fill: col, op: 0.35, cls: 'vp-fx-wiggle', st: anim(1.6 + i * 0.2, -i * 0.3) }));
      g.shapes.push(path({ d, stroke: col, sw: 1.6, cap: 'round', cls: 'vp-fx-wiggle', st: anim(1.6 + i * 0.2, -i * 0.3) }));
      out.push(g);
    }
  });
}

// ── Crowns ───────────────────────────────────────────────────────────────────
// The crown of the group's longest current streak, in every finish's own style but always the
// same size and place as the plain gold one (46 × 36 above the head). On list sizes it sits
// tilted on the top-right corner, as everywhere in the app.

export const CROWN_PATH = 'M-19 -60 L-23 -83 L-11 -73 L0 -91 L11 -73 L23 -83 L19 -60 Q0 -55 -19 -60 Z';
const CROWN_LITE = 'translate(40 -44) rotate(30) scale(1.25) translate(0 73)';
const CROWN_TIPS: [number, number, number][] = [[0, -91, 2.4], [-23, -83, 2], [23, -83, 2]];

interface CrownLook {
  fill: string;
  stroke: string;
  glow?: string;
  gem: string;
  sides: string;
  tips: string;
  tipCls?: string;
}

const CROWN_LOOKS: Partial<Record<FinishKey, CrownLook>> = {
  'ring-volt': { fill: '#123a5c', stroke: '#7fe6ff', glow: '#3cc8ff', gem: '#e9fdff', sides: '#7fe6ff', tips: '#ffffff', tipCls: 'vp-fx-candle' },
  'ring-flare': { fill: '#d93a00', stroke: '#ffd23f', glow: '#ff6a00', gem: '#fff3c4', sides: '#ffb300', tips: '#ffd23f', tipCls: 'vp-fx-candle' },
  'ring-blizzard': { fill: '#dff6ff', stroke: '#8fdcff', glow: '#bfefff', gem: '#ffffff', sides: '#a8e6ff', tips: '#ffffff', tipCls: 'vp-fx-twinkle' },
  'ring-orbit': { fill: '#2b2540', stroke: '#c7b8ff', gem: '#ffe7a3', sides: '#a8e6ff', tips: '#ffffff', tipCls: 'vp-fx-twinkle' },
  'ring-aurora': { fill: '#0e3b3a', stroke: '#2affb0', glow: '#2affb0', gem: '#b06bff', sides: '#00d4ff', tips: '#c8ffe9' },
  'ring-witch': { fill: '#3a1f5c', stroke: '#ff8a1f', glow: '#8a2be2', gem: '#ff8a1f', sides: '#39ff6b', tips: '#ffe066' },
};

function drawCrown(out: Group[], key: FinishKey, lite: boolean, blur: string): void {
  const g = group({ tr: lite ? CROWN_LITE : undefined });
  if (key === 'ring-arcade') {
    pixelIcon(g, PIXELS.crown, 0, -73, 6.6, ICON_COLORS.crown);
  } else if (key === 'ring-neon') {
    const d = iconPath('crown', 0, -73, 17.6);
    g.shapes.push(path({ d, stroke: '#ffc83d', sw: 6, op: 0.6, f: blur }));
    g.shapes.push(path({ d, stroke: '#fff7c2', sw: 2.4 }));
    g.shapes.push(path({ d, stroke: '#ffc83d', sw: 1, op: 0.9 }));
  } else if (key === 'ring-doodle') {
    const d = iconPath('crown', 0, -73, 17.6);
    g.shapes.push(path({ d, fill: '#f2c14e', op: 0.45, cls: 'vp-fx-wiggle', st: anim(1.8, 0) }));
    g.shapes.push(path({ d, stroke: '#f2c14e', sw: 2.2, cap: 'round', cls: 'vp-fx-wiggle', st: anim(1.8, 0) }));
  } else {
    const look = CROWN_LOOKS[key]!;
    if (look.glow && !lite) g.shapes.push(path({ d: CROWN_PATH, stroke: look.glow, sw: 5, op: 0.55, f: blur, cls: 'vp-fx-pulse' }));
    g.shapes.push(path({ d: CROWN_PATH, fill: look.fill, stroke: look.stroke, sw: 1.4 }));
    g.shapes.push(path({ d: 'M-19 -62 Q0 -57 19 -62', stroke: look.stroke, sw: 1 }));
    if (key === 'ring-witch') {
      // A little pumpkin for the jewel.
      g.shapes.push(path({ d: ellipse(0, -71, 4.6, 3.8), fill: look.gem, stroke: '#7a3200', sw: 0.6 }));
      g.shapes.push(path({ d: 'M-0.6 -74.6 L0.6 -74.6 L0.9 -77 L-0.4 -77 Z', fill: '#4caf3a' }));
    } else {
      g.shapes.push(circ({ cx: 0, cy: -71, r: 4, fill: look.gem, stroke: look.stroke, sw: 0.5 }));
    }
    g.shapes.push(circ({ cx: -12, cy: -65, r: 2.4, fill: look.sides }));
    g.shapes.push(circ({ cx: 12, cy: -65, r: 2.4, fill: look.sides }));
    CROWN_TIPS.forEach(([x, y, r], i) => {
      if (look.tipCls === 'vp-fx-twinkle') g.shapes.push(path({ d: sparkle(x, y, r * 1.6), fill: look.tips, cls: 'vp-fx-twinkle', st: anim(2 + i * 0.3, -i * 0.5) }));
      else g.shapes.push(circ({ cx: x, cy: y, r, fill: look.tips, cls: look.tipCls, st: look.tipCls ? { 'animation-delay': `${-i * 0.4}s` } : undefined }));
    });
  }
  out.push(g);
}

// ── Neón: tube → inner tube → running light → second light → colour cycle → bulbs → third
// tube, plus the game's icons as neon signs ──

function neon(out: Group[], lvl: number, lite: boolean, blur: string, crown: boolean): void {
  const hue = lvl >= 5 ? 'vp-fx-hue' : undefined;
  const hue2 = lvl >= 5 ? 'vp-fx-hue2' : undefined;
  const tube = group({ cls: 'vp-fx-flicker' });
  if (!lite) tube.shapes.push(circ({ r: 62, stroke: '#ff2bd6', sw: 6 + lvl * 0.5, op: F(0.45 + lvl * 0.04), f: blur, cls: hue }));
  else tube.shapes.push(circ({ r: 62, stroke: '#ff2bd6', sw: 5, op: 0.35, cls: hue }));
  tube.shapes.push(circ({ r: 62, stroke: '#ff7ae8', sw: lite ? 3 : 2.2, cls: hue }));
  tube.shapes.push(circ({ r: 62, stroke: '#ffe3fb', sw: 0.8 }));
  if (lvl >= 2) {
    if (!lite) tube.shapes.push(circ({ r: 47, stroke: '#00e5ff', sw: F(5 + lvl * 0.4), op: 0.5, f: blur, cls: hue2 }));
    tube.shapes.push(circ({ r: 47, stroke: '#7ff3ff', sw: lite ? 2.4 : 1.8, cls: hue2 }));
  }
  if (lvl >= 7) {
    if (!lite) tube.shapes.push(circ({ r: 66, stroke: '#a24bff', sw: 4, op: 0.5, f: blur }));
    tube.shapes.push(circ({ r: 66, stroke: '#d6b3ff', sw: 1.2 }));
  }
  out.push(tube);
  if (lvl >= 3) {
    const dashes = lvl >= 7 ? '22 172.8' : '22 367.6';
    const run = group({ cls: 'vp-fx-spin', st: { '--d': `${lvl >= 7 ? 2.2 : lvl >= 5 ? 2.8 : 3.6}s` } });
    if (!lite) run.shapes.push(circ({ r: 62, stroke: '#ffffff', sw: 6, op: 0.7, da: dashes, cap: 'round', f: blur }));
    run.shapes.push(circ({ r: 62, stroke: '#ffffff', sw: lite ? 3.4 : 2.6, da: dashes, cap: 'round' }));
    out.push(run);
  }
  if (lvl >= 4) {
    const run2 = group({ cls: 'vp-fx-spin-rev', st: { '--d': '4.6s' } });
    run2.shapes.push(circ({ r: 47, stroke: '#ffffff', sw: 2.2, da: '16 279.3', cap: 'round' }));
    out.push(run2);
  }
  // On list sizes, where the icons can't be told apart, marquee bulbs take their place.
  if (lvl >= 6 && lite) {
    const bulbs = group();
    for (let k = 0; k < 10; k++) {
      const b = polar((k / 10) * 360 + 10, 70);
      bulbs.shapes.push(circ({ cx: b[0], cy: b[1], r: 2.6, fill: k % 2 ? '#fff3a6' : '#ffffff', cls: 'vp-fx-chase', st: { 'animation-delay': `${F(-(k % 3) * 0.3)}s` } }));
    }
    out.push(bulbs);
  }
  drawIcons(out, lvl, lite, 'neon', blur, crown);
}

// ── Arcade: more blocks and colours per level, inner dots, marquee, plus the game's icons in
// pixels ──

const ARCADE_PALETTES = [
  ['#3ae374', '#17c0eb'],
  ['#3ae374', '#17c0eb'],
  ['#3ae374', '#17c0eb', '#fff200'],
  ['#3ae374', '#17c0eb', '#fff200'],
  ['#3ae374', '#17c0eb', '#fff200', '#ff4d4d'],
  ['#3ae374', '#17c0eb', '#fff200', '#ff4d4d'],
  ['#3ae374', '#17c0eb', '#fff200', '#ff9f1a', '#ff4d4d', '#c56cf0'],
  ['#3ae374', '#17c0eb', '#fff200', '#ff9f1a', '#ff4d4d', '#c56cf0'],
];

function arcade(out: Group[], lvl: number, lite: boolean, blur: string, crown: boolean): void {
  const pal = ARCADE_PALETTES[lvl];
  const n = lite ? [8, 10, 12, 14, 14, 16, 16, 18][lvl] : [12, 16, 20, 24, 24, 24, 24, 24][lvl];
  const size = lite ? 7.4 : 5.6;
  const cls = lvl >= 3 ? 'vp-fx-chase' : 'vp-fx-blink';
  const blocks = group();
  for (let k = 0; k < n; k++) {
    const ang = (k / n) * 360;
    const c = polar(ang, 62.5);
    blocks.shapes.push(
      rect({ x: F(c[0] - size / 2), y: F(c[1] - size / 2), w: size, h: size, fill: pal[k % pal.length], stroke: '#111', sw: 0.8, tr: `rotate(${Math.round(ang)} ${c[0]} ${c[1]})`, cls, st: { 'animation-delay': `${F(-(k % 3) * 0.3)}s` } })
    );
  }
  if (!lite && lvl >= 2)
    for (let i = 0; i < 36; i++) {
      const a = (i / 36) * 360;
      const d = polar(a, 46.6);
      blocks.shapes.push(rect({ x: F(d[0] - 1.1), y: F(d[1] - 1.1), w: 2.2, h: 2.2, fill: i % 2 ? '#111' : pal[i % pal.length], op: 0.85, tr: `rotate(${Math.round(a)} ${d[0]} ${d[1]})` }));
    }
  out.push(blocks);
  drawIcons(out, lvl, lite, 'pixel', blur, crown);
}

// ── Garabato: a hand-drawn marker ring, plus the game's icons as doodles. It "boils" like a
// cartoon only on the large sizes; on list sizes the lines stay still so it doesn't flicker ──

function wobble(r: number, amp: number, rnd: () => number, n: number): string {
  let d = '';
  for (let k = 0; k <= n; k++) {
    const p = polar((k / n) * 360 + (k === n ? 4 : 0), r + (rnd() - 0.5) * amp);
    d += (k ? ' L' : 'M') + p.join(' ');
  }
  return d;
}

function doodle(out: Group[], lvl: number, lite: boolean, blur: string, rnd: () => number, crown: boolean): void {
  const cols = ['#fff3b0', '#ff8fc7', '#7fe9ff'];
  const lines = lvl >= 4 ? 3 : lvl >= 2 ? 2 : 1;
  for (let l = 0; l < lines; l++) {
    for (let f = 0; f < (lite ? 1 : 3); f++) {
      const g = group(lite ? {} : { cls: 'vp-fx-boil', st: { 'animation-delay': `${F(-f * 0.3)}s` } });
      g.shapes.push(path({ d: wobble(61.5 + l * (lite ? 3.4 : 2.6), lite ? 1.2 : 2.4, rnd, 36), stroke: cols[l], sw: lite ? 2.6 : 1.5, cap: 'round' }));
      out.push(g);
    }
  }
  if (lvl >= 3 && !lite) {
    const hatch = group({ cls: 'vp-fx-boil', st: { 'animation-delay': '-0.15s' } });
    for (let h = 0; h < 18; h++) {
      const a = (h / 18) * 360 + rnd() * 6;
      const p0 = polar(a, 66.5);
      const p1 = polar(a + 3, 70);
      hatch.shapes.push(path({ d: `M${p0.join(' ')} L${p1.join(' ')}`, stroke: '#fff3b0', sw: 1.1, cap: 'round', op: 0.85 }));
    }
    out.push(hatch);
  }
  drawIcons(out, lvl, lite, 'doodle', blur, crown);
}

// ── Rayos: bolts that jump off the ring (so they read at list sizes) → glow → forks → bolts out
// of the ring → orbiting balls → storm ──

function volt(out: Group[], lvl: number, lite: boolean, blur: string, rnd: () => number): void {
  if (lvl >= 1) {
    const tint = group({ cls: 'vp-fx-pulse' });
    tint.shapes.push(circ({ r: 59.6, stroke: '#7fe6ff', sw: lite ? 1.4 : 0.9, op: 0.8 }));
    tint.shapes.push(circ({ r: 48.6, stroke: '#7fe6ff', sw: 0.7, op: 0.6 }));
    if (lvl >= 7 && !lite) tint.shapes.push(circ({ r: 54, stroke: '#3cc8ff', sw: 16, op: 0.35, f: blur }));
    out.push(tint);
  }
  const count = lite ? [1, 1, 2, 2, 3, 3, 4, 4][lvl] : [1, 2, 2, 3, 3, 4, 5, 6][lvl];
  const speed = (lite ? 2.6 : 3.4) - lvl * 0.2;
  const jag = (a0: number, span: number, r0: number, r1: number, amp: number) => {
    const pts: [number, number][] = [];
    const steps = Math.max(3, Math.round(span / 6));
    for (let s = 0; s <= steps; s++) {
      let r = r0 + ((r1 - r0) * s) / steps;
      if (s && s < steps) r += (s % 2 ? 1 : -1) * (amp * 0.5 + rnd() * amp);
      pts.push(polar(a0 + (span * s) / steps, r));
    }
    return pts;
  };
  const line = (pts: [number, number][]) => 'M' + pts.map((p) => p.join(' ')).join(' L');
  for (let z = 0; z < count; z++) {
    const a0 = (z / count) * 360 + rnd() * 40;
    const span = 40 + rnd() * 35;
    const pts = jag(a0, span, 55, 55, lite ? 8 : 6.5);
    const bolt = group({ cls: lite ? 'vp-fx-zap-long' : 'vp-fx-zap', st: anim(speed + rnd(), -rnd() * 3) });
    if (lite) bolt.shapes.push(path({ d: line(pts), stroke: '#3cc8ff', sw: 5, op: 0.55 }));
    else if (lvl >= 3) bolt.shapes.push(path({ d: line(pts), stroke: '#3cc8ff', sw: 4.5, op: 0.8, f: blur }));
    bolt.shapes.push(path({ d: line(pts), stroke: '#e9fdff', sw: lite ? 2.4 : 1.3, cap: 'round' }));
    if (lvl >= 4 && !lite) {
      const m = pts[Math.floor(pts.length / 2)];
      bolt.shapes.push(path({ d: line([m, polar(a0 + span / 2 + 6, 62), polar(a0 + span / 2 + 2, 67)]), stroke: '#e9fdff', sw: 0.9, cap: 'round' }));
    }
    out.push(bolt);
  }
  if (lvl >= 5) {
    for (let o = 0; o < (lvl >= 7 ? 3 : 2); o++) {
      const pts = jag(60 + o * 120 + rnd() * 30, 6, 58, 72, 3);
      const outward = group({ cls: lite ? 'vp-fx-zap-long' : 'vp-fx-zap', st: anim(2.6 + rnd(), -rnd() * 2.6) });
      if (!lite) outward.shapes.push(path({ d: line(pts), stroke: '#3cc8ff', sw: 4, op: 0.7, f: blur }));
      outward.shapes.push(path({ d: line(pts), stroke: '#ffffff', sw: lite ? 2.2 : 1.1, cap: 'round' }));
      out.push(outward);
    }
  }
  if (lvl >= 6) {
    const balls = group({ cls: 'vp-fx-spin', st: { '--d': '2.6s' } });
    for (const a of [0, 180]) {
      const p = polar(a, 54);
      if (!lite) balls.shapes.push(circ({ cx: p[0], cy: p[1], r: 5, fill: '#7fe6ff', op: 0.8, f: blur }));
      balls.shapes.push(circ({ cx: p[0], cy: p[1], r: lite ? 3 : 2, fill: '#ffffff' }));
    }
    out.push(balls);
  }
}

// ── Órbita: moon → trail → second moon → stars → ringed planet → third moon → galaxy dust.
// Moons and trail grow on list sizes too, so the level reads there ──

function orbit(out: Group[], lvl: number, lite: boolean, blur: string, rnd: () => number): void {
  const track = group();
  track.shapes.push(circ({ r: 70, stroke: '#c7b8ff', sw: lite ? 1.1 : 0.7, op: lvl >= 2 ? 0.55 : 0.35, da: '1.5 3' }));
  out.push(track);
  if (lvl >= 7 && !lite) {
    const dust = group({ cls: 'vp-fx-spin-rev', st: { '--d': '40s' } });
    for (let k = 0; k < 30; k++) {
      const p = polar((k / 30) * 360 + rnd() * 6, 74 + rnd() * 3);
      dust.shapes.push(circ({ cx: p[0], cy: p[1], r: F(0.4 + rnd() * 0.6), fill: rnd() > 0.5 ? '#ffffff' : '#c7b8ff', op: F(0.35 + rnd() * 0.6) }));
    }
    out.push(dust);
  }
  const moons = lvl >= 6 ? 3 : lvl >= 3 ? 2 : 1;
  const cols = ['#ffe7a3', '#a8e6ff', '#ffb3d9'];
  const C = 2 * Math.PI * 70;
  for (let mo = 0; mo < moons; mo++) {
    const orb = group({ cls: 'vp-fx-spin', st: { '--d': `${[7, 10.5, 14][mo]}s`, 'animation-delay': `${F(-mo * 3.1)}s` } });
    const trail = lvl < 2 ? 0 : 40 + lvl * 10;
    if (trail) orb.shapes.push(circ({ r: 70, stroke: cols[mo], sw: lite ? 3.4 : 2.2, op: 0.5, da: `${F(trail)} ${F(C)}`, dof: F(-(0.75 * C - trail)), cap: 'round' }));
    const size = (lite ? 5.4 + lvl * 0.2 : 3.6 + lvl * 0.25) * (mo ? 0.8 : 1);
    if (!lite) orb.shapes.push(circ({ cy: -70, r: F(size * 1.8), fill: cols[mo], op: 0.35, f: blur }));
    orb.shapes.push(circ({ cy: -70, r: F(size), fill: cols[mo], stroke: '#2b2540', sw: 0.6 }));
    if (!lite) {
      orb.shapes.push(circ({ cx: -1.4, cy: -71.2, r: F(size * 0.24), fill: '#000', op: 0.18 }));
      orb.shapes.push(circ({ cx: 1.6, cy: -68.8, r: F(size * 0.15), fill: '#000', op: 0.18 }));
    }
    if (mo === 0 && lvl >= 5) {
      const rx = F(size * 2.1);
      const ry = F(size * 0.6);
      orb.shapes.push(path({ d: `M${-rx} -70 a${rx} ${ry} 0 1 0 ${F(2 * rx)} 0 a${rx} ${ry} 0 1 0 ${F(-2 * rx)} 0`, stroke: '#fff3c4', sw: lite ? 1.4 : 0.9, op: 0.9 }));
    }
    out.push(orb);
  }
  if (lvl >= 4 && !lite) {
    const stars = group();
    for (let s = 0; s < lvl + 1; s++) {
      const p = polar(rnd() * 360, 73 + rnd() * 4);
      stars.shapes.push(path({ d: sparkle(p[0], p[1], F(2 + rnd() * 1.6)), fill: '#ffffff', cls: 'vp-fx-twinkle', st: anim(2 + rnd() * 1.6, -rnd() * 3) }));
    }
    out.push(stars);
  }
}

// ── Band layers: aurora, flare and the witch's spell share one footprint, a strip from the rim
// out to r ≈ 70; the level adds colour and brightness, not width ──

const BAND_MASK = 'radial-gradient(circle closest-side, transparent 56%, #000 60%, #000 65%, transparent 71%)';

function band(background: string, lite: boolean, extra: Style): Style {
  return { background, '-webkit-mask': BAND_MASK, mask: BAND_MASK, filter: `blur(${lite ? 1 : 2.5}px)`, ...extra };
}

function auroraLayers(lvl: number, lite: boolean): Layer[] {
  const a = band(
    'conic-gradient(from 0deg, transparent 0deg, #2affb0 30deg, #00d4ff 70deg, transparent 110deg, #7cff6b 160deg, transparent 200deg, #2affb0 250deg, #00d4ff 290deg, transparent 340deg)',
    lite,
    { opacity: F(Math.min(1, (lite ? 0.75 : 0.55) + lvl * 0.06)), '--d': `${F(26 - lvl * 1.5)}s` }
  );
  const layers: Layer[] = [{ cls: 'vp-fx-band vp-fx-band--a', st: a }];
  if (lvl >= 3) {
    const colours =
      lvl >= 6
        ? 'conic-gradient(from 40deg, transparent 0deg, #b06bff 40deg, #ff6bd5 80deg, transparent 130deg, #6b8bff 190deg, transparent 230deg, #ff6bd5 280deg, #b06bff 320deg, transparent 360deg)'
        : 'conic-gradient(from 40deg, transparent 0deg, #b06bff 50deg, transparent 110deg, #2affb0 190deg, transparent 240deg, #6b8bff 300deg, transparent 360deg)';
    layers.push({ cls: 'vp-fx-band vp-fx-band--b', st: band(colours, lite, { '--d': `${F(34 - lvl * 1.5)}s` }) });
  }
  return layers;
}

function aurora(out: Group[], lvl: number, lite: boolean, rnd: () => number): void {
  if (lvl >= 5 && !lite) {
    const stars = group();
    for (let s = 0; s < lvl + 2; s++) {
      const p = polar(rnd() * 360, 64 + rnd() * 8);
      stars.shapes.push(path({ d: sparkle(p[0], p[1], F(1.6 + rnd() * 1.6)), fill: '#ffffff', cls: 'vp-fx-twinkle', st: anim(2 + rnd() * 2, -rnd() * 3) }));
    }
    out.push(stars);
  }
  if (lvl >= 7 && !lite) {
    const comet = group({ tr: 'rotate(-30)' });
    comet.shapes.push(path({ d: 'M-26 -74 L-8 -74', stroke: '#ffffff', sw: 1.2, cap: 'round', cls: 'vp-fx-comet' }));
    out.push(comet);
  }
}

// ── Llamarada: a band of fire licking round the ring, built like the aurora ──

function flareLayers(lvl: number, lite: boolean): Layer[] {
  const layers: Layer[] = [
    {
      cls: 'vp-fx-band vp-fx-band--a',
      st: band(
        'conic-gradient(from 0deg, transparent 0deg, #ff2a00 25deg, #ff7a1a 60deg, transparent 95deg, #ff4d00 140deg, #ffb300 175deg, transparent 210deg, #ff2a00 250deg, #ff8a1a 290deg, transparent 330deg)',
        lite,
        { opacity: F(Math.min(1, (lite ? 0.8 : 0.6) + lvl * 0.06)), '--d': `${F(14 - lvl * 0.8)}s` }
      ),
    },
  ];
  if (lvl >= 2)
    layers.push({
      cls: 'vp-fx-band vp-fx-band--b',
      st: band(
        'conic-gradient(from 20deg, transparent 0deg, #ffd23f 35deg, #fff3c4 55deg, transparent 90deg, #ffb300 150deg, transparent 190deg, #ffe066 240deg, transparent 280deg, #fff3c4 320deg, transparent 350deg)',
        lite,
        { opacity: F(0.55 + lvl * 0.05), 'animation-duration': `${F(9 - lvl * 0.5)}s, ${F(0.9 - lvl * 0.06)}s` }
      ),
    });
  return layers;
}

function flare(out: Group[], lvl: number, lite: boolean, rnd: () => number): void {
  if (lvl >= 4) {
    const hot = group({ cls: 'vp-fx-pulse' });
    hot.shapes.push(circ({ r: 59.4, stroke: '#fff3c4', sw: lite ? 1.4 : 0.9, op: 0.9 }));
    out.push(hot);
  }
  if (lvl >= 2 && !lite) {
    const sparks = group();
    for (let s = 0; s < 2 + lvl; s++) {
      const a = rnd() * 360;
      const p = polar(a, 64);
      const d = polar(a + (rnd() - 0.5) * 20, 5 + rnd() * 5);
      sparks.shapes.push(circ({ cx: p[0], cy: p[1], r: F(0.6 + rnd() * 0.7), fill: rnd() > 0.4 ? '#ffd23f' : '#fff3d6', cls: 'vp-fx-rise', st: anim(1.2 + rnd() * 1.2, -rnd() * 2.4, { '--dx': `${d[0]}px`, '--dy': `${d[1]}px` }) }));
    }
    out.push(sparks);
  }
  if (lvl >= 6) {
    const swirl = group({ cls: 'vp-fx-spin', st: { '--d': '8s' } });
    for (let e = 0; e < 6; e++) {
      const p = polar(e * 60 + rnd() * 20, 66 + rnd() * 3);
      swirl.shapes.push(circ({ cx: p[0], cy: p[1], r: lite ? 1.6 : 1, fill: '#ffd23f' }));
    }
    out.push(swirl);
  }
}

// ── Ventisca: a swirl of snow in three depths round the avatar, with cold mist ──

function blizzardLayers(lvl: number, lite: boolean): Layer[] {
  if (lvl < 3) return [];
  return [
    {
      cls: 'vp-fx-band vp-fx-band--a',
      st: band(
        'conic-gradient(from 0deg, transparent 0deg, #ffffff 40deg, #bfefff 80deg, transparent 130deg, #e8fbff 190deg, transparent 240deg, #a8e6ff 300deg, transparent 350deg)',
        lite,
        { opacity: F(0.3 + lvl * 0.06), '--d': '18s' }
      ),
    },
  ];
}

function blizzard(out: Group[], lvl: number, lite: boolean, blur: string, rnd: () => number): void {
  const edge = group();
  edge.shapes.push(circ({ r: 59.6, stroke: '#ffffff', sw: 0.6, op: 0.7 }));
  out.push(edge);
  const layer = (n: number, rMin: number, rMax: number, size: number, op: number, dur: number, f?: string) => {
    const g = group({ cls: 'vp-fx-spin', st: { '--d': `${dur}s` } });
    for (let k = 0; k < n; k++) {
      const p = polar((k / n) * 360 + rnd() * (360 / n), rMin + rnd() * (rMax - rMin));
      g.shapes.push(circ({ cx: p[0], cy: p[1], r: F(size * (0.6 + rnd() * 0.8)), fill: '#ffffff', op: F(op * (0.6 + rnd() * 0.4)), f }));
    }
    out.push(g);
  };
  layer(lite ? 8 : 10 + lvl * 3, 60, 71, lite ? 1 : 0.6, 0.6, 30);
  if (lvl >= 2) layer(lite ? 5 : 6 + lvl * 2, 61, 70, lite ? 1.8 : 1.2, 0.9, 16);
  if (lvl >= 4 && !lite) layer(3 + lvl, 63, 70, 2, 0.8, 9, blur);
  if (lvl >= 5 && !lite) {
    const flakes = group({ cls: 'vp-fx-spin', st: { '--d': '16s' } });
    for (let f = 0; f < lvl - 2; f++) {
      const fp = polar((f / (lvl - 2)) * 360 + rnd() * 30, 66 + rnd() * 3);
      const s = 3 + rnd() * 1.2;
      let d = '';
      for (const ax of [0, 60, 120]) {
        const e1 = polar(ax, s);
        const e2 = polar(ax + 180, s);
        d += `M${F(fp[0] + e1[0])} ${F(fp[1] + e1[1])} L${F(fp[0] + e2[0])} ${F(fp[1] + e2[1])} `;
        for (const arm of [ax, ax + 180]) {
          const m = polar(arm, s * 0.6);
          const b1 = polar(arm + 40, s * 0.35);
          const b2 = polar(arm - 40, s * 0.35);
          d += `M${F(fp[0] + m[0] + b1[0])} ${F(fp[1] + m[1] + b1[1])} L${F(fp[0] + m[0])} ${F(fp[1] + m[1])} L${F(fp[0] + m[0] + b2[0])} ${F(fp[1] + m[1] + b2[1])} `;
        }
      }
      flakes.shapes.push(path({ d, stroke: '#ffffff', sw: 0.8, cap: 'round', cls: 'vp-fx-twinkle', st: anim(2.4 + rnd() * 1.6, -rnd() * 3) }));
    }
    out.push(flakes);
  }
}

// ── Noche de brujas (Halloween 2026, seasonal): a witch's hat on top, a purple and green spell
// round the ring, a carved jack-o'-lantern on the corner and big bats flying across ──

function witchLayers(lvl: number, lite: boolean): Layer[] {
  const layers: Layer[] = [
    {
      cls: 'vp-fx-band vp-fx-band--a',
      st: band(
        'conic-gradient(from 0deg, transparent 0deg, #8a2be2 40deg, #c13cff 80deg, transparent 120deg, #6a1fb0 180deg, transparent 220deg, #b04bff 280deg, transparent 330deg)',
        lite,
        { opacity: F(Math.min(1, (lite ? 0.8 : 0.6) + lvl * 0.05)), '--d': '20s' }
      ),
    },
  ];
  if (lvl >= 2)
    layers.push({
      cls: 'vp-fx-band vp-fx-band--b',
      st: band('conic-gradient(from 60deg, transparent 0deg, #39ff6b 30deg, transparent 80deg, #7dff3a 170deg, transparent 220deg, #39ff6b 300deg, transparent 340deg)', lite, { '--d': '26s' }),
    });
  return layers;
}

function batPath(cy: number, s: number): string {
  const p = [[0, 0], [-3, -3.2], [-6, -1.2], [-8.5, -3.4], [-11.5, -0.4], [-8.6, 0.2], [-7.6, 2.4], [-5, 1], [-3, 2.8], [-1.6, 1.4], [0, 2.6], [1.6, 1.4], [3, 2.8], [5, 1], [7.6, 2.4], [8.6, 0.2], [11.5, -0.4], [8.5, -3.4], [6, -1.2], [3, -3.2], [0, 0]];
  const P = (q: number[]) => `${F(q[0] * s)} ${F(cy + q[1] * s)}`;
  let d = 'M' + P(p[0]);
  for (let k = 1; k < p.length; k += 2) d += ` Q${P(p[k])} ${P(p[k + 1])}`;
  return `${d} Z M${P([-1.4, -0.6])} L${P([-1.8, -2.6])} L${P([-0.5, -1])} Z M${P([1.4, -0.6])} L${P([1.8, -2.6])} L${P([0.5, -1])} Z`;
}

function jackOLantern(g: Group, cx: number, cy: number, k: number, lit: boolean): void {
  g.shapes.push(path({ d: ellipse(F(cx - 5.4 * k), cy, F(7 * k), F(9.6 * k)), fill: '#e86a00', stroke: '#7a3200', sw: 0.8 }));
  g.shapes.push(path({ d: ellipse(F(cx + 5.4 * k), cy, F(7 * k), F(9.6 * k)), fill: '#e86a00', stroke: '#7a3200', sw: 0.8 }));
  g.shapes.push(path({ d: ellipse(cx, cy, F(6.6 * k), F(10.4 * k)), fill: '#ff8a1f', stroke: '#7a3200', sw: 0.8 }));
  g.shapes.push(
    path({ d: `M${F(cx - 1.6 * k)} ${F(cy - 9.6 * k)} Q${F(cx - 2.2 * k)} ${F(cy - 14.5 * k)} ${F(cx + 0.6 * k)} ${F(cy - 16 * k)} L${F(cx + 2.8 * k)} ${F(cy - 15 * k)} Q${F(cx + 1.2 * k)} ${F(cy - 12.6 * k)} ${F(cx + 1.8 * k)} ${F(cy - 9.6 * k)} Z`, fill: '#6b4417', stroke: '#3a240a', sw: 0.6 })
  );
  const cls = lit ? 'vp-fx-pulse' : undefined;
  for (const side of [-1, 1])
    g.shapes.push(path({ d: `M${F(cx + side * 1.4 * k)} ${F(cy - 0.6 * k)} L${F(cx + side * 4.6 * k)} ${F(cy - 0.6 * k)} L${F(cx + side * 3 * k)} ${F(cy - 4.4 * k)} Z`, fill: '#ffe066', cls }));
  g.shapes.push(
    path({
      d: `M${F(cx - 6 * k)} ${F(cy + 2.6 * k)} L${F(cx - 4 * k)} ${F(cy + 5 * k)} L${F(cx - 2 * k)} ${F(cy + 3.4 * k)} L${cx} ${F(cy + 5.6 * k)} L${F(cx + 2 * k)} ${F(cy + 3.4 * k)} L${F(cx + 4 * k)} ${F(cy + 5 * k)} L${F(cx + 6 * k)} ${F(cy + 2.6 * k)} Q${cx} ${F(cy + 9 * k)} ${F(cx - 6 * k)} ${F(cy + 2.6 * k)} Z`,
      fill: '#ffe066',
      cls,
    })
  );
}

function witch(out: Group[], lvl: number, lite: boolean, blur: string, crown: boolean): void {
  const jack = group();
  const jc = polar(138, 64);
  jackOLantern(jack, jc[0], jc[1], lite ? 1.35 : 1.05, lvl >= 3);
  out.push(jack);
  // The hat gives way to the crown.
  if (!crown) {
    const hat = group();
    hat.shapes.push(path({ d: ellipse(0, -62, 23, 4.6), fill: '#2a1640', stroke: '#120a1e', sw: 0.8 }));
    hat.shapes.push(path({ d: 'M-13 -63.5 Q-7 -76 -1 -90 Q4 -97 13 -95 Q6 -92 5 -86 Q8 -74 13 -63.5 Z', fill: '#3a1f5c', stroke: '#120a1e', sw: 0.8 }));
    hat.shapes.push(path({ d: 'M-12.3 -66 L12.3 -66 L11.4 -70.5 L-11.4 -70.5 Z', fill: '#ff8a1f' }));
    hat.shapes.push(path({ d: 'M-3 -66 L3 -66 L3 -70.5 L-3 -70.5 Z', stroke: '#ffe066', sw: 1.2 }));
    out.push(hat);
  }
  if (lvl >= 4 && !lite) {
    const sparks = group({ cls: 'vp-fx-spin', st: { '--d': '12s' } });
    for (let s = 0; s < lvl; s++) {
      const p = polar((s / lvl) * 360, 66);
      sparks.shapes.push(path({ d: sparkle(p[0], p[1], 2.2), fill: s % 2 ? '#39ff6b' : '#e6b3ff', cls: 'vp-fx-twinkle', st: anim(1.8 + s * 0.2, -s * 0.4) }));
    }
    out.push(sparks);
  }
  // Big bats crossing in front of the picture, then a pause; the moon, the spider and the ghost
  // on the top levels.
  const bats = [0, 1, 1, 2, 2, 2, 3, 3][lvl];
  for (let b = 0; b < bats; b++) {
    const dur = 9 + b * 1.7;
    const g = group({
      cls: 'vp-fx-batfly',
      st: { '--d': `${F(dur)}s`, 'animation-delay': `${F(-(b * dur) / 3 - 1)}s`, '--y0': `${[-34, 6, -52][b]}px`, '--ym': `${[-50, -16, -70][b]}px`, '--y1': `${[-28, 14, -44][b]}px` },
    });
    const s = lite ? 1.7 : 1.35;
    g.shapes.push(path({ d: batPath(0, s), fill: '#1b1124', stroke: '#ff9a1f', sw: 0.5, cls: 'vp-fx-flap', st: anim(0.22, -b * 0.07) }));
    g.shapes.push(circ({ cx: F(-1.1 * s), cy: F(-0.5 * s), r: F(0.5 * s), fill: '#ffd23f' }));
    g.shapes.push(circ({ cx: F(1.1 * s), cy: F(-0.5 * s), r: F(0.5 * s), fill: '#ffd23f' }));
    out.push(g);
  }
  if (lvl >= 5 && !lite) {
    const moon = group();
    moon.shapes.push(circ({ cx: -56, cy: -60, r: 11, fill: '#fff3c4', op: 0.3, f: blur }));
    moon.shapes.push(circ({ cx: -56, cy: -60, r: 7, fill: '#fff3c4' }));
    out.push(moon);
  }
  if (lvl >= 6 && !lite) {
    const web = group();
    web.shapes.push(path({ d: 'M-40 46 L-40 66', stroke: '#d9d0e6', sw: 0.5, op: 0.8 }));
    out.push(web);
    const spider = group();
    spider.shapes.push(circ({ cx: -40, cy: 67, r: 2.8, fill: '#1b1124', stroke: '#ff9a1f', sw: 0.4, cls: 'vp-fx-bob' }));
    out.push(spider);
  }
  if (lvl >= 7 && !lite) {
    const ghost = group();
    ghost.shapes.push(path({ d: 'M54 -58 Q54 -68 62 -68 Q70 -68 70 -58 L70 -48 L67.3 -50.5 L64.7 -48 L62 -50.5 L59.3 -48 L56.7 -50.5 L54 -48 Z', fill: '#f4f0ff', op: 0.92, cls: 'vp-fx-bob' }));
    ghost.shapes.push(circ({ cx: 59.5, cy: -60, r: 1.2, fill: '#1b1124', cls: 'vp-fx-bob' }));
    ghost.shapes.push(circ({ cx: 64.5, cy: -60, r: 1.2, fill: '#1b1124', cls: 'vp-fx-bob' }));
    out.push(ghost);
  }
}

// ── Entry point ──────────────────────────────────────────────────────────────

// level: 0 = no tier yet (the finish sits on a plain neutral ring), 1–7 = wood … amethyst.
export function renderFinish(key: FinishKey, level: number, lite: boolean, crown: boolean, blurUrl: string): FinishMarkup {
  const lvl = Math.max(0, Math.min(7, level));
  const rnd = seeded(lvl * 131 + key.length * 17 + (lite ? 7 : 3));
  const out: Group[] = [];
  let layers: Layer[] = [];
  switch (key) {
    case 'ring-neon':
      neon(out, lvl, lite, blurUrl, crown);
      break;
    case 'ring-arcade':
      arcade(out, lvl, lite, blurUrl, crown);
      break;
    case 'ring-doodle':
      doodle(out, lvl, lite, blurUrl, rnd, crown);
      break;
    case 'ring-volt':
      volt(out, lvl, lite, blurUrl, rnd);
      break;
    case 'ring-orbit':
      orbit(out, lvl, lite, blurUrl, rnd);
      break;
    case 'ring-aurora':
      layers = auroraLayers(lvl, lite);
      aurora(out, lvl, lite, rnd);
      break;
    case 'ring-flare':
      layers = flareLayers(lvl, lite);
      flare(out, lvl, lite, rnd);
      break;
    case 'ring-blizzard':
      layers = blizzardLayers(lvl, lite);
      blizzard(out, lvl, lite, blurUrl, rnd);
      break;
    case 'ring-witch':
      layers = witchLayers(lvl, lite);
      witch(out, lvl, lite, blurUrl, crown);
      break;
  }
  if (crown) drawCrown(out, key, lite, blurUrl);
  return {
    front: out.map(groupText).join(''),
    layers: layers.map((l) => `<div class="${l.cls}" style="${styleText(l.st)}"></div>`).join(''),
    hideAura: layers.length > 0,
  };
}
