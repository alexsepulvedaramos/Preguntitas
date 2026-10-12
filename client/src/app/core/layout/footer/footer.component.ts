import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';

import { APP_VERSION } from '../../releases/releases';
import { I18nService } from '../../i18n/i18n.service';

@Component({
  selector: 'app-footer',
  imports: [RouterLink],
  templateUrl: './footer.component.html',
  styleUrl: './footer.component.css',
})
export class FooterComponent {
  protected readonly version = APP_VERSION;
  protected readonly t = inject(I18nService).t;
}
