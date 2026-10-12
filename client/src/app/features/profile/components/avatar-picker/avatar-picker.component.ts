import {
  Component, output, input, signal, inject, ElementRef, viewChild
} from '@angular/core';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideCamera, lucideImage, lucideCheck, lucideRefreshCw } from '@ng-icons/lucide';
import { HlmButtonImports } from '@spartan-ng/helm/button';
import { AuthService } from '../../../../core/auth/auth.service';
import { UserService } from '../../../../core/services/user.service';
import { needsLightBackdrop } from '../../../../shared/utils/avatar-backdrop';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { TranslationKey } from '../../../../core/i18n/translation.types';
import { apiErrorMessage } from '../../../../shared/utils/api-error';

export interface DiceBearStyle {
  id: string;
  label: TranslationKey;
}

const DICEBEAR_STYLES: DiceBearStyle[] = [
  { id: 'bottts-neutral', label: 'avatar.styles.robots' },
  { id: 'fun-emoji', label: 'avatar.styles.emojis' },
  { id: 'pixel-art', label: 'avatar.styles.pixelArt' },
  { id: 'adventurer', label: 'avatar.styles.adventurers' },
  { id: 'notionists-neutral', label: 'avatar.styles.sketches' },
  { id: 'big-smile', label: 'avatar.styles.smiles' },
  { id: 'croodles-neutral', label: 'avatar.styles.doodles' },
  { id: 'lorelei', label: 'avatar.styles.portraits' },
];

@Component({
  selector: 'app-avatar-picker',
  standalone: true,
  imports: [HlmButtonImports, NgIcon],
  providers: [provideIcons({ lucideCamera, lucideImage, lucideCheck, lucideRefreshCw })],
  templateUrl: './avatar-picker.component.html',
})
export class AvatarPickerComponent {
  readonly username = input.required<string>();
  readonly avatarSelected = output<void>();

  protected readonly authService = inject(AuthService);
  protected readonly userService = inject(UserService);
  protected readonly t = inject(I18nService).t;

  protected readonly styles = DICEBEAR_STYLES;
  protected readonly activeTab = signal<'photo' | 'generated'>('photo');
  protected readonly isUploading = signal(false);
  protected readonly previewUrl = signal<string | null>(null);
  protected readonly previewBlob = signal<Blob | null>(null);
  protected readonly errorMessage = signal<string | null>(null);

  private readonly fileInput = viewChild<ElementRef<HTMLInputElement>>('fileInput');

  diceBearUrl(styleId: string): string {
    return `https://api.dicebear.com/9.x/${styleId}/svg?seed=${encodeURIComponent(this.username())}`;
  }

  lightBackdrop(styleId: string): boolean {
    return needsLightBackdrop(this.diceBearUrl(styleId));
  }

  triggerFileInput(): void {
    this.fileInput()?.nativeElement.click();
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    this.errorMessage.set(null);
    this.cropImageToCircle(file).then(blob => {
      this.previewBlob.set(blob);
      this.previewUrl.set(URL.createObjectURL(blob));
    }).catch(() => {
      this.errorMessage.set(this.t('avatar.processFailed'));
    });

    input.value = '';
  }

  retakePhoto(): void {
    this.previewUrl.set(null);
    this.previewBlob.set(null);
  }

  confirmPhotoUpload(): void {
    const blob = this.previewBlob();
    if (!blob) return;

    this.isUploading.set(true);
    this.errorMessage.set(null);

    this.userService.uploadAvatar(blob, 'image/jpeg').subscribe({
      next: ({ avatarUrl }) => {
        this.authService.patchCurrentUser({ avatarUrl });
        this.isUploading.set(false);
        this.avatarSelected.emit();
      },
      error: err => {
        // A 400 explains what's wrong with the file, in the UI language.
        const fallback = this.t('avatar.uploadFailed');
        this.errorMessage.set(err.status === 400 ? apiErrorMessage(err, fallback) : fallback);
        this.isUploading.set(false);
      },
    });
  }

  selectDiceBear(styleId: string): void {
    const url = this.diceBearUrl(styleId);
    this.isUploading.set(true);
    this.errorMessage.set(null);

    this.userService.updateProfile({ avatarUrl: url }).subscribe({
      next: profile => {
        this.authService.patchCurrentUser({ avatarUrl: profile.avatarUrl });
        this.isUploading.set(false);
        this.avatarSelected.emit();
      },
      error: () => {
        this.errorMessage.set(this.t('avatar.saveFailed'));
        this.isUploading.set(false);
      },
    });
  }

  private cropImageToCircle(file: File): Promise<Blob> {
    return new Promise((resolve, reject) => {
      const img = new Image();
      const objectUrl = URL.createObjectURL(file);
      img.onload = () => {
        const size = Math.min(img.width, img.height);
        const canvas = document.createElement('canvas');
        canvas.width = 400;
        canvas.height = 400;
        const ctx = canvas.getContext('2d');
        if (!ctx) { reject(new Error('Canvas not supported')); return; }

        ctx.beginPath();
        ctx.arc(200, 200, 200, 0, Math.PI * 2);
        ctx.clip();

        const offsetX = (img.width - size) / 2;
        const offsetY = (img.height - size) / 2;
        ctx.drawImage(img, offsetX, offsetY, size, size, 0, 0, 400, 400);

        canvas.toBlob(blob => {
          URL.revokeObjectURL(objectUrl);
          if (blob) resolve(blob);
          else reject(new Error('Blob conversion failed'));
        }, 'image/jpeg', 0.88);
      };
      img.onerror = () => { URL.revokeObjectURL(objectUrl); reject(new Error('Image load failed')); };
      img.src = objectUrl;
    });
  }
}
