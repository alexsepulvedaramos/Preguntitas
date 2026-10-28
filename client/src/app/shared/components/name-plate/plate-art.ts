import { FRAME_COLOR_KEYS, FrameColorKey, STREAK_TIERS, streakTierFor } from '../../../core/constants/streak-tiers';

// Plates (rama 60, spec §17.18): an illustrated scene behind the wearer's name in the
// featured places — the profile header, the member card banner, a result's leader, the
// duel scoreboard, the monthly awards and the shop. One per ring finish plus a plain one per
// frame colour; the legendary ones (Neón, Arcade, Garabato) grow with the wearer's streak
// tier, step for step with their frame. Every value comes from these constants, never from
// user input, so the markup is safe to trust. Styling and animation live in styles-lime.css
// (.vp-plate*); the legendaries reuse the frame's own .vp-fx-* animations so plate and ring
// blink, chase and boil together.

// Where the plate is drawn: behind a row (a name next to a 40 px avatar) or as the banner of
// a card, whose middle stays clear for the avatar and the frame's icons and crown.
export type PlateContext = 'row' | 'banner';

export const PLATE_FINISHES = [
  'volt',
  'sparks',
  'bubbles',
  'confetti',
  'flare',
  'blizzard',
  'aurora',
  'neon',
  'arcade',
  'doodle',
  'witch',
] as const;
export type PlateFinish = (typeof PLATE_FINISHES)[number];

const F = (n: number) => Math.round(n * 100) / 100;

// A plate key ("plate-neon", "plate-purple") → its scene, or null when it isn't one.
export function plateScene(key: string | null | undefined): { finish: PlateFinish } | { color: FrameColorKey } | null {
  const name = key?.startsWith('plate-') ? key.slice('plate-'.length) : null;
  if (!name) return null;
  if ((PLATE_FINISHES as readonly string[]).includes(name)) return { finish: name as PlateFinish };
  if ((FRAME_COLOR_KEYS as readonly string[]).includes(name)) return { color: name as FrameColorKey };
  return null;
}

export function isPlate(key: string | null | undefined): boolean {
  return plateScene(key) !== null;
}

// Light plates carry dark text; the rest carry white text.
export function plateIsLight(key: string | null | undefined): boolean {
  const scene = plateScene(key);
  return !!scene && ('color' in scene || scene.finish === 'blizzard');
}

// The text colour for content in front of a plate: white on the dark scenes, dark ink on the
// light one (Ventisca), and the theme's text on a frame colour's plain plate, which is mixed
// with the card colour. Empty when there is no plate.
export function plateTextClass(key: string | null | undefined): string {
  const scene = plateScene(key);
  if (!scene) return '';
  if ('color' in scene) return 'text-foreground';
  return scene.finish === 'blizzard' ? 'vp-on-plate vp-on-plate--light' : 'vp-on-plate';
}

// The streak tier level the legendaries grow with: 0 = no tier yet, 1 = wood … 7 = amethyst.
export function plateLevel(streak: number | null | undefined): number {
  const tier = streakTierFor(streak);
  return tier ? STREAK_TIERS.indexOf(tier) + 1 : 0;
}

// ── Shared pieces ──────────────────────────────────────────────────────────────

const flame = (x: number, w: number, h: number, base = 60) =>
  `<g class="vpp-tongue"><path d="M${x} ${base} C ${x - w / 2} ${base}, ${x - w / 2} ${F(base - h * 0.45)}, ${F(x - w * 0.15)} ${F(base - h * 0.7)} C ${F(x - w * 0.05)} ${F(base - h * 0.85)}, ${x} ${F(base - h * 0.9)}, ${F(x + w * 0.05)} ${base - h} C ${F(x + w * 0.2)} ${F(base - h * 0.7)}, ${x + w / 2} ${F(base - h * 0.5)}, ${x + w / 2} ${base} Z" fill="#ff7a00"/><path d="M${x} ${base} C ${F(x - w * 0.28)} ${base}, ${F(x - w * 0.25)} ${F(base - h * 0.35)}, ${F(x - w * 0.05)} ${F(base - h * 0.55)} C ${F(x + w * 0.1)} ${F(base - h * 0.4)}, ${F(x + w * 0.28)} ${F(base - h * 0.3)}, ${F(x + w * 0.28)} ${base} Z" fill="#ffd23f"/></g>`;

const BAT =
  '<svg viewBox="0 0 32 16"><path d="M16 5c1-2 2-2 2-2l.5 2C21 3 25 2 31 4c-3 1-4 3-4 6-2-2-4-2-6 0-1-1-3-2-5 2-2-4-4-3-5-2-2-2-4-2-6 0 0-3-1-5-4-6 6-2 10-1 12.5 1l.5-2s1 0 2 2z"/></svg>';

// ── Rares: one simple movement, no evolution ───────────────────────────────────

// Rayos — «Rayo en el mar»: a branching bolt strikes the horizon now and then.
function volt(banner: boolean): string {
  const fork = (x: number, h: number, s: number) =>
    `<svg class="vpp-bolt" style="left:${x}%;top:0;height:${h}%;width:${s}px" viewBox="0 0 30 60" preserveAspectRatio="none"><path d="M16 0 L12 14 L18 18 L10 32 L16 36 L8 60 M12 14 L4 22 M10 32 L22 42" fill="none" stroke="#f2fbff" stroke-width="1.6" stroke-linejoin="round"/></svg>`;
  const sea = `<svg style="left:0;right:0;bottom:0;width:100%;height:28%" viewBox="0 0 400 20" preserveAspectRatio="none"><path d="M0 4 Q 20 0, 40 4 T 80 4 T 120 4 T 160 4 T 200 4 T 240 4 T 280 4 T 320 4 T 360 4 T 400 4 V20 H0 Z" fill="#0c0820"/><path d="M0 4 Q 20 0, 40 4 T 80 4 T 120 4 T 160 4 T 200 4 T 240 4 T 280 4 T 320 4 T 360 4 T 400 4" fill="none" stroke="#5b4a9a" stroke-width="1"/></svg>`;
  return `<i class="vpp-flash"></i>${sea}${banner ? fork(12, 80, 22) + fork(84, 76, 22) : fork(66, 80, 16)}`;
}

// Chispas — «Purpurina»: glints twinkling over gold.
function sparks(banner: boolean): string {
  return Array.from({ length: banner ? 26 : 16 }, (_, i) => {
    let x = (i * 37 + 11) % 100;
    const y = (i * 53 + 7) % 90;
    if (banner && x > 32 && x < 68 && y < 60) x = x < 50 ? x - 30 : x + 30;
    if (!banner) x = 40 + (x % 45);
    return `<i class="vpp-glint" style="left:${x}%;top:${y}%;animation-delay:${F(-(i % 6) * 0.3)}s;--s:${F(1 + (i % 3) * 0.4)}"></i>`;
  }).join('');
}

// Burbujas — «Fondo marino»: light rays, seaweed and bubbles rising.
function bubbles(banner: boolean): string {
  const spots = banner
    ? [[6, 60, 16], [18, 80, 9], [80, 70, 14], [92, 50, 8], [70, 90, 6]]
    : [[58, 70, 14], [70, 90, 9], [80, 60, 7], [64, 100, 6]];
  const bubs = spots
    .map(([x, y, s], i) => `<i class="vpp-bubble" style="left:${x}%;top:${y}%;width:${s}px;height:${s}px;animation-delay:${F(-i * 1.1)}s"></i>`)
    .join('');
  return `<i class="vpp-rays"></i>${bubs}<svg style="${banner ? 'left:2%' : 'right:16%'};bottom:-2px;width:${banner ? 60 : 50}px;height:70%" viewBox="0 0 60 60" preserveAspectRatio="none"><path d="M10 60 C 4 44, 18 36, 12 20 C 8 10, 16 4, 14 0 L 18 0 C 22 10, 14 18, 20 30 C 26 44, 14 50, 18 60 Z M40 60 C 36 48, 48 42, 42 30 L 46 28 C 54 42, 44 50, 46 60 Z" fill="#06384f"/></svg>`;
}

// Confeti — «Fiesta»: paper confetti falling and spinning.
function confetti(banner: boolean): string {
  const cols = ['#ff8fc7', '#7fe9ff', '#7dff7a', '#ffd400', '#c56cf0'];
  return Array.from({ length: banner ? 22 : 12 }, (_, i) => {
    let x = (i * 41 + 5) % 100;
    if (banner && x > 34 && x < 66) x = x < 50 ? x - 30 : x + 30;
    if (!banner) x = 40 + (x % 46);
    return `<i class="vpp-confetti" style="left:${x}%;background:${cols[i % 5]};animation-delay:${F(-(i * 0.37) % 3.6)}s;--dx:${(i % 2 ? -1 : 1) * (4 + (i % 4) * 3)}px"></i>`;
  }).join('');
}

// ── Epics: a richer scene, no evolution ────────────────────────────────────────

// Llamarada — «Acampada»: a campfire on a ring of stones under the stars.
function flare(banner: boolean): string {
  const pine = (x: number, h: number, w: number) => `<path d="M${x} ${52 - h} L${x - w} 52 L${x + w} 52 Z" fill="#0b0a18"/>`;
  const fire = `<ellipse cx="90" cy="54" rx="26" ry="5" fill="rgb(255 120 30 / .35)"/>${flame(90, 26, 44, 53)}${flame(80, 16, 28, 54)}${flame(100, 16, 30, 54)}<path d="M84 40 C 86 46, 94 46, 96 40 C 94 47, 92 50, 90 50 C 88 50, 86 47, 84 40 Z" fill="#fff6c4" class="vpp-core"/><rect x="74" y="51" width="34" height="4" rx="2" fill="#4a2410" transform="rotate(-10 91 53)"/><rect x="74" y="51" width="34" height="4" rx="2" fill="#5a2c14" transform="rotate(10 91 53)"/>${[70, 76, 83, 90, 97, 104, 110].map((x, i) => `<ellipse cx="${x}" cy="${55 + (i % 2)}" rx="3.6" ry="2.6" fill="#5b5550" stroke="#2a2522" stroke-width=".6"/>`).join('')}`;
  const swirls = [0, 1, 2, 3, 4, 5].map((i) => `<i class="vpp-swirl" style="animation-delay:${F(-i * 0.45)}s;--sx:${i % 2 ? -1 : 1}"></i>`).join('');
  if (banner)
    // The avatar covers the middle: pines on the left, the fire on its own on the right.
    return `<i class="vpp-stars"></i><i class="vpp-ground"></i><svg class="vpp-pines" viewBox="0 0 50 60" preserveAspectRatio="xMinYMax meet">${pine(14, 30, 9)}${pine(30, 22, 7)}<path d="M0 52 L50 52 L50 60 L0 60 Z" fill="#0d0805"/></svg><i class="vpp-glow"></i><svg class="vpp-fire" viewBox="62 6 56 54" preserveAspectRatio="xMaxYMax meet"><path d="M62 52 L118 52 L118 60 L62 60 Z" fill="#0d0805"/>${fire}</svg>${swirls}`;
  return `<i class="vpp-stars"></i><i class="vpp-glow"></i><svg class="vpp-scene" viewBox="0 0 160 60" preserveAspectRatio="xMaxYMax meet">${pine(18, 30, 9)}${pine(34, 22, 7)}${pine(148, 34, 10)}${pine(134, 24, 7)}<path d="M0 52 L160 52 L160 60 L0 60 Z" fill="#0d0805"/>${fire}</svg>${swirls}`;
}

// Ventisca — «Colinas de día»: snowy hills with small pines and snow falling.
function blizzard(): string {
  return `<svg class="vpp-hills" viewBox="0 0 400 60" preserveAspectRatio="none"><path d="M0 34 C 60 18, 110 30, 170 22 C 240 12, 300 30, 400 16 L400 60 L0 60 Z" fill="#c4def2"/><g fill="#7fa6c6"><path d="M250 30 l6 -16 l6 16 z"/><path d="M262 32 l5 -13 l5 13 z"/><path d="M330 24 l7 -18 l7 18 z"/><path d="M344 26 l5 -12 l5 12 z"/><path d="M120 36 l5 -12 l5 12 z"/></g><path d="M0 46 C 80 34, 150 48, 230 40 C 300 32, 350 46, 400 38 L400 60 L0 60 Z" fill="#f4f9fd"/><path d="M0 46 C 80 34, 150 48, 230 40 C 300 32, 350 46, 400 38" fill="none" stroke="#a9c8e0" stroke-width="1.2" vector-effect="non-scaling-stroke"/><rect x="0" y="57" width="400" height="3" fill="#cfe1ef"/></svg><i class="vpp-snow"></i><i class="vpp-snow vpp-snow--far"></i>`;
}

// Aurora: a night sky with waving northern lights over dark mountains.
function aurora(): string {
  return `<i class="vpp-stars"></i><i class="vpp-ribbon vpp-ribbon--a"></i><i class="vpp-ribbon vpp-ribbon--b"></i><i class="vpp-ribbon vpp-ribbon--c"></i><svg class="vpp-mountains" viewBox="0 0 400 40" preserveAspectRatio="none"><path d="M0 40 L0 26 L40 14 L70 24 L110 6 L150 22 L190 12 L230 26 L270 8 L310 20 L350 10 L400 24 L400 40 Z" fill="#03060f"/></svg><i class="vpp-shooting-star"></i>`;
}

// ── Legendaries: they grow with the streak, step for step with their frame ─────

// Neón: a brick wall whose neon signs grow with the frame's tubes and lights.
const WAVE = 'M2 14 q 8 -14 16 0 t 16 0 t 16 0 t 16 0';
const STAR = 'M12 1 l3.6 8 l8.8 .9 l-6.6 5.8 l2 8.6 l-7.8 -4.6 l-7.8 4.6 l2 -8.6 l-6.6 -5.8 l8.8 -.9 z';

// A tube: its glowing stroke plus a white core. From gold on it cycles colour with the ring.
function tube(d: string, color: 'pink' | 'cyan', level: number, width = 2.2, extra = ''): string {
  const cycle = level >= 5 ? ` vpp-cycle-${color}` : '';
  return `<g class="vp-fx-flicker"><path class="vpp-tube vpp-tube--${color}${cycle}" d="${d}" stroke-width="${width}" ${extra}/><path class="vpp-tube-core" d="${d}" stroke-width="${F(width * 0.35)}" ${extra}/></g>`;
}

function neon(level: number, banner: boolean): string {
  let h = `<i class="vpp-wallglow" style="right:${banner ? '-6%' : '4%'};width:${banner ? 46 : 60}%;--wg:rgb(${level >= 7 ? '162 75 255' : '255 43 214'} / ${F(0.16 + level * 0.035)})"></i>`;
  if (banner && level >= 2) h += `<i class="vpp-wallglow" style="left:-6%;width:40%;--wg:rgb(0 229 255 / ${F(0.1 + level * 0.02)})"></i>`;
  // The wave sign, always; a light runs along it from bronze (the ring's running light).
  const wavePos = banner ? 'left:5%;top:34%;width:27%' : 'right:30%;top:26%;width:22%';
  h += `<svg style="${wavePos}" viewBox="0 0 68 28">${tube(WAVE, 'pink', level, level >= 1 ? 2.6 : 2.2)}${level >= 3 ? `<path class="vpp-chaser" d="${WAVE}" stroke-width="1.6" stroke-dasharray="10 90" style="--cl:-100;--cd:${level >= 5 ? 1.6 : 2.4}s"/>` : ''}</svg>`;
  // The star sign from stone (the ring's second tube).
  if (level >= 2)
    h += `<svg style="${banner ? 'right:7%;top:18%;width:16%' : 'right:17%;top:14%;width:9%'}" viewBox="-2 -2 28 28">${tube(STAR, 'cyan', level, 2)}</svg>`;
  // A cyan floor tube from silver, with its own light (the ring's second light).
  if (level >= 4) {
    const d = banner ? 'M4 2 H396' : 'M150 2 H396';
    h += `<svg style="left:0;right:0;bottom:${banner ? 10 : 12}%;width:100%;height:4px" viewBox="0 0 400 4" preserveAspectRatio="none">${tube(d, 'cyan', level, 1.6, 'vector-effect="non-scaling-stroke"')}<path class="vpp-chaser" d="${d}" stroke-width="1.6" vector-effect="non-scaling-stroke" stroke-dasharray="24 376" style="--cl:400;--cd:3s"/></svg>`;
  }
  // Marquee bulbs from sapphire (the ring's bulbs); on the banner the top row skips the icons.
  if (level >= 6) {
    const n = banner ? 18 : 16;
    for (let i = 0; i < n; i++) {
      const x = 3 + (i * 94) / (n - 1);
      const delay = `animation-delay:${F(-(i % 3) * 0.3)}s`;
      if (!(banner && x > 30 && x < 70)) h += `<i class="vpp-bulb vp-fx-chase" style="left:${F(x)}%;top:5px;${delay}"></i>`;
      h += `<i class="vpp-bulb vp-fx-chase" style="left:${F(x)}%;bottom:4px;${delay}"></i>`;
    }
  }
  // Amethyst: a violet tube framing the plate (the third tube) and the floor catching the light.
  if (level >= 7) {
    const r = banner ? 10 : 12;
    h += `<i class="vpp-reflection"></i><svg style="inset:0;width:100%;height:100%" viewBox="0 0 400 100" preserveAspectRatio="none"><rect x="3" y="3" width="394" height="94" rx="${r}" class="vpp-tube vpp-tube--violet" fill="none" stroke-width="1.6" vector-effect="non-scaling-stroke"/><rect x="3" y="3" width="394" height="94" rx="${r}" class="vpp-tube-core" fill="none" stroke-width=".6" vector-effect="non-scaling-stroke"/></svg>`;
  }
  return h;
}

// Arcade: a pixel game level in the frame's own colours at each tier (ARCADE_PALETTES).
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
const COIN = ['.###.', '#ooo#', '#oXo#', '#ooo#', '.###.'];
const SLIME = ['..XXXX..', '.XXXXXX.', 'XX.XX.XX', 'XXXXXXXX', 'XXXXXXXX', 'X.X..X.X'];

function sprite(rows: string[], x: number, y: number, u: number, colors: Record<string, string>): string {
  return rows
    .map((row, ry) =>
      [...row]
        .map((ch, rx) => (ch === '.' ? '' : `<rect x="${F(x + rx * u)}" y="${F(y + ry * u)}" width="${F(u)}" height="${F(u)}" fill="${colors[ch] ?? colors['X']}"/>`))
        .join('')
    )
    .join('');
}

function arcade(level: number, banner: boolean): string {
  const pal = ARCADE_PALETTES[level];
  const W = 400;
  const H = banner ? 100 : 58;
  const u = banner ? 4 : 3;
  const clear = (x: number) => banner && x > W * 0.3 && x < W * 0.7;
  const blink = level >= 3 ? 'vp-fx-chase' : 'vp-fx-blink';
  let svg = '';
  // Pixel stars, more with the tier.
  for (let i = 0; i < 6 + level * 3; i++) {
    const x = (i * 97 + 31) % W;
    const y = 4 + ((i * 53) % (H * 0.5));
    if (!clear(x) || y > H * 0.7)
      svg += `<rect x="${x}" y="${F(y)}" width="${F(u * 0.7)}" height="${F(u * 0.7)}" fill="#fff" class="vp-fx-blink" style="animation-delay:${F(-(i % 4) * 0.4)}s"/>`;
  }
  // Stone: a row of dots (the frame's inner dots) and coins on the sides.
  if (level >= 2) {
    for (let x = banner ? 8 : 150; x < W - 8; x += u * 3)
      if (!clear(x)) svg += `<rect x="${x}" y="${F(H * 0.52)}" width="${F(u * 0.7)}" height="${F(u * 0.7)}" fill="${pal[Math.floor(x / (u * 3)) % pal.length]}" opacity=".85"/>`;
    (banner ? [70, 312] : [250, 310]).forEach((x, i) => {
      svg += `<g class="vpp-bob" style="animation-delay:${F(-i * 0.6)}s">${sprite(COIN, x, H * 0.22, u * 0.8, { '#': '#7a5a00', o: '#fff200', X: '#c99a00' })}</g>`;
    });
  }
  // Sapphire: a rainbow platform.
  if (level >= 6)
    for (const px of banner ? [44, 270] : [200])
      pal.forEach((c, i) => {
        svg += `<rect x="${F(px + i * u * 2.6)}" y="${F(H * 0.42)}" width="${F(u * 2.6)}" height="${u * 2}" fill="${c}" stroke="#111" stroke-width=".6"/>`;
      });
  // The ground: blocks in the frame's colours, blinking, then chasing like the ring from bronze.
  const gb = u * 3;
  const gy = H - gb;
  for (let x = 0, k = 0; x < W; x += gb, k++)
    svg += `<rect x="${x}" y="${gy}" width="${gb}" height="${gb}" fill="${pal[k % pal.length]}" stroke="#111" stroke-width=".8" class="${blink}" style="animation-delay:${F(-(k % 3) * 0.3)}s"/>`;
  // Silver: a red slime patrolling the ground.
  if (level >= 4)
    svg += `<g class="vpp-patrol" style="--pd:${banner ? -50 : -60}px"><g class="vpp-slime">${sprite(SLIME, banner ? 300 : 330, gy - u * 6, u, { X: '#ff4d4d' })}</g></g>`;
  // Amethyst: pixel fireworks on the sides.
  if (level >= 7)
    (banner ? [[70, 22], [320, 26], [100, 40]] : [[230, 14], [290, 22], [350, 12]]).forEach(([x, y], i) => {
      let b = '';
      for (let a = 0; a < 8; a++) {
        const ang = (a / 8) * Math.PI * 2;
        b += `<rect x="${Math.round(x + Math.cos(ang) * u * 3)}" y="${Math.round(y + Math.sin(ang) * u * 3)}" width="${F(u * 0.8)}" height="${F(u * 0.8)}" fill="${pal[(a + i) % pal.length]}"/>`;
      }
      svg += `<g class="vpp-burst" style="animation-delay:${F(-i * 0.8)}s">${b}</g>`;
    });
  let html = '';
  // Gold: pixel hills scroll behind the ground — the level moves.
  if (level >= 5) html += `<i class="vpp-pixel-hills" style="bottom:${gb}px"></i>`;
  html += `<svg class="vpp-pixels" style="inset:0;width:100%;height:100%" viewBox="0 0 ${W} ${H}" preserveAspectRatio="xMidYMax slice">${svg}</svg>`;
  // Amethyst: an arcade-cabinet bezel in the six colours, chasing.
  if (level >= 7) {
    let bezel = '';
    for (let x = 0, k = 0; x < W; x += 10, k++)
      bezel += `<rect x="${x}" y="0" width="10" height="4" fill="${pal[k % pal.length]}" class="vp-fx-chase" style="animation-delay:${F(-(k % 3) * 0.3)}s"/>`;
    html += `<svg class="vpp-pixels" style="left:0;right:0;top:0;width:100%;height:4px" viewBox="0 0 ${W} 4" preserveAspectRatio="none">${bezel}</svg><i style="inset:0;box-shadow:inset 3px 0 0 ${pal[0]}, inset -3px 0 0 ${pal[5]}"></i>`;
  }
  return html;
}

// Garabato: a blackboard filling with marker doodles, each drawn in three jittered frames
// boiled with the frame's own vp-fx-boil, so board and ring boil together.
const INK = { cream: '#fff3b0', pink: '#ff8fc7', cyan: '#7fe9ff', ink: '#2a2233', paper: '#fffaf0' };
type Pt = [number, number] | null;
const ring = (n: number, f: (a: number) => [number, number]): Pt[] => Array.from({ length: n }, (_, i) => f(i));
const SHAPES: Record<string, Pt[]> = {
  squiggle: [[0, 10], [5, 3], [10, 12], [15, 4], [20, 12], [25, 5], [30, 11]],
  star: [[15, 0], [19, 10], [30, 11], [21, 18], [24, 29], [15, 23], [6, 29], [9, 18], [0, 11], [11, 10], [15, 0]],
  spiral: ring(22, (i) => [15 + Math.cos(i * 0.62) * (1 + i * 0.62), 15 + Math.sin(i * 0.62) * (1 + i * 0.62)]),
  cloud: ring(25, (i) => {
    const a = (i / 24) * Math.PI * 2;
    const r = 9 + 2.6 * Math.abs(Math.sin(a * 3));
    return [16 + Math.cos(a) * r * 1.4, 14 + Math.sin(a) * r * 0.8];
  }),
  zigzag: [[0, 12], [6, 2], [12, 12], [18, 2], [24, 12], [30, 2]],
  flower: ring(41, (i) => {
    const a = (i / 40) * Math.PI * 2;
    const r = 6 + 6 * Math.abs(Math.cos(a * 2.5));
    return [15 + Math.cos(a) * r, 15 + Math.sin(a) * r];
  }),
  arrow: [[0, 22], [10, 14], [20, 16], [30, 6], null, [22, 5], [30, 6], [29, 14]],
  plane: [[0, 12], [30, 2], [12, 16], [0, 12], null, [12, 16], [14, 24], [18, 14]],
  spark: [[15, 2], [15, 28], null, [2, 15], [28, 15], null, [6, 6], [24, 24], null, [24, 6], [6, 24]],
};

// Deterministic jitter so a doodle looks the same on every render.
function seeded(seed: number): () => number {
  let s = seed;
  return () => (s = (s * 9301 + 49297) % 233280) / 233280;
}

function jitter(pts: Pt[], amp: number, rnd: () => number): string {
  let d = '';
  let pen = false;
  for (const p of pts) {
    if (!p) {
      pen = false;
      continue;
    }
    d += `${pen ? 'L' : 'M'}${F(p[0] + (rnd() - 0.5) * amp)} ${F(p[1] + (rnd() - 0.5) * amp)} `;
    pen = true;
  }
  return d;
}

function doodleAt(shape: string, color: string, x: number, y: number, size: number): string {
  const rnd = seeded(shape.length * 31 + Math.round(x * 7 + y * 3));
  const frames = [0, 1, 2]
    .map((f) => `<path class="vp-fx-boil" style="animation-delay:${F(-f * 0.3)}s" d="${jitter(SHAPES[shape], 1.6, rnd)}" stroke="${color}" stroke-width="2"/>`)
    .join('');
  return `<svg class="vpp-doodle" style="left:${x}%;top:${y}%;width:${F(size)}px;height:${F(size)}px" viewBox="-2 -2 34 34">${frames}</svg>`;
}

// A sticker like the frame's: a wobbly paper blob, the doodle inked on top, its colour printed off the line.
function sticker(x: number, y: number, size: number, color: string, shape = 'star', tilt = -8): string {
  const rnd = seeded(Math.round(x * 13 + y));
  const blob = ring(10, (k) => {
    const a = (k / 9) * Math.PI * 2;
    const r = 14 + (rnd() - 0.5) * 3;
    return [15 + Math.cos(a) * r, 15 + Math.sin(a) * r];
  });
  const d = jitter(SHAPES[shape].map((p) => p && [7 + p[0] * 0.53, 7 + p[1] * 0.53]), 0.6, rnd);
  return `<svg class="vpp-doodle vp-fx-wiggle" style="left:${x}%;top:${y}%;width:${size}px;height:${size}px;--d:2.2s" viewBox="-2 -2 34 34"><g transform="rotate(${tilt} 15 15)"><path d="${jitter(blob, 0.4, rnd)}Z" fill="${INK.paper}" stroke="${INK.ink}" stroke-width="1.2"/><path d="${d}" stroke="${color}" stroke-width="3" transform="translate(1.2 1.2)"/><path d="${d}" stroke="${INK.ink}" stroke-width="1.4"/></g></svg>`;
}

function doodle(level: number, banner: boolean): string {
  // [x%, y%]; on the banner the middle third stays empty for the avatar, icons and crown.
  const P = banner
    ? { a: [6, 14], b: [20, 52], c: [76, 12], d: [86, 50], e: [4, 58], f: [70, 56], g: [26, 10], h: [90, 16], s1: [8, 30], s2: [78, 28], s3: [88, 58] }
    : { a: [46, 10], b: [57, 50], c: [67, 8], d: [75, 48], e: [47, 52], f: [82, 30], g: [54, 6], h: [63, 32], s1: [71, 20], s2: [58, 28], s3: [80, 4] };
  const at = (k: keyof typeof P) => P[k] as [number, number];
  const sz = banner ? 34 : 22;
  let h = doodleAt('squiggle', INK.cream, ...at('a'), sz) + doodleAt('star', INK.cream, ...at('c'), sz * 0.8);
  if (level >= 1) h += doodleAt('spiral', INK.cream, ...at('b'), sz * 0.8) + doodleAt('cloud', INK.cream, ...at('d'), sz);
  // Stone: pink doodles (the frame's second line).
  if (level >= 2) h += doodleAt('zigzag', INK.pink, ...at('e'), sz) + doodleAt('flower', INK.pink, ...at('g'), sz * 0.8);
  // Bronze: a star sticker like the frame's, and little sparkles popping in and out.
  if (level >= 3) {
    (banner ? [[2, 42], [95, 44], [22, 74]] : [[50, 28], [66, 60]]).forEach(([x, y], i) => {
      h += `<span class="vpp-pop" style="left:${x}%;top:${y}%;animation-delay:${F(-i * 0.7)}s">${doodleAt('spark', i % 2 ? INK.cyan : INK.pink, 0, 0, banner ? 16 : 12)}</span>`;
    });
    h += sticker(...at('s1'), banner ? 40 : 26, INK.pink);
  }
  // Silver: cyan doodles (the frame's third line).
  if (level >= 4) h += doodleAt('arrow', INK.cyan, ...at('f'), sz) + doodleAt('cloud', INK.cyan, ...at('h'), sz * 0.9);
  // Gold: a paper plane flying.
  if (level >= 5)
    h += `<span class="vpp-plane" style="left:${banner ? 4 : 30}%;top:${banner ? 34 : 20}%;--px:${banner ? 30 : 70}px">${doodleAt('plane', INK.cyan, 0, 0, sz)}</span>`;
  // Sapphire: two more stickers.
  if (level >= 6) h += sticker(...at('s2'), banner ? 36 : 24, INK.cyan, 'flower', 10) + sticker(...at('s3'), banner ? 30 : 22, '#ffd400', 'spiral', -14);
  // Amethyst: a doodled three-colour border round the plate.
  if (level >= 7) {
    const W = 400;
    const H = 100;
    const m = 4;
    const rnd = seeded(77);
    const rect = (o: number): Pt[] => [[m + o, m], [W / 2, m - o], [W - m, m + o], [W - m - o, H / 2], [W - m, H - m], [W / 2, H - m + o], [m, H - m - o], [m + o, H / 2], [m + o, m]];
    [INK.cream, INK.pink, INK.cyan].forEach((c, i) => {
      const frames = [0, 1, 2]
        .map((f) => `<path class="vp-fx-boil" style="animation-delay:${F(-f * 0.3)}s" d="${jitter(rect(i * 1.5), 2.5, rnd)}" stroke="${c}" stroke-width="${F(2 - i * 0.4)}" vector-effect="non-scaling-stroke"/>`)
        .join('');
      h += `<svg class="vpp-doodle" style="inset:0;width:100%;height:100%" viewBox="0 0 ${W} ${H}" preserveAspectRatio="none">${frames}</svg>`;
    });
  }
  return h;
}

// ── Seasonal ───────────────────────────────────────────────────────────────────

// Noche de brujas — «Casa encantada»: a haunted house with flickering windows, gravestones,
// bats and a full moon. The Halloween frame draws its own moon from gold on at full size, so
// the banner drops this one there.
function witch(level: number, banner: boolean): string {
  const house = `<g transform="translate(${banner ? 14 : 300} 16)"><path d="M0 44 V20 L14 6 L28 20 V44 Z M24 20 V2 L34 -6 L44 2 V44 H28 Z" fill="#0a0716"/>${[[6, 24], [17, 24], [6, 34], [30, 10], [35, 26]]
    .map(([x, y], i) => `<rect class="vpp-window" style="animation-delay:${F(-i * 0.3)}s" x="${x}" y="${y}" width="5" height="6" fill="#ffcf5a"/>`)
    .join('')}</g>`;
  const stone = (x: number, y: number, s: number, tilt: number) =>
    `<g transform="translate(${x} ${y}) rotate(${tilt}) scale(${s})"><path d="M-4 0 V-7 Q-4 -11 0 -11 Q4 -11 4 -7 V0 Z" fill="#8f8a9e" stroke="#4a4658" stroke-width=".7"/><path d="M-1.6 -7.5 H1.6 M0 -9 V-5" stroke="#5c586c" stroke-width=".7"/></g>`;
  const graves = banner
    ? stone(80, 53, 1, -6) + stone(96, 54, 0.8, 5) + stone(312, 53, 0.95, 4) + stone(330, 54, 0.8, -5)
    : stone(230, 53, 0.9, -5) + stone(246, 54, 0.75, 6) + stone(272, 53, 0.85, 3);
  const moon = banner && level >= 5 ? '' : `<i class="vpp-moon" style="${banner ? 'right:8%;top:10%;width:34px;height:34px' : 'right:30%;top:10%;width:22px;height:22px'}"></i>`;
  const bats = [0, 1]
    .map((i) => `<i class="vpp-bat" style="left:${banner ? (i ? 60 : 2) : 40}%;top:${12 + i * 26}%;--bw:${banner ? 120 : 160}px;animation-delay:${-i * 3}s${i ? ';width:14px;height:7px' : ''}">${BAT}</i>`)
    .join('');
  return `${moon}${bats}<i class="vpp-ground vpp-ground--night"></i><svg style="left:0;right:0;bottom:0;width:100%;height:100%" viewBox="0 0 400 60" preserveAspectRatio="${banner ? 'xMidYMax meet' : 'xMaxYMax slice'}"><path d="M0 52 C 120 46, 260 56, 400 50 L400 60 L0 60 Z" fill="#0a0716"/>${graves}${house}</svg>`;
}

// ── Entry point ────────────────────────────────────────────────────────────────

// The inner markup of a plate; the host element carries `vp-plate vp-plate--<scene>`.
export function renderPlate(key: string | null | undefined, streak: number | null | undefined, context: PlateContext): string {
  const scene = plateScene(key);
  if (!scene) return '';
  if ('color' in scene) return '<i class="vpp-tint-glow"></i>';
  const banner = context === 'banner';
  const level = plateLevel(streak);
  switch (scene.finish) {
    case 'volt':
      return volt(banner);
    case 'sparks':
      return sparks(banner);
    case 'bubbles':
      return bubbles(banner);
    case 'confetti':
      return confetti(banner);
    case 'flare':
      return flare(banner);
    case 'blizzard':
      return blizzard();
    case 'aurora':
      return aurora();
    case 'neon':
      return neon(level, banner);
    case 'arcade':
      return arcade(level, banner);
    case 'doodle':
      return doodle(level, banner);
    case 'witch':
      return witch(level, banner);
  }
}

// The class that styles the plate's background: the finish, or "tint" for a frame colour.
export function plateClass(key: string | null | undefined): string {
  const scene = plateScene(key);
  if (!scene) return '';
  return 'color' in scene ? 'vp-plate--tint' : `vp-plate--${scene.finish}`;
}

export function plateTintColor(key: string | null | undefined): string | null {
  const scene = plateScene(key);
  return scene && 'color' in scene ? `var(--frame-${scene.color})` : null;
}
