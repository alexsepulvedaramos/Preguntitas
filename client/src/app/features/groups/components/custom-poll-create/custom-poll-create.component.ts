import { Component, computed, input, model } from '@angular/core';

import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmFieldImports } from '@spartan-ng/helm/field';
import { HlmInputImports } from '@spartan-ng/helm/input';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucidePlus, lucideX } from '@ng-icons/lucide';

const MIN_OPTIONS = 2;
const MAX_OPTIONS = 8;

// Custom poll create fields (spec §5.5, §11): 2-8 text options (1-80 chars each), plus
// min/max selections allowed per voter (single vs multi-select).
@Component({
  selector: 'app-custom-poll-create',
  imports: [HlmButtonImports, HlmFieldImports, HlmInputImports, NgIcon],
  providers: [provideIcons({ lucidePlus, lucideX })],
  templateUrl: './custom-poll-create.component.html',
})
export class CustomPollCreateComponent {
  public readonly options = model<string[]>(['', '']);
  public readonly minSelections = model(1);
  public readonly maxSelections = model(1);
  public readonly allowOther = model(false);

  protected readonly canAddOption = computed(() => this.options().length < MAX_OPTIONS);
  protected readonly canRemoveOption = computed(() => this.options().length > MIN_OPTIONS);

  updateOption(index: number, text: string) {
    const options = [...this.options()];
    options[index] = text;
    this.options.set(options);
  }

  addOption() {
    if (!this.canAddOption()) return;
    this.options.set([...this.options(), '']);
  }

  removeOption(index: number) {
    if (!this.canRemoveOption()) return;
    this.options.set(this.options().filter((_, i) => i !== index));

    // Keep min/max within the new option count
    const count = this.options().length;
    if (this.maxSelections() > count) this.maxSelections.set(count);
    if (this.minSelections() > this.maxSelections()) this.minSelections.set(this.maxSelections());
  }

  onMinSelectionsChange(value: string) {
    const min = Number(value);
    this.minSelections.set(min);
    // Min can never exceed max.
    if (min > this.maxSelections()) this.maxSelections.set(min);
  }

  onMaxSelectionsChange(value: string) {
    const max = Number(value);
    this.maxSelections.set(max);
    // Max can never drop below min.
    if (max < this.minSelections()) this.minSelections.set(max);
  }

  toggleAllowOther() {
    this.allowOther.set(!this.allowOther());
  }
}
