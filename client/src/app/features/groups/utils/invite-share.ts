import { I18nService } from '../../../core/i18n/i18n.service';

// Invitation sharing shared by the group card dialog and the group-detail "invite a
// friend" prompt (rama 21).

export type InviteShareResult = 'shared' | 'cancelled' | 'copied' | 'failed';

export function inviteLink(invitationCode: string): string {
  return `${window.location.origin}/join/${invitationCode}`;
}

// Opens the device's native share sheet; falls back to copying the link where the
// Web Share API isn't available (most desktop browsers).
// The message goes out in the sharer's UI language.
export async function shareInvite(
  groupName: string,
  invitationCode: string,
  t: I18nService['t'],
): Promise<InviteShareResult> {
  const url = inviteLink(invitationCode);
  const shareData: ShareData = {
    title: t('groups.invite.shareTitle', { group: groupName }),
    text: t('groups.invite.shareText', { group: groupName, code: invitationCode }),
    url,
  };

  if (navigator.share && navigator.canShare?.(shareData)) {
    try {
      await navigator.share(shareData);
      return 'shared';
    } catch (err) {
      // The user closing the share sheet isn't an error worth reporting.
      if (err instanceof DOMException && err.name === 'AbortError') return 'cancelled';
      console.error('Error executing native share: ', err);
    }
  }

  try {
    await navigator.clipboard.writeText(url);
    return 'copied';
  } catch (err) {
    console.error('Failed to copy link: ', err);
    return 'failed';
  }
}
