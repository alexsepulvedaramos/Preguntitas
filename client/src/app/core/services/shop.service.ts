import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

import { BuyItemResponse, InventoryItem, PurchaseRequest, PurchaseResponse } from '../models/coin.model';
import { AuthService } from '../auth/auth.service';
import { environment } from '../../../environments/environment';

// Spending coins (§17.2). Rama 35 sells streak freezes (own or gift) and streak repairs in a
// group; rama 45 adds the item inventory (buy ahead, use later in any group). Every call also
// refreshes the wallet chip and the inventory counts.
@Injectable({ providedIn: 'root' })
export class ShopService {
  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthService);

  purchase(request: PurchaseRequest): Observable<PurchaseResponse> {
    return this.http
      .post<PurchaseResponse>(`${environment.apiUrl}/shop/purchase`, request)
      .pipe(tap((res) => this.authService.patchCurrentUser({ coins: res.balance, inventory: res.inventory })));
  }

  buyItem(item: InventoryItem): Observable<BuyItemResponse> {
    return this.http
      .post<BuyItemResponse>(`${environment.apiUrl}/shop/inventory`, { item })
      .pipe(tap((res) => this.authService.patchCurrentUser({ coins: res.balance, inventory: res.inventory })));
  }
}
