import { Component, OnInit, inject, signal } from '@angular/core';
import { PushNotificationService } from '../../../core/services/push-notification.service';

@Component({
  selector: 'app-push-notification-prompt',
  standalone: true,
  template: `
    @if (visible()) {
      <div class="fixed bottom-4 left-4 right-4 z-50 sm:left-auto sm:right-4 sm:w-80">
        <div class="bg-card border border-border rounded-2xl p-4 shadow-2xl">
          <div class="flex items-start gap-3">
            <div class="shrink-0 w-10 h-10 rounded-xl bg-accent/10 flex items-center justify-center text-lg">🔔</div>
            <div class="flex-1 min-w-0">
              <p class="text-sm font-semibold text-foreground leading-tight">¿Te avisamos de la pregunta?</p>
              <p class="text-xs text-muted-foreground mt-0.5 leading-snug">
                Recibe una notificación cuando llegue la pregunta del día y cuando te toque elegir.
              </p>
            </div>
            <button
              (click)="dismiss()"
              class="shrink-0 text-muted-foreground hover:text-foreground transition-colors p-1"
              aria-label="Cerrar"
            >
              <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4" viewBox="0 0 24 24" fill="none"
                stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/>
              </svg>
            </button>
          </div>
          <div class="flex gap-2 mt-3">
            <button
              (click)="activate()"
              [disabled]="loading()"
              class="flex-1 text-xs font-semibold bg-accent text-accent-foreground px-3 py-2 rounded-lg
                     hover:opacity-90 transition-opacity disabled:opacity-50 disabled:pointer-events-none"
            >
              {{ loading() ? 'Activando…' : 'Activar notificaciones' }}
            </button>
            <button
              (click)="dismiss()"
              class="text-xs text-muted-foreground px-3 py-2 rounded-lg hover:bg-muted transition-colors"
            >
              Ahora no
            </button>
          </div>
        </div>
      </div>
    }
  `,
})
export class PushNotificationPromptComponent implements OnInit {
  private readonly pushService = inject(PushNotificationService);

  protected visible = signal(false);
  protected loading = signal(false);

  ngOnInit() {
    // Small delay so it doesn't compete with the PWA install prompt visually.
    setTimeout(() => {
      if (this.pushService.shouldShowPrompt) this.visible.set(true);
    }, 4000);
  }

  protected async activate() {
    this.loading.set(true);
    await this.pushService.requestAndSubscribe();
    this.loading.set(false);
    this.visible.set(false);
  }

  protected dismiss() {
    this.pushService.dismissPrompt();
    this.visible.set(false);
  }
}
