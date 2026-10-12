import { browserLang, isLang } from './languages';

describe('browserLang', () => {
  const prefer = (...languages: string[]) => vi.spyOn(navigator, 'languages', 'get').mockReturnValue(languages);

  afterEach(() => vi.restoreAllMocks());

  it('matches a regional tag by its primary subtag', () => {
    prefer('de-AT', 'en');
    expect(browserLang()).toBe('de');
  });

  it('takes the first supported language in the preference list', () => {
    prefer('fr-FR', 'it', 'en-GB');
    expect(browserLang()).toBe('en');
  });

  it('falls back to Spanish when none is supported', () => {
    prefer('fr', 'pt-BR');
    expect(browserLang()).toBe('es');
  });
});

describe('isLang', () => {
  it('accepts only the supported codes', () => {
    expect(isLang('es')).toBe(true);
    expect(isLang('en')).toBe(true);
    expect(isLang('de')).toBe(true);
    expect(isLang('en-GB')).toBe(false);
    expect(isLang(null)).toBe(false);
  });
});
