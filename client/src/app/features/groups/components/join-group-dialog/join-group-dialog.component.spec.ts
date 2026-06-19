import { ComponentFixture, TestBed } from '@angular/core/testing';

import { JoinGroupDialogComponent } from './join-group-dialog.component';

describe('JoinGroupDialogComponent', () => {
  let component: JoinGroupDialogComponent;
  let fixture: ComponentFixture<JoinGroupDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [JoinGroupDialogComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(JoinGroupDialogComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
