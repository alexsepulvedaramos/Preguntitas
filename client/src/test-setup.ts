// Global test setup for `ng test` (Vitest + jsdom).

// jsdom has no matchMedia; ThemeService (and the components that inject it) read the
// device colour scheme through it. Default: a light device. Specs can override it.
if (!window.matchMedia) {
  window.matchMedia = (query: string) =>
    ({
      matches: false,
      media: query,
      onchange: null,
      addEventListener: () => {},
      removeEventListener: () => {},
      addListener: () => {},
      removeListener: () => {},
      dispatchEvent: () => false,
    }) as MediaQueryList;
}
