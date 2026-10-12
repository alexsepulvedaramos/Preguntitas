// Open-loop load test against a LOCAL throwaway API. Usage: node load.mjs vote | node load.mjs ramp 10,25,50,100 [secs]
import { readFileSync } from 'node:fs';
const BASE = process.env.BASE || 'http://localhost:5299/api';
const seed = JSON.parse(readFileSync(new URL('./seed.json', import.meta.url)));
const users = seed.groups.flatMap((g) => g.tokens.map((t) => ({ gid: g.id, t })));
const H = (t) => ({ Authorization: `Bearer ${t}`, 'Content-Type': 'application/json' });

function pct(a, p) { if (!a.length) return 0; const s = [...a].sort((x, y) => x - y); return s[Math.min(s.length - 1, Math.floor(s.length * p))]; }
async function timed(name, stats, fn) {
  const t0 = performance.now();
  try { const r = await fn(); const ms = performance.now() - t0; (stats[name] ??= { lat: [], err: 0, bytes: 0 }).lat.push(ms); if (!r.ok) stats[name].err++; stats[name].bytes += +(r.headers.get('content-length') ?? 0) || (await r.text()).length; }
  catch { (stats[name] ??= { lat: [], err: 0, bytes: 0 }).err++; }
}
function report(label, stats, secs) {
  console.log(`\n== ${label} ==`);
  for (const [k, v] of Object.entries(stats))
    console.log(`${k.padEnd(14)} n=${String(v.lat.length).padStart(6)} err=${String(v.err).padStart(5)} p50=${pct(v.lat, .5).toFixed(0)}ms p95=${pct(v.lat, .95).toFixed(0)}ms p99=${pct(v.lat, .99).toFixed(0)}ms avgBody=${(v.bytes / Math.max(1, v.lat.length)).toFixed(0)}B`);
  const total = Object.values(stats).reduce((a, v) => a + v.lat.length, 0);
  console.log(`achieved ${(total / secs).toFixed(1)} req/s`);
}

async function vote() {
  const stats = {}; let okc = 0, bad = {};
  const members = {};
  for (const g of seed.groups) { const r = await fetch(`${BASE}/groups/${g.id}/members`, { headers: H(g.tokens[0]) }); members[g.id] = (await r.json()).map((m) => m.id); }
  await Promise.all(users.map(async (u, i) => {
    const cur = await (await fetch(`${BASE}/groups/${u.gid}/daily/current`, { headers: H(u.t) })).json();
    const q = cur.today?.question; if (!q) return;
    const mem = members[u.gid];
    const body = { selectedOptionIds: q.options.length ? [q.options[i % q.options.length].id] : undefined, numericValue: 1 + (i % 10), freeText: 'respuesta de carga', selectedTargetUserId: mem[i % mem.length], selectedTargetUserIds: [mem[i % mem.length], mem[(i + 1) % mem.length], mem[(i + 2) % mem.length]] };
    const t0 = performance.now();
    const r = await fetch(`${BASE}/groups/${u.gid}/daily/vote`, { method: 'POST', headers: H(u.t), body: JSON.stringify(body) });
    (stats.vote ??= { lat: [], err: 0, bytes: 0 }).lat.push(performance.now() - t0);
    if (r.ok) okc++; else { stats.vote.err++; const k = `${q.type}:${r.status}`; bad[k] = (bad[k] ?? 0) + 1; }
  }));
  report('vote burst (300 users at once)', stats, 1);
  console.log('ok votes', okc, 'failures by type:status', bad);
}

async function ramp(rates, secs) {
  for (const rps of rates) {
    const stats = {}; let inflight = 0, dropped = 0;
    const end = performance.now() + secs * 1000; let n = 0;
    const start = performance.now();
    while (performance.now() < end) {
      const due = Math.floor(((performance.now() - start) / 1000) * rps);
      while (n < due) {
        n++;
        if (inflight > 400) { dropped++; continue; }
        const u = users[Math.floor(Math.random() * users.length)];
        // real polling mix: chat every 15s, daily every 30s => 2:1
        const isChat = Math.random() < 2 / 3;
        inflight++;
        timed(isChat ? 'GET chat' : 'GET daily/cur', stats, () => fetch(`${BASE}/groups/${u.gid}/daily${isChat ? '/chat' : '/current'}`, { headers: H(u.t) })).finally(() => inflight--);
      }
      await new Promise((r) => setTimeout(r, 5));
    }
    while (inflight > 0) await new Promise((r) => setTimeout(r, 20));
    report(`${rps} req/s target (~${rps * 10} visible players) dropped=${dropped}`, stats, secs);
  }
}
const [cmd, a, b] = process.argv.slice(2);
if (cmd === 'vote') await vote(); else await ramp((a ?? '10,25,50').split(',').map(Number), +(b ?? 20));
