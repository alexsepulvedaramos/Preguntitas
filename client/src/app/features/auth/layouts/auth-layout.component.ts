import { ChangeDetectionStrategy, Component, ViewEncapsulation } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
    selector: 'spartan-login-simple-reactive-form',
    imports: [RouterOutlet],
    encapsulation: ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        class: 'block',
    },
    styleUrl: './auth-layout.component.css',
    template: `
		<div class="flex min-h-svh w-full items-center justify-center p-6 md:p-10">
			<div class="w-full max-w-sm">
				<router-outlet></router-outlet>
			</div>
		</div>
	`,
})
export class AuthLayoutComponent { }