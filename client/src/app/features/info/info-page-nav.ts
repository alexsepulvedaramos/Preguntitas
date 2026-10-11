import { Injectable, inject } from '@angular/core';
import { Location } from '@angular/common';
import { Router } from '@angular/router';

// Back button for the public info pages: they can be opened from inside the app or
// landed on directly from a shared link, where there is no history to go back to.
@Injectable({ providedIn: 'root' })
export class InfoPageNav {
  private readonly location = inject(Location);
  private readonly router = inject(Router);

  back(): void {
    if (window.history.length > 1) {
      this.location.back();
    } else {
      this.router.navigate(['/groups']);
    }
  }
}
