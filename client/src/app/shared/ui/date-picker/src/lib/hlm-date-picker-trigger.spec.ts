import { Component, viewChild } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { HlmDatePickerImports } from '@spartan-ng/helm/date-picker';
import { HlmDialog } from '@spartan-ng/helm/dialog';

@Component({
  imports: [HlmDialog, HlmDatePickerImports],
  template: `
    <hlm-dialog>
      <hlm-date-picker>
        <hlm-date-picker-trigger>Pick</hlm-date-picker-trigger>
      </hlm-date-picker>
    </hlm-dialog>
  `,
})
class HostComponent {
  readonly dialog = viewChild.required(HlmDialog);
}

describe('HlmDatePickerTrigger', () => {
  // Inside a dialog (the flash form), the trigger used to wire the dialog itself to close on
  // outside clicks, so choosing a month or year in the calendar's dropdowns closed the form.
  it('leaves an enclosing dialog alone', () => {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    const dialog = fixture.componentInstance.dialog();
    expect(dialog.mutableCloseOnOutsidePointerEvents()).toBe(false);
    expect(dialog.mutableAttachTo()).toBeNull();
  });
});
