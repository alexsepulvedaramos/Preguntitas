import { TestBed } from '@angular/core/testing';

import { Groups } from './groups.service';

describe('Groups', () => {
  let service: Groups;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(Groups);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
