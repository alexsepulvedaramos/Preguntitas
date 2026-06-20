import { ChangeDetectionStrategy, Component, ViewEncapsulation } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { ThemeComponent } from "../../../core/layout/theme/theme.component";
import { LogoWordmarkComponent } from "../../../core/layout/logo/logo-wordmark.component";
import { HeaderComponent } from "../../../core/layout/header/header.component";

@Component({
    selector: 'spartan-login-simple-reactive-form',
    imports: [RouterOutlet, ThemeComponent, LogoWordmarkComponent, HeaderComponent],
    encapsulation: ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        class: 'block',
    },
    styleUrl: './auth-layout.component.css',
    templateUrl: './auth-layout.component.html'
})
export class AuthLayoutComponent { }