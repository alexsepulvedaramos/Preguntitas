import { Component } from '@angular/core';

import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideUser, lucidePlus } from '@ng-icons/lucide';
import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';

@Component({
  selector: 'app-group-list',
  imports: [
    HlmButtonImports,
    HlmCardImports,
    NgIcon
  ],
  providers: [provideIcons({ lucideUser, lucidePlus })],
  templateUrl: "./group-list.component.html",
  styleUrl: "./group-list.component.css"
})
export class GroupListComponent {
  groups = [
    { id: 1, name: 'Work', members: 8 },
    { id: 2, name: 'Family', members: 5 },
    { id: 3, name: 'Friends', members: 12 },
    { id: 4, name: 'Project X', members: 3 },
  ];
}