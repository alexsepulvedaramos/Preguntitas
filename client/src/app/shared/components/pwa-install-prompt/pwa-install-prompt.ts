import { Component, HostListener, OnInit, signal } from '@angular/core';

@Component({
  selector: 'app-pwa-install-prompt',
  standalone: true,
  template: `
    @if (showAndroid()) {
      <div class="fixed bottom-4 left-4 right-4 z-50 sm:left-auto sm:right-4 sm:w-80">
        <div class="bg-card border border-border rounded-2xl p-4 shadow-2xl flex items-center gap-3">
          <img
            src="icons/android-chrome-192x192.png"
            alt="Vaya Preguntita"
            class="w-11 h-11 rounded-xl shrink-0"
          />
          <div class="flex-1 min-w-0">
            <p class="text-sm font-semibold text-foreground leading-tight">Instala la app</p>
            <p class="text-xs text-muted-foreground mt-0.5">Accede desde tu pantalla de inicio</p>
          </div>
          <div class="flex flex-col gap-1.5 shrink-0">
            <button
              (click)="install()"
              class="text-xs font-semibold bg-primary text-primary-foreground px-3 py-1.5 rounded-lg hover:opacity-90 transition-opacity"
            >
              Instalar
            </button>
            <button
              (click)="dismissAndroid()"
              class="text-xs text-muted-foreground px-3 py-1.5 rounded-lg hover:bg-muted transition-colors text-center"
            >
              Ahora no
            </button>
          </div>
        </div>
      </div>
    }

    @if (showIos()) {
      <div class="fixed bottom-4 left-4 right-4 z-50 sm:left-auto sm:right-4 sm:w-80">
        <div class="bg-card border border-border rounded-2xl p-4 shadow-2xl">
          <div class="flex items-center gap-3 mb-3">
            <img
              src="icons/android-chrome-192x192.png"
              alt="Vaya Preguntita"
              class="w-11 h-11 rounded-xl shrink-0"
            />
            <div class="flex-1">
              <p class="text-sm font-semibold text-foreground leading-tight">Instala la app</p>
              <p class="text-xs text-muted-foreground mt-0.5">Sigue estos pasos en Safari</p>
            </div>
            <button
              (click)="dismissIos()"
              class="text-muted-foreground hover:text-foreground transition-colors p-1 shrink-0"
              aria-label="Cerrar"
            >
              <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/>
              </svg>
            </button>
          </div>
          <ol class="space-y-2">
            <li class="flex items-center gap-2 text-xs text-muted-foreground">
              <span class="text-foreground font-semibold shrink-0">1.</span>
              <span>Pulsa el botón compartir</span>
              <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4 text-primary shrink-0" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <path d="M4 12v8a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2v-8"/><polyline points="16 6 12 2 8 6"/><line x1="12" y1="2" x2="12" y2="15"/>
              </svg>
            </li>
            <li class="flex items-center gap-2 text-xs text-muted-foreground">
              <span class="text-foreground font-semibold shrink-0">2.</span>
              <span>Toca <strong class="text-foreground">"Añadir a pantalla de inicio"</strong></span>
            </li>
            <li class="flex items-center gap-2 text-xs text-muted-foreground">
              <span class="text-foreground font-semibold shrink-0">3.</span>
              <span>Confirma pulsando <strong class="text-foreground">"Añadir"</strong></span>
            </li>
          </ol>
        </div>
      </div>
    }
  `,
})
export class PwaInstallPromptComponent implements OnInit {
  protected showAndroid = signal(false);
  protected showIos = signal(false);
  private deferredPrompt: any = null;

  ngOnInit() {
    const isIos = /iphone|ipad|ipod/i.test(navigator.userAgent);
    const isInstalled = (navigator as any).standalone === true;
    if (isIos && !isInstalled) {
      this.showIos.set(true);
    }
  }

  @HostListener('window:beforeinstallprompt', ['$event'])
  onBeforeInstallPrompt(event: Event) {
    event.preventDefault();
    this.deferredPrompt = event;
    this.showAndroid.set(true);
  }

  protected dismissAndroid() {
    this.showAndroid.set(false);
  }

  protected dismissIos() {
    this.showIos.set(false);
  }

  protected async install() {
    if (!this.deferredPrompt) return;
    this.deferredPrompt.prompt();
    await this.deferredPrompt.userChoice;
    this.deferredPrompt = null;
    this.showAndroid.set(false);
  }
}
