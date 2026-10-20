import { Component, inject, input } from '@angular/core';
import { provideIcons } from '@ng-icons/core';
import { lucideLanguages } from '@ng-icons/lucide';

import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmDropdownMenuImports } from '@spartan-ng/helm/dropdown-menu';
import { HlmIconImports } from '@spartan-ng/helm/icon';
import { HlmToggleGroupImports } from '@spartan-ng/helm/toggle-group';

import { I18nService } from '../../../core/i18n/i18n.service';
import { LanguagePreferenceService } from '../../../core/i18n/language-preference.service';
import { LANGUAGES, LANGUAGE_NAMES, Lang, isLang } from '../../../core/i18n/languages';

// Language picker (rama 23). `compact`: a globe button with a dropdown, for top bars and the
// auth pages. `toggle`: a full-width toggle group, for the user menu and the profile.
// Language names are always shown in their own language.
@Component({
  selector: 'app-language-picker',
  imports: [HlmButtonImports, HlmDropdownMenuImports, HlmIconImports, HlmToggleGroupImports],
  providers: [provideIcons({ lucideLanguages })],
  template: `
    @if (variant() === 'toggle') {
      <hlm-toggle-group
        type="single"
        variant="outline"
        [nullable]="false"
        [value]="i18n.lang()"
        (valueChange)="select($event)"
        [attr.aria-label]="t('common.language')"
        class="w-full"
      >
        @for (lang of languages; track lang) {
          <button hlmToggleGroupItem [value]="lang" class="flex-1" [attr.lang]="lang">
            {{ names[lang] }}
          </button>
        }
      </hlm-toggle-group>
    } @else {
      <button
        hlmBtn
        variant="ghost"
        size="sm"
        class="gap-1.5 rounded-full px-2.5 text-foreground hover:bg-muted"
        [hlmDropdownMenuTrigger]="menu"
        align="end"
        [attr.aria-label]="t('common.language') + ': ' + names[i18n.lang()]"
      >
        <ng-icon hlm name="lucideLanguages" size="sm" />
        <span class="hidden text-xs font-semibold uppercase sm:inline">{{ i18n.lang() }}</span>
      </button>

      <ng-template #menu>
        <hlm-dropdown-menu class="w-40">
          @for (lang of languages; track lang) {
            <button
              hlmDropdownMenuRadio
              [checked]="lang === i18n.lang()"
              [attr.lang]="lang"
              (triggered)="select(lang)"
            >
              <hlm-dropdown-menu-radio-indicator />
              {{ names[lang] }}
            </button>
          }
        </hlm-dropdown-menu>
      </ng-template>
    }
  `,
})
export class LanguagePickerComponent {
  protected readonly i18n = inject(I18nService);
  private readonly preference = inject(LanguagePreferenceService);
  protected readonly t = this.i18n.t;

  readonly variant = input<'compact' | 'toggle'>('compact');

  protected readonly languages = LANGUAGES;
  protected readonly names = LANGUAGE_NAMES;

  protected select(value: Lang | Lang[] | string | null | undefined): void {
    if (isLang(value)) this.preference.change(value);
  }
}
