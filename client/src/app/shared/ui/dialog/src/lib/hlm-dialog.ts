import { ChangeDetectionStrategy, Component, forwardRef } from '@angular/core';
import { BrnDialog, provideBrnDialogDefaultOptions } from '@spartan-ng/brain/dialog';
import { HlmDialogOverlay } from './hlm-dialog-overlay';

@Component({
	selector: 'hlm-dialog',
	exportAs: 'hlmDialog',
	imports: [HlmDialogOverlay],
	providers: [
		{
			provide: BrnDialog,
			useExisting: forwardRef(() => HlmDialog),
		},
		provideBrnDialogDefaultOptions({
			// Focus the panel itself on open, not its first button: opening with a tap must not
			// light up the close button's focus ring (UX audit T7). Tab still reaches everything.
			autoFocus: 'dialog',
		}),
	],
	changeDetection: ChangeDetectionStrategy.OnPush,
	template: `
		<hlm-dialog-overlay />
		<ng-content />
	`,
})
export class HlmDialog extends BrnDialog {}
