import { Injectable, signal, computed, effect, inject, PLATFORM_ID } from '@angular/core';
import { DOCUMENT, isPlatformBrowser } from '@angular/common';

export type ThemePreference = 'light' | 'dark' | 'system';

const STORAGE_KEY = 'theme';
// Where the shop app theme (rama 42) was cached on the device; themes were retired in rama 57,
// so it is only cleared.
const RETIRED_PALETTE_KEY = 'vp-palette';
const DARK_QUERY = '(prefers-color-scheme: dark)';

@Injectable({
    providedIn: 'root'
})
export class ThemeService {
    private document = inject(DOCUMENT);
    private platformId = inject(PLATFORM_ID);
    private isBrowser = isPlatformBrowser(this.platformId);

    // What the user picked; 'system' follows the device setting (no stored value)
    readonly preference = signal<ThemePreference>(this.getStoredPreference());

    // The device's colour scheme, kept live so 'system' reacts when it changes
    private systemDark = signal<boolean>(this.isBrowser && window.matchMedia(DARK_QUERY).matches);

    readonly darkMode = computed(() => {
        const preference = this.preference();
        return preference === 'dark' || (preference === 'system' && this.systemDark());
    });

    constructor() {
        if (this.isBrowser) {
            window.matchMedia(DARK_QUERY).addEventListener('change', e => this.systemDark.set(e.matches));
            try {
                localStorage.removeItem(RETIRED_PALETTE_KEY);
            } catch {
                // Storage blocked: nothing was cached either.
            }
        }

        // Keep the DOM class and localStorage in sync with the current state
        effect(() => {
            if (!this.isBrowser) return;

            this.document.documentElement.classList.toggle('dark', this.darkMode());

            const preference = this.preference();
            if (preference === 'system') {
                localStorage.removeItem(STORAGE_KEY);
            } else {
                localStorage.setItem(STORAGE_KEY, preference);
            }
        });
    }

    setPreference(preference: ThemePreference) {
        this.preference.set(preference);
    }

    // Flip to the opposite of what is shown right now (header button on guest pages)
    toggleTheme() {
        this.preference.set(this.darkMode() ? 'light' : 'dark');
    }

    private getStoredPreference(): ThemePreference {
        if (!this.isBrowser) return 'system';
        const stored = localStorage.getItem(STORAGE_KEY);
        return stored === 'light' || stored === 'dark' ? stored : 'system';
    }
}
