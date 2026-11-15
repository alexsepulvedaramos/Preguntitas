import { ActivatedRouteSnapshot, BaseRouteReuseStrategy } from '@angular/router';

// Going from one group to another (a push of another group opened with the app already on a
// group screen) keeps the same route config, so the router would reuse the screen — which
// loads its group once, on init. A different `groupId` builds the screen again.
export class GroupRouteReuseStrategy extends BaseRouteReuseStrategy {
  override shouldReuseRoute(future: ActivatedRouteSnapshot, curr: ActivatedRouteSnapshot): boolean {
    return super.shouldReuseRoute(future, curr) && future.params['groupId'] === curr.params['groupId'];
  }
}
