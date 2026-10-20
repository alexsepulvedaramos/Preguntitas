import { NgTemplateOutlet } from '@angular/common';
import { Component, DestroyRef, computed, effect, inject, input, signal } from '@angular/core';

import { HlmDialogImports } from '@spartan-ng/helm/dialog';
import { HlmSkeletonImports } from '@spartan-ng/helm/skeleton';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideMaximize2 } from '@ng-icons/lucide';

import { inviteLink } from '../../utils/invite-share';
import { I18nService } from '../../../../core/i18n/i18n.service';

// Modules of empty border around the code; the QR spec asks for 4 so scanners lock on.
const QUIET_ZONE = 4;

interface QrMatrix {
  size: number;
  path: string;
}

// QR code of a group's invite link (rama 28). Scanning it with the phone camera opens
// /join/{code}, which already auto-joins (rama 17). Tapping the tile shows it full screen
// so a friend can scan it straight off your phone.
@Component({
  selector: 'app-invite-qr',
  imports: [NgTemplateOutlet, HlmDialogImports, HlmSkeletonImports, NgIcon],
  providers: [provideIcons({ lucideMaximize2 })],
  template: `
    <hlm-dialog (stateChanged)="onDialogState($event)">
      <button
        hlmDialogTrigger
        type="button"
        class="group flex flex-col items-center gap-2 rounded-default p-1 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
        [attr.aria-label]="t('groups.invite.qrEnlarge')"
        (click)="$event.stopPropagation()"
      >
        <!-- QR codes must stay dark-on-light to scan, so the tile is white in both themes. -->
        <span class="relative block size-40 overflow-hidden rounded-default bg-white ring-1 ring-border shadow-sm">
          @if (qr(); as m) {
            <ng-container *ngTemplateOutlet="code; context: { $implicit: m }" />
          } @else {
            <span hlmSkeleton class="block size-full"></span>
          }
        </span>
        <!-- The enlarge cue sits under the code, never on it (it hid a corner of the QR) -->
        <span class="inline-flex items-center gap-1 text-xs text-muted-foreground group-hover:text-foreground">
          <ng-icon hlm name="lucideMaximize2" size="xs" />{{ t('groups.invite.qrHint') }}
        </span>
      </button>

      <hlm-dialog-content
        *hlmDialogPortal="let ctx"
        class="flex h-dvh w-screen max-w-none flex-col items-center justify-center gap-6 rounded-none sm:max-w-none"
      >
        <div class="flex flex-col items-center gap-1 text-center">
          <h3 hlmDialogTitle class="font-display italic text-2xl text-foreground">{{ groupName() }}</h3>
          <p hlmDialogDescription class="text-sm text-muted-foreground max-w-xs text-balance">
            {{ t('groups.invite.qrScan') }}
          </p>
        </div>

        <div class="w-full max-w-[min(85vw,60dvh)] aspect-square rounded-default bg-white p-2 shadow-lg">
          @if (qr(); as m) {
            <ng-container *ngTemplateOutlet="code; context: { $implicit: m }" />
          }
        </div>

        <p class="font-mono text-2xl font-bold tracking-[0.25em] text-foreground">{{ invitationCode() }}</p>
      </hlm-dialog-content>
    </hlm-dialog>

    <ng-template #code let-m>
      <svg
        class="block size-full"
        [attr.viewBox]="'0 0 ' + m.size + ' ' + m.size"
        shape-rendering="crispEdges"
        role="img"
        [attr.aria-label]="t('groups.invite.qrAlt', { group: groupName() })"
      >
        <path [attr.d]="m.path" fill="#000" />
      </svg>
    </ng-template>
  `,
})
export class InviteQrComponent {
  protected readonly t = inject(I18nService).t;

  readonly invitationCode = input.required<string>();
  readonly groupName = input.required<string>();

  private readonly link = computed(() => inviteLink(this.invitationCode()));
  protected readonly qr = signal<QrMatrix | null>(null);

  private wakeLock: WakeLockSentinel | null = null;

  constructor() {
    // The generator is loaded on demand so it stays out of the initial bundle.
    effect(() => {
      const link = this.link();
      import('qrcode-generator').then(({ default: qrcode }) => {
        const code = qrcode(0, 'M');
        code.addData(link);
        code.make();
        this.qr.set(toMatrix(code));
      });
    });

    inject(DestroyRef).onDestroy(() => this.releaseWakeLock());
  }

  // Keeps the screen awake while the code is shown full screen, so it doesn't dim mid-scan.
  // Browsers don't let a page raise the brightness itself.
  protected async onDialogState(state: 'open' | 'closed') {
    if (state === 'closed') {
      this.releaseWakeLock();
      return;
    }
    try {
      this.wakeLock = (await navigator.wakeLock?.request('screen')) ?? null;
    } catch {
      // Not supported or refused (e.g. low battery): the QR still works.
    }
  }

  private releaseWakeLock() {
    this.wakeLock?.release().catch(() => undefined);
    this.wakeLock = null;
  }
}

// One SVG path with a unit square per dark module, offset by the quiet zone.
function toMatrix(code: { getModuleCount(): number; isDark(r: number, c: number): boolean }): QrMatrix {
  const count = code.getModuleCount();
  let path = '';
  for (let r = 0; r < count; r++) {
    for (let c = 0; c < count; c++) {
      if (code.isDark(r, c)) path += `M${c + QUIET_ZONE} ${r + QUIET_ZONE}h1v1h-1z`;
    }
  }
  return { size: count + QUIET_ZONE * 2, path };
}
