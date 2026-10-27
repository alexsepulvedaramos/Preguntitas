import { RELEASES } from '../../releases/releases';
import { LANGUAGES } from '../languages';
import { de } from './de';
import { en } from './en';
import { es } from './es';

// The types already force en/de to have es's keys; these checks cover what types can't:
// empty texts and placeholders that went missing (or got renamed) in a translation.

type Dictionary = Record<string, unknown>;

function flatten(node: Dictionary, prefix = ''): Map<string, string> {
  const entries = new Map<string, string>();
  for (const [key, value] of Object.entries(node)) {
    const path = prefix ? `${prefix}.${key}` : key;
    if (typeof value === 'string') entries.set(path, value);
    else for (const [k, v] of flatten(value as Dictionary, path)) entries.set(k, v);
  }
  return entries;
}

const placeholders = (text: string) => [...text.matchAll(/\{(\w+)\}/g)].map((m) => m[1]).sort();

const source = flatten(es);

describe.each([
  ['en', en],
  ['de', de],
])('%s dictionary', (_, dictionary) => {
  const target = flatten(dictionary as unknown as Dictionary);

  it('has exactly the Spanish keys', () => {
    expect([...target.keys()].sort()).toEqual([...source.keys()].sort());
  });

  it('keeps every placeholder of the Spanish text', () => {
    for (const [key, text] of source) {
      expect(placeholders(target.get(key) ?? ''), key).toEqual(placeholders(text));
    }
  });

  it('has no empty texts', () => {
    for (const [key, text] of target) expect(text.trim(), key).not.toBe('');
  });
});

describe('release notes', () => {
  it('are written in every language', () => {
    for (const release of RELEASES) {
      const texts = [release.title, ...release.items.flatMap((item) => 'title' in item ? [item.title, item.text] : [item.text])];
      for (const text of texts) {
        for (const lang of LANGUAGES) expect(text[lang]?.trim(), `${release.version} ${lang}`).toBeTruthy();
      }
    }
  });

  it('are listed newest first', () => {
    const versions = RELEASES.map((r) => r.version.split('.').map(Number));
    for (let i = 1; i < versions.length; i++) {
      const [a, b] = [versions[i - 1], versions[i]];
      const newer = a[0] - b[0] || a[1] - b[1] || a[2] - b[2];
      expect(newer, RELEASES[i].version).toBeGreaterThan(0);
    }
  });
});
