import { Provider, inject } from '@angular/core';
import { BrnCalendarI18nService, BrnCalendarI18nToken } from '@spartan-ng/brain/calendar';

import { I18nService } from './i18n.service';

// 1 Jan 2023 was a Sunday: index 0 → Sunday, matching the calendar's weekday indexes.
const weekdayDate = (index: number) => new Date(2023, 0, 1 + index);

// Spartan's calendar (the history date picker) in the UI language. Every function reads the
// current locale when called, so the calendar re-renders with it when the language changes.
// Weeks start on Monday, as they do in all three languages' countries.
export function provideCalendarI18n(): Provider {
  return {
    provide: BrnCalendarI18nToken,
    useFactory: () => {
      const i18n = inject(I18nService);
      const locale = () => i18n.locale();
      const service = new BrnCalendarI18nService();
      service.use({
        formatWeekdayName: (index) => weekdayDate(index).toLocaleDateString(locale(), { weekday: 'short' }),
        labelWeekday: (index) => weekdayDate(index).toLocaleDateString(locale(), { weekday: 'long' }),
        months: () => {
          const name = (month: number) => new Date(2000, month).toLocaleDateString(locale(), { month: 'short' });
          return [name(0), name(1), name(2), name(3), name(4), name(5), name(6), name(7), name(8), name(9), name(10), name(11)];
        },
        formatHeader: (month, year) =>
          new Date(year, month).toLocaleDateString(locale(), { month: 'long', year: 'numeric' }),
        formatMonth: (month) => new Date(2000, month).toLocaleDateString(locale(), { month: 'short' }),
        formatYear: (year) => new Date(year, 0).toLocaleDateString(locale(), { year: 'numeric' }),
        labelPrevious: () => i18n.t('calendar.previous'),
        labelNext: () => i18n.t('calendar.next'),
        firstDayOfWeek: () => 1,
      });
      return service;
    },
  };
}
