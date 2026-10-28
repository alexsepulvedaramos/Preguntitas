import { Injectable, signal } from '@angular/core';

// Where plates show (rama 60, §17.18). By default only in the featured places (profile,
// member card, a result's leader, duels, monthly awards, shop). A temporary per-device switch
// in the profile also draws them in the member list, the chat and every result row, so the
// developer can judge how busy that gets; it goes once that is decided.
const STORAGE_KEY = 'vp_plates_everywhere';

function readEverywhere(): boolean {
  try {
    return localStorage.getItem(STORAGE_KEY) === '1';
  } catch {
    return false;
  }
}

@Injectable({ providedIn: 'root' })
export class PlatesDisplayService {
  readonly everywhere = signal(readEverywhere());

  setEverywhere(value: boolean): void {
    this.everywhere.set(value);
    try {
      localStorage.setItem(STORAGE_KEY, value ? '1' : '0');
    } catch {
      // Storage unavailable: the choice lasts for this visit.
    }
  }
}
