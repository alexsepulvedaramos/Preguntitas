import { ChangeDetectionStrategy, Component, ViewEncapsulation } from '@angular/core';
import { LoginComponent } from '../components/login/login.component';

@Component({
    selector: 'spartan-login-simple-reactive-form',
    imports: [LoginComponent],
    encapsulation: ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        class: 'block',
    },
    styleUrl: './login-page.component.css',
    template: `
		<div class="flex min-h-svh w-full items-center justify-center p-6 md:p-10">
			<div class="w-full max-w-sm">
				<app-login />
			</div>
		</div>
	`,
})
export default class LoginSimpleReactiveFormPage { }