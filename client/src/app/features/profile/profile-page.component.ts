import {
  Component, inject, signal, computed, OnInit
} from '@angular/core';
import { Router } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { RouterLink } from '@angular/router';
import {
  lucideArrowLeft, lucideChartColumn, lucideCheck, lucideEye, lucideEyeOff, lucideFlame, lucideBan, lucideShoppingBag
} from '@ng-icons/lucide';

import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmInputImports } from '@spartan-ng/helm/input';
import { HlmLabelImports } from '@spartan-ng/helm/label';
import { HlmDrawerImports } from '@spartan-ng/helm/drawer';
import { HlmSeparatorImports } from '@spartan-ng/helm/separator';
import { HlmSwitchImports } from '@spartan-ng/helm/switch';
import { HlmToggleGroupImports } from '@spartan-ng/helm/toggle-group';
import { toast } from '@spartan-ng/brain/sonner';

import { AuthService } from '../../core/auth/auth.service';
import { UserService } from '../../core/services/user.service';
import { UserAvatarComponent } from '../../shared/components/user-avatar/user-avatar.component';
import { AvatarPickerComponent } from './components/avatar-picker/avatar-picker.component';
import { PushNotificationService } from '../../core/services/push-notification.service';
import {
  NotificationPreferencesService,
  NotificationPreferences,
  STREAK_DANGER_HOURS,
} from '../../core/services/notification-preferences.service';
import { MemberCardService } from '../../core/services/member-card.service';
import {
  FRAME_NONE, FRAME_STREAK, frameColorVar, isColorFrame, STREAK_MATERIAL_KEYS, TITLE_AUTO, TITLE_NONE, frameFollowsStreak, streakTierFor } from '../../core/constants/streak-tiers';
import { COSMETICS, CosmeticKind, frameColorItemKey } from '../../core/constants/shop-catalog';
import { UserProfileDto } from '../../core/services/user.service';
import { I18nService } from '../../core/i18n/i18n.service';
import { TranslationKey } from '../../core/i18n/translation.types';
import { LanguagePickerComponent } from '../../shared/components/language-picker/language-picker.component';
import { apiErrorMessage } from '../../shared/utils/api-error';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { NamePlateComponent } from '../../shared/components/name-plate/name-plate.component';
import { plateTextClass } from '../../shared/components/name-plate/plate-art';
import { PlatesDisplayService } from '../../core/services/plates-display.service';

const FRAME_COLORS: { label: TranslationKey; value: string }[] = [
  { label: 'profile.frameColors.streak', value: FRAME_STREAK },
  { label: 'profile.frameColors.purple', value: 'purple' },
  { label: 'profile.frameColors.blue', value: 'blue' },
  { label: 'profile.frameColors.green', value: 'green' },
  { label: 'profile.frameColors.yellow', value: 'yellow' },
  { label: 'profile.frameColors.orange', value: 'orange' },
  { label: 'profile.frameColors.red', value: 'red' },
  { label: 'profile.frameColors.pink', value: 'pink' },
  { label: 'profile.frameColors.none', value: FRAME_NONE },
];

@Component({
  selector: 'app-profile-page',
  standalone: true,
  imports: [PageHeaderComponent, RouterLink,
    ReactiveFormsModule,
    HlmButtonImports,
    HlmInputImports,
    HlmLabelImports,
    HlmDrawerImports,
    HlmSeparatorImports,
    HlmSwitchImports,
    HlmToggleGroupImports,
    NgIcon,
    UserAvatarComponent,
    AvatarPickerComponent,
    LanguagePickerComponent,
    NamePlateComponent,
  ],
  providers: [provideIcons({ lucideArrowLeft, lucideChartColumn, lucideCheck, lucideEye, lucideEyeOff, lucideFlame, lucideBan, lucideShoppingBag })],
  templateUrl: './profile-page.component.html',
})
export class ProfilePageComponent implements OnInit {
  protected readonly authService = inject(AuthService);
  private readonly userService = inject(UserService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  protected readonly pushService = inject(PushNotificationService);
  private readonly prefsService = inject(NotificationPreferencesService);
  private readonly memberCard = inject(MemberCardService);
  protected readonly t = inject(I18nService).t;

  // The streak ring, no frame and the frame colours you own (bought in the shop since rama 57).
  protected readonly frameColors = computed(() => {
    const owned = new Set(this.user()?.ownedCosmetics ?? []);
    return FRAME_COLORS.filter((c) => !isColorFrame(c.value) || owned.has(frameColorItemKey(c.value)));
  });
  // The ring finishes you own (rama 57), worn over the streak ring.
  protected readonly ownedFinishes = computed(() => {
    const owned = new Set(this.user()?.ownedCosmetics ?? []);
    return COSMETICS.filter((c) => c.kind === CosmeticKind.RingSkin && owned.has(c.key)).map((c) => ({
      key: c.key,
      name: this.t(c.name),
    }));
  });
  // The finish worn now, if the frame follows the streak.
  protected readonly wornFinish = computed(() =>
    frameFollowsStreak(this.user()?.frameColor) ? (this.user()?.look?.ringSkin ?? null) : null
  );
  protected readonly wornFinishName = computed(() => this.ownedFinishes().find((f) => f.key === this.wornFinish())?.name ?? null);

  // Plates (rama 60): the owned ones, worn independently of the ring.
  protected readonly ownedPlates = computed(() => {
    const owned = new Set(this.user()?.ownedCosmetics ?? []);
    return COSMETICS.filter((c) => c.kind === CosmeticKind.Plate && owned.has(c.key)).map((c) => ({
      key: c.key,
      name: this.t(c.name),
      text: plateTextClass(c.key),
    }));
  });
  protected readonly wornPlate = computed(() => this.user()?.look?.plate ?? null);
  protected readonly isSavingPlate = signal(false);
  // Temporary switch to compare «featured places only» with «everywhere» (§17.18).
  protected readonly platesDisplay = inject(PlatesDisplayService);
  protected readonly frameNone = FRAME_NONE;
  protected readonly swatch = (value: string) => (isColorFrame(value) ? frameColorVar(value) : null);
  protected readonly frameStreak = FRAME_STREAK;
  protected readonly streakDangerHours = STREAK_DANGER_HOURS;
  protected readonly isSavingProfile = signal(false);
  protected readonly isSavingPassword = signal(false);
  protected readonly isSavingFrame = signal(false);
  protected readonly profileSuccess = signal(false);
  protected readonly passwordSuccess = signal(false);
  protected readonly profileError = signal<string | null>(null);
  protected readonly passwordError = signal<string | null>(null);
  protected readonly showCurrentPassword = signal(false);
  protected readonly showNewPassword = signal(false);
  protected readonly showConfirmPassword = signal(false);

  protected readonly notifPrefs = signal<NotificationPreferences>({
    newQuestion: true, selectorTurn: true, userVoted: true, newMessage: true,
    streakDanger: true, streakDangerHoursBefore: 3, raceToSelect: true, flashQuestion: true, monthlySummary: true,
    nudge: true, reaction: true, shopNews: true,
  });

  protected readonly user = computed(() => this.authService.currentUser());

  // null is treated as "streak" (the default since rama 19).
  protected readonly selectedFrame = computed(() => {
    const frame = this.user()?.frameColor;
    if (!frameFollowsStreak(frame)) return frame;
    return this.wornFinish() ? null : FRAME_STREAK;
  });

  protected readonly streakFrameMaterial = computed(() => {
    const tier = streakTierFor(this.user()?.highestStreak);
    return tier ? this.t(STREAK_MATERIAL_KEYS[tier.key]) : null;
  });

  // Titles (rama 19): unlocked forever by the best streak ever; the user picks which to show.
  protected readonly profile = signal<UserProfileDto | null>(null);
  protected readonly isSavingTitle = signal(false);
  protected readonly titleAuto = TITLE_AUTO;
  protected readonly titleNone = TITLE_NONE;
  protected readonly selectedTitle = computed(() => this.profile()?.selectedTitleKey ?? TITLE_AUTO);

  protected readonly profileForm = this.fb.group({
    username: ['', [Validators.required, Validators.minLength(3)]],
    email: ['', [Validators.required, Validators.email]],
  });

  protected readonly passwordForm = this.fb.group({
    currentPassword: ['', Validators.required],
    newPassword: ['', [Validators.required, Validators.minLength(8)]],
    confirmPassword: ['', Validators.required],
  });

  ngOnInit(): void {
    const user = this.user();
    if (user) {
      this.profileForm.patchValue({ username: user.username, email: user.email });
    }
    this.prefsService.get().subscribe(prefs => this.notifPrefs.set(prefs));
    this.userService.getProfile().subscribe(profile => this.profile.set(profile));
  }

  setStreakDangerHours(hours: number | number[] | null | undefined): void {
    if (typeof hours !== 'number') return;
    const updated = { ...this.notifPrefs(), streakDangerHoursBefore: hours };
    this.notifPrefs.set(updated);
    this.prefsService.update(updated).subscribe();
  }

  selectTitle(key: string): void {
    this.isSavingTitle.set(true);
    this.userService.updateProfile({ titleKey: key }).subscribe({
      next: profile => {
        this.profile.set(profile);
        this.authService.patchCurrentUser({ title: profile.title });
        this.isSavingTitle.set(false);
      },
      error: () => {
        toast.error(this.t('profile.titleFailed'));
        this.isSavingTitle.set(false);
      },
    });
  }

  openMyStats(): void {
    const id = this.user()?.id;
    if (id != null) this.memberCard.openOwn(id);
  }

  togglePref(
    key:
      | 'newQuestion'
      | 'selectorTurn'
      | 'raceToSelect'
      | 'flashQuestion'
      | 'monthlySummary'
      | 'nudge'
      | 'reaction'
      | 'shopNews'
      | 'userVoted'
      | 'newMessage'
      | 'streakDanger',
  ): void {
    const updated = { ...this.notifPrefs(), [key]: !this.notifPrefs()[key] };
    this.notifPrefs.set(updated);
    this.prefsService.update(updated).subscribe();
  }

  async togglePushSubscription(): Promise<void> {
    if (this.pushService.enabled()) {
      await this.pushService.unsubscribe();
    } else if (!(await this.pushService.requestAndSubscribe())) {
      toast.error(
        this.pushService.permission() === 'denied'
          ? this.t('profile.pushBlockedToast')
          : this.t('profile.pushFailedToast')
      );
    }
  }

  goBack(): void {
    this.router.navigate(['/groups']);
  }

  selectFrame(color: string): void {
    this.isSavingFrame.set(true);
    this.userService.updateProfile({ frameColor: color }).subscribe({
      next: profile => {
        // A plain colour takes the ring finish off (rama 57).
        this.authService.patchCurrentUser({ frameColor: profile.frameColor, look: profile.look });
        this.isSavingFrame.set(false);
      },
      error: () => this.isSavingFrame.set(false),
    });
  }

  // Wears a finish (it puts the frame back on the streak ring), or takes it off.
  // Tapping the worn plate takes it off.
  selectPlate(key: string): void {
    this.isSavingPlate.set(true);
    this.userService.updateProfile({ plateKey: this.wornPlate() === key ? '' : key }).subscribe({
      next: profile => {
        this.authService.patchCurrentUser({ look: profile.look });
        this.isSavingPlate.set(false);
      },
      error: () => this.isSavingPlate.set(false),
    });
  }

  selectFinish(key: string): void {
    this.isSavingFrame.set(true);
    this.userService.updateProfile({ ringSkinKey: this.wornFinish() === key ? '' : key }).subscribe({
      next: profile => {
        this.authService.patchCurrentUser({ frameColor: profile.frameColor, look: profile.look });
        this.isSavingFrame.set(false);
      },
      error: () => this.isSavingFrame.set(false),
    });
  }

  saveProfile(): void {
    if (this.profileForm.invalid) return;
    const { username, email } = this.profileForm.value;
    this.isSavingProfile.set(true);
    this.profileError.set(null);
    this.profileSuccess.set(false);

    this.userService.updateProfile({ username: username!, email: email! }).subscribe({
      next: profile => {
        this.authService.patchCurrentUser({ username: profile.username, email: profile.email });
        this.profileForm.patchValue({ username: profile.username, email: profile.email });
        this.isSavingProfile.set(false);
        this.profileSuccess.set(true);
        setTimeout(() => this.profileSuccess.set(false), 3000);
      },
      error: err => {
        this.profileError.set(this.t(err.status === 409 ? 'profile.taken' : 'profile.saveFailed'));
        this.isSavingProfile.set(false);
      },
    });
  }

  changePassword(): void {
    if (this.passwordForm.invalid) return;
    const { currentPassword, newPassword, confirmPassword } = this.passwordForm.value;

    if (newPassword !== confirmPassword) {
      this.passwordError.set(this.t('auth.register.mismatch'));
      return;
    }

    this.isSavingPassword.set(true);
    this.passwordError.set(null);
    this.passwordSuccess.set(false);

    this.userService.changePassword({ currentPassword: currentPassword!, newPassword: newPassword! }).subscribe({
      next: () => {
        this.passwordForm.reset();
        this.isSavingPassword.set(false);
        this.passwordSuccess.set(true);
        setTimeout(() => this.passwordSuccess.set(false), 3000);
      },
      error: err => {
        // A 400 carries the API's reason (e.g. wrong current password) in the UI language.
        const fallback = this.t('profile.passwordFailed');
        this.passwordError.set(err.status === 400 ? apiErrorMessage(err, fallback) : fallback);
        this.isSavingPassword.set(false);
      },
    });
  }
}
