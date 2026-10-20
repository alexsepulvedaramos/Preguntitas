import { Component, input } from '@angular/core';

// The app's coin (§17.2): the question mark of the app icon (DM Serif Display italic) in sage
// on soft gold. Sized in em, so it matches the text around it.
// Decorative by default; pass `label` when the coin alone says "coins".
@Component({
  selector: 'app-coin',
  host: {
    class: 'vp-coin',
    '[attr.role]': 'label() ? "img" : null',
    '[attr.aria-label]': 'label() || null',
    '[attr.aria-hidden]': 'label() ? null : "true"',
  },
  template: `
    <svg viewBox="0 0 24 24" width="100%" height="100%" focusable="false">
      <circle class="vp-coin__face" cx="12" cy="12" r="11.25" />
      <circle class="vp-coin__rim" cx="12" cy="12" r="9.5" />
      <path
        class="vp-coin__mark"
        d="M9.31 14.82 9.47 11.18Q11.52 10.82 12.67 9.87Q13.82 8.92 13.82 7.61Q13.82 6.47 13.42 5.94Q13.03 5.4 12.4 5.4Q11.25 5.4 10.87 6.84L10.79 7.24Q10.65 7.81 10.34 8.15Q10.04 8.49 9.45 8.49Q8.95 8.49 8.65 8.16Q8.34 7.83 8.34 7.3Q8.34 6.64 8.87 6.11Q9.39 5.59 10.28 5.29Q11.17 5 12.22 5Q14.04 5 15.14 5.74Q16.24 6.47 16.24 8.21Q16.24 9.53 15.45 10.46Q14.67 11.4 13.29 11.99Q11.92 12.58 10.16 12.86L9.72 14.82ZM9.17 19Q8.55 19 8.15 18.61Q7.76 18.21 7.76 17.65Q7.76 16.96 8.26 16.48Q8.77 16.01 9.39 16.01Q10.02 16.01 10.4 16.41Q10.79 16.82 10.79 17.38Q10.79 18.05 10.29 18.53Q9.8 19 9.17 19Z"
      />
    </svg>
  `,
})
export class CoinComponent {
  readonly label = input<string>('');
}
