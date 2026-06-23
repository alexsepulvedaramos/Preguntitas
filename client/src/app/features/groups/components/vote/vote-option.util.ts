// Shared class toggle for the option-picker voting components (superlative, deathmatch,
// secret-pairing, custom-poll). Uses the design system's dedicated `.vp-poll-option` /
// `.vp-poll-option-selected` classes (styles.css) rather than the generic hlmBtn outline
// variant, which is too low-contrast against the card background in dark mode.
export function voteOptionClass(selected: boolean): string {
  return selected ? 'vp-poll-option vp-poll-option-selected' : 'vp-poll-option';
}
