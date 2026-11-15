import type { BooleanInput, NumberInput } from '@angular/cdk/coercion';
import { ChangeDetectionStrategy, Component, booleanAttribute, computed, input, numberAttribute } from '@angular/core';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideCheck, lucideInfo, lucideLoader2, lucideTriangleAlert, lucideX } from '@ng-icons/lucide';
import { BrnSonnerImports, type ToasterProps } from '@spartan-ng/brain/sonner';
import { hlm } from '@spartan-ng/helm/utils';
import type { ClassValue } from 'clsx';

@Component({
	selector: 'hlm-toaster',
	imports: [BrnSonnerImports, NgIcon],
	providers: [provideIcons({ lucideCheck, lucideInfo, lucideTriangleAlert, lucideX, lucideLoader2 })],
	changeDetection: ChangeDetectionStrategy.OnPush,
	template: `
		<brn-sonner-toaster
			[class]="_computedClass()"
			[invert]="invert()"
			[theme]="theme()"
			[position]="position()"
			[hotKey]="hotKey()"
			[richColors]="richColors()"
			[expand]="expand()"
			[duration]="duration()"
			[visibleToasts]="visibleToasts()"
			[closeButton]="closeButton()"
			[toastOptions]="_computedToastOptions()"
			[offset]="offset()"
			[style]="userStyle()"
		>
			<!-- A glyph tile per kind (§14, rama 56): sage for done and info, red only for errors -->
			<ng-template #loadingIcon>
				<span class="vp-toast-tile"><ng-icon name="lucideLoader2" class="overflow-visible! [&>svg]:motion-safe:animate-spin" /></span>
			</ng-template>
			<ng-template #successIcon>
				<span class="vp-toast-tile"><ng-icon name="lucideCheck" class="overflow-visible!" /></span>
			</ng-template>
			<ng-template #errorIcon>
				<span class="vp-toast-tile vp-toast-tile--error"><ng-icon name="lucideX" class="overflow-visible!" /></span>
			</ng-template>
			<ng-template #infoIcon>
				<span class="vp-toast-tile"><ng-icon name="lucideInfo" class="overflow-visible!" /></span>
			</ng-template>
			<ng-template #warningIcon>
				<span class="vp-toast-tile vp-toast-tile--warning"><ng-icon name="lucideTriangleAlert" class="overflow-visible!" /></span>
			</ng-template>
		</brn-sonner-toaster>
	`,
})
export class HlmToaster {
	public readonly invert = input<ToasterProps['invert'], BooleanInput>(false, {
		transform: booleanAttribute,
	});
	public readonly theme = input<ToasterProps['theme']>('light');
	public readonly position = input<ToasterProps['position']>('bottom-right');
	public readonly hotKey = input<ToasterProps['hotkey']>(['altKey', 'KeyT']);
	public readonly richColors = input<ToasterProps['richColors'], BooleanInput>(false, {
		transform: booleanAttribute,
	});
	public readonly expand = input<ToasterProps['expand'], BooleanInput>(false, {
		transform: booleanAttribute,
	});
	public readonly duration = input<ToasterProps['duration'], NumberInput>(4000, {
		transform: numberAttribute,
	});
	public readonly visibleToasts = input<ToasterProps['visibleToasts'], NumberInput>(3, {
		transform: numberAttribute,
	});
	public readonly closeButton = input<ToasterProps['closeButton'], BooleanInput>(false, {
		transform: booleanAttribute,
	});
	public readonly toastOptions = input<ToasterProps['toastOptions']>({});

	protected readonly _computedToastOptions = computed(() => {
		const options = this.toastOptions();
		return {
			...options,
			classes: {
				...options?.classes,
				toast: hlm(options?.classes?.toast),
			},
		};
	});
	public readonly offset = input<ToasterProps['offset']>(null);
	public readonly userClass = input<ClassValue>('', { alias: 'class' });
	public readonly userStyle = input<Record<string, string>>(
		{
			'--normal-bg': 'var(--popover)',
			'--normal-text': 'var(--popover-foreground)',
			'--normal-border': 'var(--border)',
			'--border-radius': 'var(--radius)',
		},
		{ alias: 'style' },
	);

	protected readonly _computedClass = computed(() => hlm('toaster group', this.userClass()));
}
