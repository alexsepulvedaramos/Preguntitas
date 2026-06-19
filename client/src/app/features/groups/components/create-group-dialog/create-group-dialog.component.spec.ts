import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CreateGroupDialogComponent } from './create-group-dialog.component';

describe('CreateGroupDialogComponent', () => {
  let component: CreateGroupDialogComponent;
  let fixture: ComponentFixture<CreateGroupDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CreateGroupDialogComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(CreateGroupDialogComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
