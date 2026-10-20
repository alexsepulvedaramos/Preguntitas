import {
  Component,
  OnInit,
  computed,
  inject,
  input,
  signal,
} from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { toast } from '@spartan-ng/brain/sonner';

import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmInputImports } from '@spartan-ng/helm/input';
import { HlmLabelImports } from '@spartan-ng/helm/label';
import { HlmSkeletonImports } from '@spartan-ng/helm/skeleton';
import { HlmSpinnerImports } from '@spartan-ng/helm/spinner';
import { HlmAlertDialogImports } from '@spartan-ng/helm/alert-dialog';
import { HlmDialogImports } from '@spartan-ng/helm/dialog';
import { NgIcon, provideIcons } from '@ng-icons/core';
import {
  lucideArrowLeft,
  lucideChevronRight,
  lucideCopy,
  lucideRefreshCw,
  lucideShield,
  lucideUserMinus,
} from '@ng-icons/lucide';

import { GroupsService } from '../../services/groups.service';
import { GroupMember, GroupResponse, UpdateGroupRequest } from '../../models/group.models';
import { UserAvatarComponent } from '../../../../shared/components/user-avatar/user-avatar.component';
import { HlmSwitchImports } from '@spartan-ng/helm/switch';
import { HlmBadgeImports } from '@spartan-ng/helm/badge';
import { PacksService } from '../../../../core/services/packs.service';
import { MemberCardService } from '../../../../core/services/member-card.service';
import { GroupStreaksService } from '../../services/group-streaks.service';
import { Pack } from '../../../../core/models/pack.model';
import { MobileTopBarDirective } from '../../../../shared/directives/mobile-top-bar.directive';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { TranslationKey } from '../../../../core/i18n/translation.types';
import { apiErrorMessage } from '../../../../shared/utils/api-error';
import { InviteQrComponent } from '../invite-qr/invite-qr.component';
import { UserNameComponent } from '../../../../shared/components/user-name/user-name.component';
import { GlyphComponent } from '../../../../shared/components/glyph/glyph.component';

@Component({
  selector: 'app-group-settings',
  imports: [
    GlyphComponent,
    UserNameComponent,
    RouterLink,
    MobileTopBarDirective,
    FormsModule,
    HlmButtonImports,
    HlmInputImports,
    HlmLabelImports,
    HlmSkeletonImports,
    HlmSpinnerImports,
    HlmAlertDialogImports,
    HlmDialogImports,
    HlmSwitchImports,
    HlmBadgeImports,
    NgIcon,
    UserAvatarComponent,
    InviteQrComponent,
  ],
  providers: [
    provideIcons({
      lucideArrowLeft,
      lucideChevronRight,
      lucideCopy,
      lucideRefreshCw,
      lucideShield,
      lucideUserMinus,
    }),
  ],
  templateUrl: './group-settings.component.html',
})
export class GroupSettingsComponent implements OnInit {
  private readonly groupsService = inject(GroupsService);
  private readonly packsService = inject(PacksService);
  private readonly groupStreaks = inject(GroupStreaksService);
  protected readonly memberCard = inject(MemberCardService);
  private readonly router = inject(Router);
  protected readonly t = inject(I18nService).t;

  public readonly groupId = input.required<string>();
  public readonly numericGroupId = computed(() => Number(this.groupId()));

  protected readonly group = signal<GroupResponse | null>(null);
  protected readonly members = signal<GroupMember[]>([]);
  protected readonly packs = signal<Pack[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<TranslationKey | null>(null);

  // Edit form state
  protected readonly editName = signal('');
  protected readonly editDescription = signal('');
  protected readonly editTime = signal('');
  protected readonly saving = signal(false);

  // Admin actions state
  protected readonly kickingId = signal<number | null>(null);
  protected readonly transferringId = signal<number | null>(null);
  protected readonly regenerating = signal(false);
  protected readonly leaving = signal(false);
  protected readonly notifMuted = signal(false);

  protected readonly isAdmin = computed(() =>
    this.members().find((m) => m.isCurrentUser)?.isAdmin ?? false
  );
  protected readonly enabledPacksCount = computed(
    () => this.packs().filter((p) => p.enabled).length
  );

  ngOnInit() {
    this.loadAll();
  }

  protected initials(username: string): string {
    return username.slice(0, 2).toUpperCase();
  }

  protected saveInfo() {
    if (this.saving()) return;
    this.saving.set(true);

    const req: UpdateGroupRequest = {
      name: this.editName(),
      description: this.editDescription(),
      dailyQuestionTime: this.editTime(),
    };

    this.groupsService.updateGroup(this.numericGroupId(), req).subscribe({
      next: (updated) => {
        this.group.set(updated);
        this.saving.set(false);
        if (updated.dailyTimeChangeAppliesFromTomorrow) {
          toast.success(this.t('settings.updatedFromTomorrow'));
        } else {
          toast.success(this.t('settings.updated'));
        }
      },
      error: () => {
        this.saving.set(false);
        toast.error(this.t('settings.updateFailed'));
      },
    });
  }

  protected copyCode() {
    const code = this.group()?.invitationCode;
    if (!code) return;
    navigator.clipboard.writeText(code).then(() => {
      toast.success(this.t('settings.codeCopied'));
    });
  }

  protected regenerateCode() {
    if (this.regenerating()) return;
    this.regenerating.set(true);

    this.groupsService.regenerateInviteCode(this.numericGroupId()).subscribe({
      next: (updated) => {
        this.group.set(updated);
        this.regenerating.set(false);
        toast.success(this.t('settings.codeRegenerated'));
      },
      error: () => {
        this.regenerating.set(false);
        toast.error(this.t('settings.regenerateFailed'));
      },
    });
  }

  protected kickMember(member: GroupMember) {
    if (this.kickingId()) return;
    this.kickingId.set(member.id);

    this.groupsService.kickMember(this.numericGroupId(), member.id).subscribe({
      next: () => {
        this.members.update((list) => list.filter((m) => m.id !== member.id));
        this.kickingId.set(null);
        toast.success(this.t('settings.kicked', { name: member.username }));
      },
      error: () => {
        this.kickingId.set(null);
        toast.error(this.t('settings.kickFailed'));
      },
    });
  }

  protected transferAdmin(member: GroupMember) {
    if (this.transferringId()) return;
    this.transferringId.set(member.id);

    this.groupsService
      .transferAdmin(this.numericGroupId(), { newAdminId: member.id })
      .subscribe({
        next: () => {
          this.members.update((list) =>
            list.map((m) => ({ ...m, isAdmin: m.id === member.id }))
          );
          this.transferringId.set(null);
          toast.success(this.t('settings.transferred', { name: member.username }));
        },
        error: () => {
          this.transferringId.set(null);
          toast.error(this.t('settings.transferFailed'));
        },
      });
  }

  protected toggleMute() {
    const muted = !this.notifMuted();
    this.notifMuted.set(muted);
    this.groupsService.setGroupMute(this.numericGroupId(), muted).subscribe({
      error: () => {
        this.notifMuted.set(!muted);
        toast.error(this.t('settings.muteFailed'));
      },
    });
  }

  protected togglePack(pack: Pack) {
    const enabled = !pack.enabled;
    this.packs.update((list) =>
      list.map((p) => (p.id === pack.id ? { ...p, enabled } : p))
    );

    this.packsService.setPackEnabled(this.numericGroupId(), pack.id, enabled).subscribe({
      error: (err) => {
        this.packs.update((list) =>
          list.map((p) => (p.id === pack.id ? { ...p, enabled: !enabled } : p))
        );
        toast.error(apiErrorMessage(err, this.t('settings.packFailed')));
      },
    });
  }

  protected leaveGroup() {
    if (this.leaving()) return;
    this.leaving.set(true);

    this.groupsService.leaveGroup(this.numericGroupId()).subscribe({
      next: () => {
        this.leaving.set(false);
        this.router.navigate(['/groups']);
      },
      error: () => {
        this.leaving.set(false);
        toast.error(this.t('settings.leaveFailed'));
      },
    });
  }

  private loadAll() {
    this.loading.set(true);
    this.error.set(null);

    this.groupsService.getGroup(this.numericGroupId()).subscribe({
      next: (group) => {
        this.group.set(group);
        this.editName.set(group.name);
        this.editDescription.set(group.description);
        this.editTime.set(group.dailyQuestionTime);

        this.groupsService.getGroupMembers(this.numericGroupId()).subscribe({
          next: (members) => {
            this.members.set(members);
            this.groupStreaks.set(this.numericGroupId(), members);
            const me = members.find(m => m.isCurrentUser);
            if (me) this.notifMuted.set(me.notificationsMuted);
            this.loading.set(false);
          },
          error: () => {
            this.error.set('settings.membersLoadError');
            this.loading.set(false);
          },
        });

        this.packsService.getPacks(this.numericGroupId()).subscribe({
          next: (packs) => this.packs.set(packs),
          error: () => {
            // Non-essential for the page's main load — the Packs section just stays empty.
          },
        });
      },
      error: () => {
        this.error.set('settings.groupLoadError');
        this.loading.set(false);
      },
    });
  }
}
