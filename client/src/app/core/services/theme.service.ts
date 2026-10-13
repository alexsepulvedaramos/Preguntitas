import { Injectable, signal, computed, effect, inject, PLATFORM_ID } from '@angular/core';
import { DOCUMENT, isPlatformBrowser } from '@angular/common';

export type ThemePreference = 'light' | 'dark' | 'system';

const STORAGE_KEY = 'theme';
// The shop theme last seen on this device (rama 42), so a reload paints it before the
// profile arrives.
const PALETTE_KEY = 'vp-palette';
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

    // Personal app theme bought in the shop (rama 42): a data-vp-theme value, or null for the
    // default lime. The shop's try-on overrides it while it is set.
    readonly palette = signal<string | null>(this.getStoredPalette());
    readonly previewPalette = signal<string | null | undefined>(undefined);

    readonly darkMode = computed(() => {
        const preference = this.preference();
        return preference === 'dark' || (preference === 'system' && this.systemDark());
    });

    constructor() {
        if (this.isBrowser) {
            window.matchMedia(DARK_QUERY).addEventListener('change', e => this.systemDark.set(e.matches));
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

        effect(() => {
            if (!this.isBrowser) return;
            const preview = this.previewPalette();
            const palette = preview !== undefined ? preview : this.palette();
            const root = this.document.documentElement;
            if (palette) root.setAttribute('data-vp-theme', palette);
            else root.removeAttribute('data-vp-theme');
        });
    }

    setPalette(palette: string | null) {
        this.palette.set(palette);
        if (!this.isBrowser) return;
        try {
            if (palette) localStorage.setItem(PALETTE_KEY, palette);
            else localStorage.removeItem(PALETTE_KEY);
        } catch {
            // Storage blocked: the theme still applies for this visit.
        }
    }

    setPreference(preference: ThemePreference) {
        this.preference.set(preference);
    }

    // Flip to the opposite of what is shown right now (header button on guest pages)
    toggleTheme() {
        this.preference.set(this.darkMode() ? 'light' : 'dark');
    }

    private getStoredPalette(): string | null {
        if (!this.isBrowser) return null;
        try {
            return localStorage.getItem(PALETTE_KEY);
        } catch {
            return null;
        }
    }

    private getStoredPreference(): ThemePreference {
        if (!this.isBrowser) return 'system';
        const stored = localStorage.getItem(STORAGE_KEY);
        return stored === 'light' || stored === 'dark' ? stored : 'system';
    }
}
