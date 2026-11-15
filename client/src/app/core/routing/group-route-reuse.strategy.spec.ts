import { ActivatedRouteSnapshot, Route } from '@angular/router';

import { GroupRouteReuseStrategy } from './group-route-reuse.strategy';

function snapshot(routeConfig: Route, params: Record<string, string>): ActivatedRouteSnapshot {
  return { routeConfig, params } as unknown as ActivatedRouteSnapshot;
}

describe('GroupRouteReuseStrategy', () => {
  const strategy = new GroupRouteReuseStrategy();
  const group: Route = { path: 'groups/:groupId' };

  it('rebuilds the group screen when the group changes', () => {
    expect(strategy.shouldReuseRoute(snapshot(group, { groupId: '2' }), snapshot(group, { groupId: '1' }))).toBe(false);
  });

  it('keeps the screen for the same group and for routes without a group', () => {
    const shop: Route = { path: 'shop' };
    expect(strategy.shouldReuseRoute(snapshot(group, { groupId: '1' }), snapshot(group, { groupId: '1' }))).toBe(true);
    expect(strategy.shouldReuseRoute(snapshot(shop, {}), snapshot(shop, {}))).toBe(true);
  });
});
