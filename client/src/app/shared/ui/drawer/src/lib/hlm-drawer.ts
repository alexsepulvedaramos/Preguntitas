import { ChangeDetectionStrategy, Component, forwardRef } from '@angular/core';
import { BrnDialog, provideBrnDialogDefaultOptions } from '@spartan-ng/brain/dialog';
import { BrnDrawer } from '@spartan-ng/brain/drawer';
import { HlmDrawerOverlay } from './hlm-drawer-overlay';

@Component({
	selector: 'hlm-drawer',
	exportAs: 'hlmDrawer',
	imports: [HlmDrawerOverlay],
	providers: [
		{
			provide: BrnDialog,
			useExisting: forwardRef(() => HlmDrawer),
		},
		{
			provide: BrnDrawer,
			useExisting: forwardRef(() => HlmDrawer),
		},
		provideBrnDialogDefaultOptions({
			// Focus the panel itself on open, not its first button: opening with a tap must not
			// light up the close button's focus ring (UX audit T7). Tab still reaches everything.
			autoFocus: 'dialog',
		}),
	],
	changeDetection: ChangeDetectionStrategy.OnPush,
	template: `
		<hlm-drawer-overlay />
		<ng-content />
	`,
})
export class HlmDrawer extends BrnDrawer {}
