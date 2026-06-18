import { Component } from '@angular/core';
import { HlmAvatarImports } from '@spartan-ng/helm/avatar';

@Component({
  selector: 'app-header',
  imports: [HlmAvatarImports],
  templateUrl: './header.component.html',
  styleUrl: './header.component.css',
})
export class HeaderComponent { }
