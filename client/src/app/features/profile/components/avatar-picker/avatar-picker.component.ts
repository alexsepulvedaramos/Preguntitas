import {
  Component, output, input, signal, inject, ElementRef, viewChild
} from '@angular/core';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideCamera, lucideImage, lucideCheck, lucideRefreshCw } from '@ng-icons/lucide';
import { HlmButtonImports } from '@spartan-ng/helm/button';
import { AuthService } from '../../../../core/auth/auth.service';
import { UserService } from '../../../../core/services/user.service';

export interface DiceBearStyle {
  id: string;
  label: string;
}

const DICEBEAR_STYLES: DiceBearStyle[] = [
  { id: 'bottts-neutral', label: 'Robots' },
  { id: 'fun-emoji', label: 'Emojis' },
  { id: 'pixel-art', label: 'Pixel Art' },
  { id: 'adventurer', label: 'Aventureros' },
  { id: 'notionists-neutral', label: 'Sketches' },
  { id: 'big-smile', label: 'Sonrisas' },
  { id: 'croodles-neutral', label: 'Garabatos' },
  { id: 'lorelei', label: 'Retratos' },
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
      this.errorMessage.set('No se pudo procesar la imagen.');
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
      error: () => {
        this.errorMessage.set('Error al subir la imagen. Inténtalo de nuevo.');
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
        this.errorMessage.set('Error al guardar el avatar.');
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
