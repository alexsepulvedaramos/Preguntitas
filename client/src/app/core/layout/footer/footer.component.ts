import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

import { APP_VERSION } from '../../releases/releases';

@Component({
  selector: 'app-footer',
  imports: [RouterLink],
  templateUrl: './footer.component.html',
  styleUrl: './footer.component.css',
})
export class FooterComponent {
  protected readonly version = APP_VERSION;
}
