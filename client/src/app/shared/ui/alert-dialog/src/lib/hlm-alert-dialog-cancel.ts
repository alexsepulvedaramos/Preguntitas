import { Directive, HostListener, inject, input } from '@angular/core';
import { BrnDialog } from '@spartan-ng/brain/dialog';
import { HlmButton, provideBrnButtonConfig } from '@spartan-ng/helm/button';

// BrnDialogClose host directive replaced with direct BrnDialog injection:
// BrnDialogClose injects BrnDialogRef (not optional), which crashes inline alert-dialogs.
// BrnDialog is provided by HlmAlertDialog itself and supports optional injection.
@Directive({
	selector: 'button[hlmAlertDialogCancel]',
	providers: [provideBrnButtonConfig({ variant: 'outline' })],
	hostDirectives: [{ directive: HlmButton, inputs: ['variant', 'size'] }],
	host: { 'data-slot': 'alert-dialog-cancel', '[type]': 'type()' },
})
export class HlmAlertDialogCancel {
	private readonly _dialog = inject(BrnDialog, { optional: true });
	public readonly type = input<'button' | 'submit' | 'reset'>('button');

	@HostListener('click')
	close() {
		this._dialog?.close();
	}
}
