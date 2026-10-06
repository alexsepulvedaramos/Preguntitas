import { CROWN_LOOK, FRAME_LOOKS, StreakTierKey } from '../../../core/constants/streak-tiers';

// Builds the markup of a streak frame (rama 19). Coordinates: viewBox -100..100 over a box
// twice the avatar's size, so the avatar's radius is 50 and the rim spans 49–59. Every value
// comes from our own constants, never from user input, so the string is safe to trust.
// Styling and animation live in styles-lime.css (.vp-frame*).

export interface FrameOptions {
  tier: StreakTierKey | null;
  crown: boolean;
  // Light version for list sizes: rim, shine and gems only — no particles or ornaments.
  lite: boolean;
  // Day count on the ribbon (large sizes only).
  ribbon: number | null;
  uid: string;
}

const f = (n: number) => n.toFixed(2);

function polar(deg: number, r: number): [number, number] {
  const rad = (deg * Math.PI) / 180;
  return [Math.sin(rad) * r, -Math.cos(rad) * r];
}

// Deterministic pseudo-random sequence so a frame looks the same on every render.
function seeded(seed: number): () => number {
  let s = seed;
  return () => (s = (s * 9301 + 49297) % 233280) / 233280;
}

function gradient(id: string, stops: string[], x2 = 1, y2 = 1): string {
  const s = stops.map((c, k) => `<stop offset="${k / (stops.length - 1)}" stop-color="${c}"/>`).join('');
  return `<linearGradient id="${id}" x1="0" y1="0" x2="${x2}" y2="${y2}">${s}</linearGradient>`;
}

function crownMarkup(id: string, lite: boolean): string {
  const c = CROWN_LOOK;
  // On list sizes the crown sits tilted on the avatar's top-right corner, so it doesn't
  // climb into the row above.
  const scale = lite ? 'transform="translate(40 -44) rotate(30) scale(1.25) translate(0 73)"' : '';
  return `<g ${scale}>
    <path d="M-19 -60 L-23 -83 L-11 -73 L0 -91 L11 -73 L23 -83 L19 -60 Q0 -55 -19 -60 Z" fill="url(#${id}cr)" stroke="${c.stops[0]}" stroke-width="1.2" stroke-linejoin="round"/>
    <path d="M-19 -62 Q0 -57 19 -62" fill="none" stroke="${c.stops[0]}" stroke-width="1"/>
    <g style="color:${c.gem}">${lite ? '' : `<circle class="vp-frame__pulse" cx="0" cy="-71" r="7" fill="${c.gem}" filter="url(#${id}blur)"/>`}<circle cx="0" cy="-71" r="4" fill="url(#${id}gem)"/></g>
    <g style="color:${c.side}"><circle cx="-12" cy="-65" r="2.4" fill="url(#${id}gem)"/><circle cx="12" cy="-65" r="2.4" fill="url(#${id}gem)"/></g>
    <circle cx="0" cy="-91" r="2.4" fill="#fff6d5"/><circle cx="-23" cy="-83" r="2" fill="#fff6d5"/><circle cx="23" cy="-83" r="2" fill="#fff6d5"/></g>`;
}

export function renderStreakFrame(o: FrameOptions): string {
  const { tier, crown, lite, uid: id } = o;
  const look = tier ? FRAME_LOOKS[tier] : null;
  const rnd = seeded((tier?.length ?? 1) * 131 + (lite ? 7 : 3));
  let html = '';

  if (look?.aura && !lite)
    html += `<div class="vp-frame__aura" style="background:radial-gradient(circle, ${look.aura} 0%, transparent 70%)"></div>`;
  if (look?.rays && !lite)
    html += `<div class="vp-frame__rays" style="background:repeating-conic-gradient(from 0deg, ${look.b}66 0deg 3deg, transparent 3deg 12deg)"></div>`;
  if (look?.nebula)
    html += `<div class="vp-frame__band vp-frame__nebula" style="background:conic-gradient(${[...look.nebula, look.nebula[0]].join(',')})"></div>`;

  let svg = `<svg class="vp-frame__back" viewBox="-100 -100 200 200" aria-hidden="true"><defs>
    <filter id="${id}blur" x="-50%" y="-50%" width="200%" height="200%"><feGaussianBlur stdDeviation="3"/></filter>
    <radialGradient id="${id}gem" cx=".35" cy=".3" r=".75"><stop offset="0" stop-color="#fff"/><stop offset=".35" stop-color="currentColor"/><stop offset="1" stop-color="#000" stop-opacity=".55"/></radialGradient>
    ${look?.stops ? gradient(`${id}m`, look.stops) : ''}
    ${crown ? gradient(`${id}cr`, CROWN_LOOK.stops) : ''}
  </defs>`;

  if (look?.facets && look.facet) {
    // Cut stone: four triangles per facet, see-through so the nebula glows beneath.
    const n = lite ? Math.round(look.facets / 2) : look.facets;
    const sh = look.facet;
    for (let k = 0; k < n; k++) {
      const a0 = (k / n) * 360, a1 = ((k + 1) / n) * 360, am = (a0 + a1) / 2;
      const [x0, y0] = polar(a0, 49), [x1, y1] = polar(a1, 49), [x2, y2] = polar(a1, 59), [x3, y3] = polar(a0, 59), [xm, ym] = polar(am, 54);
      svg += `<path d="M${f(x0)} ${f(y0)} L${f(x1)} ${f(y1)} L${f(xm)} ${f(ym)} Z" fill="${sh[k % 2 ? 0 : 1]}" opacity=".72"/>
        <path d="M${f(x1)} ${f(y1)} L${f(x2)} ${f(y2)} L${f(xm)} ${f(ym)} Z" fill="${sh[2]}" opacity=".5"/>
        <path d="M${f(x2)} ${f(y2)} L${f(x3)} ${f(y3)} L${f(xm)} ${f(ym)} Z" fill="${sh[k % 2 ? 1 : 0]}" opacity=".66"/>
        <path d="M${f(x3)} ${f(y3)} L${f(x0)} ${f(y0)} L${f(xm)} ${f(ym)} Z" fill="${sh[0]}" opacity=".55"/>`;
    }
    svg += `<circle r="59.2" fill="none" stroke="${sh[2]}" stroke-width=".9" opacity=".9"/><circle r="48.8" fill="none" stroke="${sh[0]}" stroke-width="1.2"/>`;
  } else if (look?.stops) {
    // Metal, wood or stone: shadow, gradient band, bevel lines, then the material's texture.
    svg += `<circle r="54" fill="none" stroke="#000" stroke-width="12" opacity=".4"/>
      <circle r="54" fill="none" stroke="url(#${id}m)" stroke-width="10"/>
      <circle r="58.4" fill="none" stroke="${look.stops[2]}" stroke-width=".9" opacity=".85"/>
      <circle r="49.6" fill="none" stroke="${look.stops[0]}" stroke-width="1"/>`;
    if (look.engraved && !lite)
      svg += `<circle r="54" fill="none" stroke="${look.stops[0]}" stroke-width="3.2" stroke-dasharray="1.2 3.2" opacity=".55"/>
        <circle r="51.4" fill="none" stroke="${look.stops[0]}" stroke-width=".6" opacity=".6"/><circle r="56.6" fill="none" stroke="${look.stops[0]}" stroke-width=".6" opacity=".6"/>`;
    if (look.hot && !lite)
      svg += `<circle class="vp-frame__pulse" r="54" fill="none" stroke="#ff7a1a" stroke-width="10" filter="url(#${id}blur)"/>`;
    if (look.grain) {
      for (const r of [51.2, 53, 55.2, 57])
        svg += `<circle r="${r}" fill="none" stroke="${look.grain}" stroke-width=".7" stroke-dasharray="${f(6 + rnd() * 10)} ${f(2 + rnd() * 4)} ${f(14 + rnd() * 12)} ${f(3 + rnd() * 3)}" opacity=".6" transform="rotate(${(rnd() * 360).toFixed(0)})"/>`;
      for (let k = 0; k < 3; k++) {
        const [x, y] = polar(rnd() * 360, 54);
        svg += `<ellipse cx="${f(x)}" cy="${f(y)}" rx="2.4" ry="1.4" fill="${look.grain}" opacity=".7" transform="rotate(${(rnd() * 180).toFixed(0)} ${f(x)} ${f(y)})"/>`;
      }
    }
    if (look.speckle)
      for (let k = 0; k < (lite ? 30 : 90); k++) {
        const [x, y] = polar(rnd() * 360, 49.8 + rnd() * 8.4);
        svg += `<circle cx="${f(x)}" cy="${f(y)}" r="${f(0.25 + rnd() * 0.55)}" fill="${rnd() > 0.5 ? '#1f2225' : '#e9ecef'}" opacity="${f(0.4 + rnd() * 0.5)}"/>`;
      }
    for (let k = 0; k < (look.notches ?? 0); k++) {
      const ang = (k / look.notches!) * 360;
      const [x0, y0] = polar(ang, 49.6), [x1, y1] = polar(ang, 52.4), [x2, y2] = polar(ang, 55.6), [x3, y3] = polar(ang, 58.4);
      svg += `<path d="M${f(x0)} ${f(y0)} L${f(x1)} ${f(y1)} M${f(x2)} ${f(y2)} L${f(x3)} ${f(y3)}" stroke="#1d2023" stroke-width="1.2" stroke-linecap="round" opacity=".75"/>`;
    }
  }

  if (look && !lite) {
    for (let k = 0; k < (look.studs ?? 0); k++) {
      const [x, y] = polar(45 + (k / look.studs!) * 360, 54);
      svg += `<circle cx="${f(x)}" cy="${f(y)}" r="3.4" fill="${look.stud![1]}"/><circle cx="${f(x - 0.9)}" cy="${f(y - 0.9)}" r="1.6" fill="${look.stud![0]}"/>`;
    }
    if (look.sideGems)
      for (const ang of [90, 270]) {
        const [x, y] = polar(ang, 54);
        svg += `<g style="color:${look.sideGems}"><circle class="vp-frame__pulse" cx="${f(x)}" cy="${f(y)}" r="9" fill="${look.sideGems}" filter="url(#${id}blur)"/>
          <path d="M${f(x)} ${f(y - 7)} L${f(x + 5.5)} ${f(y)} L${f(x)} ${f(y + 7)} L${f(x - 5.5)} ${f(y)} Z" fill="url(#${id}gem)" stroke="#fff" stroke-opacity=".6" stroke-width=".6"/></g>`;
      }
    if (look.topGem && !crown)
      svg += `<g style="color:${look.topGem}"><circle class="vp-frame__pulse" cx="0" cy="-55" r="11" fill="${look.topGem}" filter="url(#${id}blur)"/>
        <path d="M0 -64 L7 -57 L4 -48 L-4 -48 L-7 -57 Z" fill="url(#${id}gem)" stroke="#fff" stroke-opacity=".7" stroke-width=".7"/>
        <path d="M-7 -57 L7 -57 M0 -64 L-2.5 -57 L0 -48 M0 -64 L2.5 -57" stroke="#fff" stroke-opacity=".45" stroke-width=".5" fill="none"/></g>`;
    for (let k = 0; k < (look.sparks ?? 0); k++) {
      const [x, y] = polar(-60 + rnd() * 120, 58);
      svg += `<circle class="vp-frame__spark" cx="${f(x)}" cy="${f(y)}" r="${f(0.8 + rnd() * 1.4)}" fill="${rnd() > 0.5 ? look.b : look.core}"
        style="--d:${f(1.6 + rnd() * 1.8)}s;--dl:${f(-rnd() * 3)}s;--dx:${(rnd() * 24 - 12).toFixed(0)}px;--dy:${(-30 - rnd() * 40).toFixed(0)}px"/>`;
    }
    for (let k = 0; k < (look.twinkles ?? 0); k++) {
      const [x, y] = polar(rnd() * 360, 55 + rnd() * 6);
      const s = 4 + rnd() * 4;
      svg += `<path class="vp-frame__twinkle" d="M${f(x)} ${f(y - s)} Q${f(x)} ${f(y)} ${f(x + s)} ${f(y)} Q${f(x)} ${f(y)} ${f(x)} ${f(y + s)} Q${f(x)} ${f(y)} ${f(x - s)} ${f(y)} Q${f(x)} ${f(y)} ${f(x)} ${f(y - s)} Z" fill="#fff" style="--d:${f(2 + rnd() * 1.6)}s;--dl:${f(-rnd() * 3)}s"/>`;
    }
    if (look.motes) {
      svg += `<g class="vp-frame__orbit" style="--d:${look.rays ? 22 : 16}s">`;
      for (let k = 0; k < look.motes; k++) {
        const [x, y] = polar((k / look.motes) * 360 + rnd() * 20, 70 + rnd() * 10);
        svg += `<circle cx="${f(x)}" cy="${f(y)}" r="${f(0.9 + rnd() * 1.1)}" fill="${look.b}" opacity="${f(0.5 + rnd() * 0.5)}"/>`;
      }
      svg += `</g>`;
    }
  } else if (look?.topGem && !crown) {
    svg += `<g style="color:${look.topGem}"><circle cx="0" cy="-54" r="6" fill="url(#${id}gem)" stroke="#fff" stroke-opacity=".6" stroke-width=".8"/></g>`;
  }
  if (crown) svg += crownMarkup(id, lite);
  svg += `</svg>`;
  html += svg;

  // Light sweeping round the rim — from bronze on.
  if (look?.sheen) {
    const delay = (-rnd() * (look.sheenDur ?? 5)).toFixed(2);
    html += `<div class="vp-frame__band vp-frame__sheen" style="--d:${look.sheenDur}s;animation-delay:${delay}s;background:conic-gradient(from 0deg, transparent 0deg 300deg, ${look.sheen}00 305deg, ${look.sheen} 335deg, ${look.sheen}00 360deg)"></div>`;
  }

  // Ribbon with the day count.
  if (look && o.ribbon) {
    const fill = look.stops ? `url(#${id}rb)` : look.facet ? look.facet[1] : look.a;
    const grad = look.stops
      ? `<defs><linearGradient id="${id}rb" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="${look.stops[2]}"/><stop offset=".5" stop-color="${look.stops[1]}"/><stop offset="1" stop-color="${look.stops[0]}"/></linearGradient></defs>`
      : '';
    const darkText = tier === 'silver' || tier === 'gold' || tier === 'stone';
    html += `<svg class="vp-frame__front" viewBox="-100 -100 200 200" aria-hidden="true">${grad}
      <path d="M-34 44 L-26 52 L-34 60 L-18 60 L-18 44 Z M34 44 L26 52 L34 60 L18 60 L18 44 Z" fill="${fill}" opacity=".75" stroke="#000" stroke-opacity=".35" stroke-width=".8"/>
      <path d="M-24 40 L24 40 L24 58 L-24 58 Z" fill="${fill}" stroke="#000" stroke-opacity=".4" stroke-width="1"/>
      <path d="M-22 42 L22 42" stroke="#fff" stroke-opacity=".45" stroke-width="1"/>
      <text x="0" y="53.5" text-anchor="middle" font-family="Quicksand, sans-serif" font-weight="700" font-size="12" fill="${darkText ? '#2a1a00' : '#fff'}">${o.ribbon}</text></svg>`;
  }
  return html;
}
