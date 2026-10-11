import { TestBed } from '@angular/core/testing';

import { ThemeService } from './theme.service';

// A device colour scheme the test controls, including live "change" events.
function fakeDeviceScheme(dark: boolean) {
  const listeners: ((event: { matches: boolean }) => void)[] = [];
  vi.spyOn(window, 'matchMedia').mockImplementation(
    query =>
      ({
        matches: dark,
        media: query,
        addEventListener: (_: string, listener: (event: { matches: boolean }) => void) => listeners.push(listener),
        removeEventListener: () => {},
      }) as unknown as MediaQueryList,
  );
  return { change: (isDark: boolean) => listeners.forEach(listener => listener({ matches: isDark })) };
}

describe('ThemeService', () => {
  const html = document.documentElement;

  beforeEach(() => localStorage.clear());

  afterEach(() => {
    vi.restoreAllMocks();
    html.classList.remove('dark');
  });

  it('follows the device when nothing is stored', () => {
    fakeDeviceScheme(true);
    const service = TestBed.inject(ThemeService);
    TestBed.tick();

    expect(service.preference()).toBe('system');
    expect(service.darkMode()).toBe(true);
    expect(html.classList.contains('dark')).toBe(true);
  });

  it('reacts live when the device switches scheme under "system"', () => {
    const device = fakeDeviceScheme(false);
    const service = TestBed.inject(ThemeService);

    device.change(true);
    TestBed.tick();

    expect(service.darkMode()).toBe(true);
    expect(html.classList.contains('dark')).toBe(true);
  });

  it('keeps a stored choice over the device and persists a new one', () => {
    localStorage.setItem('theme', 'light');
    fakeDeviceScheme(true);
    const service = TestBed.inject(ThemeService);
    expect(service.darkMode()).toBe(false);

    service.setPreference('dark');
    TestBed.tick();

    expect(localStorage.getItem('theme')).toBe('dark');
    expect(html.classList.contains('dark')).toBe(true);
  });

  it('forgets the stored value when switching back to "system"', () => {
    localStorage.setItem('theme', 'dark');
    fakeDeviceScheme(false);
    const service = TestBed.inject(ThemeService);

    service.setPreference('system');
    TestBed.tick();

    expect(localStorage.getItem('theme')).toBeNull();
    expect(html.classList.contains('dark')).toBe(false);
  });

  it('toggles to the opposite of what is shown right now', () => {
    fakeDeviceScheme(true);
    const service = TestBed.inject(ThemeService);

    service.toggleTheme();

    expect(service.preference()).toBe('light');
    expect(service.darkMode()).toBe(false);
  });
});
