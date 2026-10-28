import { Injectable, signal } from '@angular/core';

// Where plates show (rama 60, §17.18). By default only in the featured places (profile,
// member card, a result's leader, duels, monthly awards, shop). A temporary per-device switch
// in the profile also draws them in the member list, the chat and every result row, so the
// developer can judge how busy that gets; it goes once that is decided.
const STORAGE_KEY = 'vp_plates_everywhere';
// Second trial (developer, 2026-10-28): Garabato's page with the frame's icon stickers instead
// of the doodles.
const STICKERS_KEY = 'vp_doodle_stickers';

function readFlag(key: string): boolean {
  try {
    return localStorage.getItem(key) === '1';
  } catch {
    return false;
  }
}

function writeFlag(key: string, value: boolean): void {
  try {
    localStorage.setItem(key, value ? '1' : '0');
  } catch {
    // Storage unavailable: the choice lasts for this visit.
  }
}

@Injectable({ providedIn: 'root' })
export class PlatesDisplayService {
  readonly everywhere = signal(readFlag(STORAGE_KEY));
  readonly doodleStickers = signal(readFlag(STICKERS_KEY));

  setEverywhere(value: boolean): void {
    this.everywhere.set(value);
    writeFlag(STORAGE_KEY, value);
  }

  setDoodleStickers(value: boolean): void {
    this.doodleStickers.set(value);
    writeFlag(STICKERS_KEY, value);
  }
}
