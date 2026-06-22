import { ChangeDetectionStrategy, Component, ViewEncapsulation } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { HeaderComponent } from "../../../core/layout/header/header.component";

@Component({
    selector: 'spartan-login-simple-reactive-form',
    imports: [RouterOutlet, HeaderComponent],
    encapsulation: ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        class: 'block',
    },
    styleUrl: './auth-layout.component.css',
    templateUrl: './auth-layout.component.html'
})
export class AuthLayoutComponent { }