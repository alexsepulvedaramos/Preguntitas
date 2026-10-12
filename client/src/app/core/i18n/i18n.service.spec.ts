import { TestBed } from '@angular/core/testing';

import { I18nService } from './i18n.service';
import { TranslationKey } from './translation.types';

describe('I18nService', () => {
  let service: I18nService;

  beforeEach(async () => {
    localStorage.clear();
    TestBed.configureTestingModule({});
    service = TestBed.inject(I18nService);
    await service.use('es', false);
  });

  it('translates a key', () => {
    expect(service.t('common.close')).toBe('Cerrar');
  });

  it('fills placeholders', () => {
    expect(service.t('groups.card.at', { time: '21:00' })).toBe('A las 21:00');
  });

  it('picks the plural form for the language', async () => {
    expect(service.t('common.days', { count: 1 })).toBe('1 día');
    expect(service.t('common.days', { count: 0 })).toBe('0 días');
    expect(service.t('common.days', { count: 3 })).toBe('3 días');

    await service.use('de', false);
    expect(service.t('common.days', { count: 1 })).toBe('1 Tag');
    expect(service.t('common.days', { count: 2 })).toBe('2 Tage');
  });

  it('switches language, updates <html lang> and remembers the choice', async () => {
    await service.use('en');

    expect(service.lang()).toBe('en');
    expect(service.t('common.close')).toBe('Close');
    expect(document.documentElement.lang).toBe('en');
    expect(localStorage.getItem('vp_lang')).toBe('en');
  });

  it('does not remember a language it was not asked to', async () => {
    await service.use('de', false);
    expect(localStorage.getItem('vp_lang')).toBeNull();
  });

  it('keeps the last requested language when switches overlap', async () => {
    const first = service.use('de');
    const second = service.use('en');
    await Promise.all([first, second]);

    expect(service.lang()).toBe('en');
    expect(service.t('common.close')).toBe('Close');
  });

  it('starts with the stored language over the browser one', async () => {
    localStorage.setItem('vp_lang', 'de');
    await service.init();
    expect(service.lang()).toBe('de');
  });

  it('escapes parameters in tHtml but keeps the entry markup', () => {
    expect(service.tHtml('groups.invite.title', { group: '<b>x</b>' })).toBe('Invitar a &lt;b&gt;x&lt;/b&gt;');
    expect(service.tHtml('pwa.iosStep3')).toContain('<strong>');
  });

  it('falls back to the key when it is missing', () => {
    expect(service.t('nope.missing' as TranslationKey)).toBe('nope.missing');
  });

  it('formats dates in the UI language', async () => {
    const date = new Date(2026, 9, 12);
    expect(service.formatDate(date, { month: 'long' })).toBe('octubre');
    await service.use('de', false);
    expect(service.formatDate(date, { month: 'long' })).toBe('Oktober');
  });
});
