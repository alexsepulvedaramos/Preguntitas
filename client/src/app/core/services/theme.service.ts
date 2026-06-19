import { Injectable, signal, effect, inject, PLATFORM_ID } from '@angular/core';
import { DOCUMENT, isPlatformBrowser } from '@angular/common';

@Injectable({
    providedIn: 'root'
})
export class ThemeService {
    private document = inject(DOCUMENT);
    private platformId = inject(PLATFORM_ID);

    // Signal to hold the current theme state
    darkMode = signal<boolean>(this.getInitialTheme());

    constructor() {
        // Effect to update the DOM and localStorage whenever the signal changes
        effect(() => {
            const isDark = this.darkMode();

            if (isPlatformBrowser(this.platformId)) {
                if (isDark) {
                    this.document.documentElement.classList.add('dark');
                    localStorage.setItem('theme', 'dark');
                } else {
                    this.document.documentElement.classList.remove('dark');
                    localStorage.setItem('theme', 'light');
                }
            }
        });
    }

    // Toggle the theme
    toggleTheme() {
        this.darkMode.update(isDark => !isDark);
    }

    // Determine initial theme based on localStorage or system preference
    private getInitialTheme(): boolean {
        if (isPlatformBrowser(this.platformId)) {
            const storedTheme = localStorage.getItem('theme');
            if (storedTheme) {
                return storedTheme === 'dark';
            }
            return window.matchMedia('(prefers-color-scheme: dark)').matches;
        }
        // Default fallback for Server-Side Rendering
        return false;
    }
}