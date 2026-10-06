// Particles for the streak celebration (rama 19): embers rising across the screen and, on
// tier-ups and milestones, a confetti burst. Lazy-imported by the celebration only, so it
// never lands in the main bundle. Kept in-house instead of canvas-confetti to avoid a new
// dependency while the client lockfile is out of sync (§13, rama 22).

interface Ember { x: number; y: number; vx: number; vy: number; r: number; color: string; phase: number }
interface Confetto { x: number; y: number; vx: number; vy: number; w: number; rot: number; spin: number; color: string; life: number }

const EXTRA_CONFETTI = ['#c6f36b', '#7bd88f', '#ffffff'];

// Runs until `isActive()` returns false or the canvas leaves the DOM.
export function playCelebrationParticles(
  canvas: HTMLCanvasElement,
  colors: string[],
  burst: boolean,
  isActive: () => boolean
): void {
  const ctx = canvas.getContext('2d');
  if (!ctx) return;
  const dpr = window.devicePixelRatio || 1;
  const W = canvas.clientWidth, H = canvas.clientHeight;
  canvas.width = W * dpr;
  canvas.height = H * dpr;
  ctx.scale(dpr, dpr);

  const embers: Ember[] = Array.from({ length: 60 }, (_, k) => ({
    x: Math.random() * W,
    y: H + Math.random() * H,
    vy: -(0.5 + Math.random() * 1.4),
    vx: (Math.random() - 0.5) * 0.4,
    r: 1 + Math.random() * 2.2,
    color: colors[k % colors.length],
    phase: Math.random() * 6,
  }));
  const palette = [...colors, ...EXTRA_CONFETTI];
  const confetti: Confetto[] = burst
    ? Array.from({ length: 160 }, (_, k) => {
        const a = Math.random() * Math.PI * 2, s = 5 + Math.random() * 9;
        return {
          x: W / 2, y: H * 0.36, vx: Math.cos(a) * s, vy: Math.sin(a) * s - 5,
          w: 6 + Math.random() * 6, rot: Math.random() * 6, spin: (Math.random() - 0.5) * 0.35,
          color: palette[k % palette.length], life: 1,
        };
      })
    : [];

  const start = performance.now();
  const frame = (now: number) => {
    if (!isActive() || !canvas.isConnected) return;
    const t = (now - start) / 1000;
    ctx.clearRect(0, 0, W, H);

    for (const p of embers) {
      p.y += p.vy;
      p.x += p.vx + Math.sin(t * 2 + p.phase) * 0.3;
      if (p.y < -10) { p.y = H + 10; p.x = Math.random() * W; }
      ctx.globalAlpha = 0.3 + 0.4 * Math.sin(t * 4 + p.phase) ** 2;
      ctx.fillStyle = p.color;
      ctx.shadowColor = p.color;
      ctx.shadowBlur = 8;
      ctx.beginPath();
      ctx.arc(p.x, p.y, p.r, 0, Math.PI * 2);
      ctx.fill();
    }

    ctx.shadowBlur = 0;
    for (const p of confetti) {
      if (p.life <= 0) continue;
      p.vx *= 0.985;
      p.vy = p.vy * 0.985 + 0.16;
      p.x += p.vx;
      p.y += p.vy;
      p.rot += p.spin;
      p.life -= 0.0045;
      ctx.globalAlpha = Math.max(0, p.life);
      ctx.save();
      ctx.translate(p.x, p.y);
      ctx.rotate(p.rot);
      ctx.fillStyle = p.color;
      ctx.fillRect(-p.w / 2, -p.w / 4, p.w, p.w / 2);
      ctx.restore();
    }
    requestAnimationFrame(frame);
  };
  requestAnimationFrame(frame);
}
