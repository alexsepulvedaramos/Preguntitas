// Seeds a LOCAL throwaway API: N groups x M members. Writes seed.json {groups:[{id,tokens:[...]}]}
import { writeFileSync } from 'node:fs';
const BASE = 'http://localhost:5299/api';
const GROUPS = +process.env.GROUPS || 30, MEMBERS = +process.env.MEMBERS || 10;
const j = (r) => r.json();
async function post(path, body, token) {
  const r = await fetch(BASE + path, { method: 'POST', headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}) }, body: JSON.stringify(body) });
  if (!r.ok) throw new Error(`${path} ${r.status} ${await r.text()}`);
  return j(r);
}
const out = [];
for (let g = 0; g < GROUPS; g++) {
  const tokens = [];
  for (let m = 0; m < MEMBERS; m++) {
    const n = `u${g}_${m}`;
    const a = await post('/auth/register', { username: n, email: `${n}@load.test`, password: 'Loadtest1234!' });
    tokens.push(a.accessToken);
  }
  const grp = await post('/groups', { name: `Load ${g}`, description: '', dailyQuestionTime: '00:00:00' }, tokens[0]);
  for (let m = 1; m < MEMBERS; m++) await post('/groups/join', { invitationCode: grp.invitationCode }, tokens[m]);
  out.push({ id: grp.id, tokens });
  if (g % 5 === 4) console.log('groups', g + 1);
}
writeFileSync(process.env.OUT || 'seed.json', JSON.stringify({ groups: out }));
