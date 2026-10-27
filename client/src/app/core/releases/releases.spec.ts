import ngswConfig from '../../../../ngsw-config.json';
import packageJson from '../../../../package.json';
import { APP_VERSION, LATEST_GAME_RELEASE, RELEASES, compareVersions, hasGameItems, isUpdateWorthPrompt, updateAppData } from './releases';

// Guards the release checklist (spec §14 "Releases"): the newest entry is the app version
// and must match package.json.
describe('releases', () => {
  it('the app version is the one in package.json', () => {
    expect(APP_VERSION).toBe(packageJson.version);
  });

  it('lists releases newest first, each version once', () => {
    for (let i = 0; i < RELEASES.length - 1; i++) {
      const [newer, older] = [RELEASES[i], RELEASES[i + 1]];
      expect(compareVersions(newer.version, older.version), `${newer.version} after ${older.version}`).toBeGreaterThan(0);
      expect(newer.date >= older.date, `${newer.version} dated before ${older.version}`).toBe(true);
    }
  });

  it('gives every release a semver version, an ISO date and at least one note', () => {
    for (const release of RELEASES) {
      expect(release.version).toMatch(/^\d+\.\d+\.\d+$/);
      expect(release.date).toMatch(/^\d{4}-\d{2}-\d{2}$/);
      expect(release.items.length, release.version).toBeGreaterThan(0);
    }
  });

  it('marks the kind of every item from 1.32.2 on', () => {
    for (const release of RELEASES.filter(r => compareVersions(r.version, '1.32.2') >= 0)) {
      for (const item of release.items) expect(item.kind, release.version).toBeDefined();
    }
  });

  it('announces the newest release that has a game item', () => {
    expect(hasGameItems(LATEST_GAME_RELEASE)).toBe(true);
    expect(RELEASES.slice(0, RELEASES.indexOf(LATEST_GAME_RELEASE)).some(hasGameItems)).toBe(false);
  });

  it('keeps appData in ngsw-config.json in sync with the releases', () => {
    expect(ngswConfig.appData).toEqual(updateAppData(RELEASES));
  });

  it('prompts to update only for game or critical releases newer than the running one', () => {
    const data = { version: '1.34.1', lastGameVersion: '1.33.0', lastCriticalVersion: null };
    expect(isUpdateWorthPrompt(data, '1.32.3')).toBe(true);
    expect(isUpdateWorthPrompt(data, '1.33.0')).toBe(false);
    expect(isUpdateWorthPrompt({ ...data, lastCriticalVersion: '1.34.0' }, '1.33.0')).toBe(true);
    expect(isUpdateWorthPrompt(undefined, '1.33.0')).toBe(true);
  });

  it('compares versions numerically, not as text', () => {
    expect(compareVersions('1.10.0', '1.9.1')).toBeGreaterThan(0);
    expect(compareVersions('1.9.1', '1.9.10')).toBeLessThan(0);
    expect(compareVersions('2.0.0', '2.0.0')).toBe(0);
  });
});
