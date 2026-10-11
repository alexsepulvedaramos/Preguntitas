// Invitation sharing shared by the group card dialog and the group-detail "invite a
// friend" prompt (rama 21).

export type InviteShareResult = 'shared' | 'cancelled' | 'copied' | 'failed';

export function inviteLink(invitationCode: string): string {
  return `${window.location.origin}/join/${invitationCode}`;
}

// Opens the device's native share sheet; falls back to copying the link where the
// Web Share API isn't available (most desktop browsers).
export async function shareInvite(groupName: string, invitationCode: string): Promise<InviteShareResult> {
  const url = inviteLink(invitationCode);
  const shareData: ShareData = {
    title: `Únete a ${groupName}`,
    text: `Únete a mi grupo «${groupName}» en Vaya Preguntita (código: ${invitationCode})`,
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
