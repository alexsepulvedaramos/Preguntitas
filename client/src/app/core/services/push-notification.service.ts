import { Injectable, computed, effect, inject, signal, untracked } from '@angular/core';
import { SwPush } from '@angular/service-worker';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { firstValueFrom, timeout } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from '../auth/auth.service';

const PROMPT_DISMISSED_KEY = 'notif_prompt_dismissed_at';
// Set when the user explicitly turns push off on this device, so the startup
// sync doesn't silently re-subscribe them (the browser permission stays 'granted').
const OPTED_OUT_KEY = 'notif_push_opted_out';
const DISMISS_COOLDOWN_DAYS = 7;

@Injectable({ providedIn: 'root' })
export class PushNotificationService {
  private readonly swPush = inject(SwPush);
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);

  readonly isSupported = this.swPush.isEnabled;
  readonly permission = signal<NotificationPermission>(
    typeof Notification !== 'undefined' ? Notification.permission : 'denied'
  );
  // Whether this browser currently holds a push subscription (source: the service worker).
  private readonly hasSubscription = signal(false);
  // What the UI shows as "on": permission granted AND an actual push subscription exists.
  // Permission alone isn't enough — a granted browser can have no subscription at all.
  readonly enabled = computed(() => this.permission() === 'granted' && this.hasSubscription());

  private syncedForUserId: number | null = null;

  constructor() {
    this.swPush.notificationClicks.subscribe(({ notification }) => {
      const url: string | undefined = notification?.data?.url;
      if (url) this.router.navigateByUrl(url);
    });

    if (!this.isSupported) return;

    this.swPush.subscription.subscribe((sub) => this.hasSubscription.set(!!sub));

    // On app start / login: make sure the server holds this device's subscription.
    effect(() => {
      const user = this.auth.currentUser();
      if (!user) {
        // Logged out: the next login (even as the same user) must sync again.
        this.syncedForUserId = null;
        return;
      }
      if (user.id === this.syncedForUserId) return;
      this.syncedForUserId = user.id;
      untracked(() => void this.syncSubscription());
    });

    // Opening the app clears every notification it has pending — tapping one of
    // several only closes that one otherwise.
    void this.clearDeliveredNotifications();
    document.addEventListener('visibilitychange', () => {
      if (document.visibilityState === 'visible') void this.clearDeliveredNotifications();
    });
  }

  // Logout on this device: the server forgets the subscription so the account stops
  // receiving notifications here. The browser subscription and permission are kept —
  // the next account to log in takes them over through the startup sync.
  async detachDevice(): Promise<void> {
    if (!this.isSupported) return;
    try {
      const sub = await firstValueFrom(this.swPush.subscription);
      if (!sub) return;
      await firstValueFrom(
        this.http
          .delete(`${environment.apiUrl}/notifications/subscriptions`, { body: this.toDto(sub) })
          .pipe(timeout(5000))
      );
    } catch {
      // Best effort: logout must never get stuck here.
    }
  }

  private async clearDeliveredNotifications(): Promise<void> {
    try {
      const registration = await navigator.serviceWorker?.getRegistration();
      const notifications = (await registration?.getNotifications()) ?? [];
      notifications.forEach((n) => n.close());
    } catch { /* not supported: nothing to clear */ }
  }

  get shouldShowPrompt(): boolean {
    if (!this.isSupported) return false;
    if (this.permission() !== 'default') return false;
    const dismissed = localStorage.getItem(PROMPT_DISMISSED_KEY);
    if (!dismissed) return true;
    const days = (Date.now() - Number(dismissed)) / 86_400_000;
    return days >= DISMISS_COOLDOWN_DAYS;
  }

  dismissPrompt() {
    localStorage.setItem(PROMPT_DISMISSED_KEY, String(Date.now()));
    this.permission.set(Notification.permission);
  }

  // User-initiated: asks for permission if needed, subscribes and registers with the server.
  async requestAndSubscribe(): Promise<boolean> {
    if (!this.isSupported) return false;
    try {
      await this.subscribeAndRegister();
      localStorage.removeItem(OPTED_OUT_KEY);
      return true;
    } catch (err) {
      console.warn('Push subscription failed', err);
      return false;
    } finally {
      this.permission.set(Notification.permission);
    }
  }

  async unsubscribe(): Promise<void> {
    localStorage.setItem(OPTED_OUT_KEY, '1');
    if (!this.isSupported) return;
    const sub = await firstValueFrom(this.swPush.subscription);
    if (sub) {
      try {
        await firstValueFrom(
          this.http.delete(`${environment.apiUrl}/notifications/subscriptions`, {
            body: this.toDto(sub),
          })
        );
      } catch { /* ignore */ }
      await this.swPush.unsubscribe();
    }
    this.hasSubscription.set(false);
  }

  // Background re-sync. Runs silently when permission is already granted: re-posts the
  // existing subscription (the server may have dropped it) or creates a new one (the
  // browser may have lost it). Never prompts — with permission granted there's no dialog.
  private async syncSubscription(): Promise<void> {
    this.permission.set(Notification.permission);
    if (this.permission() !== 'granted' || localStorage.getItem(OPTED_OUT_KEY)) return;
    try {
      await this.subscribeAndRegister();
    } catch (err) {
      console.warn('Push subscription sync failed', err);
    }
  }

  private async subscribeAndRegister(): Promise<void> {
    const { publicKey } = await firstValueFrom(
      this.http.get<{ publicKey: string }>(`${environment.apiUrl}/notifications/vapid-key`)
    );

    let sub = await firstValueFrom(this.swPush.subscription);
    // A subscription created with a different VAPID key is rejected by the push service.
    if (sub && !this.matchesKey(sub, publicKey)) {
      await sub.unsubscribe();
      sub = null;
    }
    sub ??= await this.swPush.requestSubscription({ serverPublicKey: publicKey });

    await firstValueFrom(
      this.http.post(`${environment.apiUrl}/notifications/subscriptions`, this.toDto(sub))
    );
    this.hasSubscription.set(true);
  }

  private matchesKey(sub: PushSubscription, publicKey: string): boolean {
    const key = sub.options?.applicationServerKey;
    if (!key) return true; // can't tell — keep it
    const bytes = new Uint8Array(key);
    let binary = '';
    for (const b of bytes) binary += String.fromCharCode(b);
    const base64Url = btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
    return base64Url === publicKey.replace(/=+$/, '');
  }

  private toDto(sub: PushSubscription) {
    const raw = sub.toJSON() as { endpoint: string; keys: { p256dh: string; auth: string } };
    return { endpoint: raw.endpoint, p256dh: raw.keys.p256dh, auth: raw.keys.auth };
  }
}
