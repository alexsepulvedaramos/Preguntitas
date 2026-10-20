import { ChangeDetectionStrategy, Component, forwardRef } from '@angular/core';
import { BrnDialog, provideBrnDialogDefaultOptions } from '@spartan-ng/brain/dialog';
import { BrnSheet } from '@spartan-ng/brain/sheet';
import { HlmSheetOverlay } from './hlm-sheet-overlay';

@Component({
	selector: 'hlm-sheet',
	exportAs: 'hlmSheet',
	imports: [HlmSheetOverlay],
	providers: [
		{
			provide: BrnDialog,
			useExisting: forwardRef(() => BrnSheet),
		},
		{
			provide: BrnSheet,
			useExisting: forwardRef(() => HlmSheet),
		},
		provideBrnDialogDefaultOptions({
			// Focus the panel itself on open, not its first button: opening with a tap must not
			// light up the close button's focus ring (UX audit T7). Tab still reaches everything.
			autoFocus: 'dialog',
		}),
	],
	changeDetection: ChangeDetectionStrategy.OnPush,
	template: `
		<hlm-sheet-overlay />
		<ng-content />
	`,
})
export class HlmSheet extends BrnSheet {}
