// DiceBear styles drawn as black linework on a transparent canvas: on the dark theme's
// card/muted surfaces they all but disappear, so they always get a light backing.
const LINEWORK_STYLES = ['croodles', 'croodles-neutral'];

export function needsLightBackdrop(avatarUrl: string | null | undefined): boolean {
  if (!avatarUrl) return false;
  const match = /api\.dicebear\.com\/[^/]+\/([^/]+)\//.exec(avatarUrl);
  return !!match && LINEWORK_STYLES.includes(match[1]);
}
