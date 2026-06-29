import { Injectable, inject, signal } from '@angular/core';
import { SwPush } from '@angular/service-worker';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';

const PROMPT_DISMISSED_KEY = 'notif_prompt_dismissed_at';
const DISMISS_COOLDOWN_DAYS = 7;

@Injectable({ providedIn: 'root' })
export class PushNotificationService {
  private readonly swPush = inject(SwPush);
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  readonly isSupported = this.swPush.isEnabled;
  readonly permission = signal<NotificationPermission>(
    typeof Notification !== 'undefined' ? Notification.permission : 'denied'
  );

  constructor() {
    this.swPush.notificationClicks.subscribe(({ notification }) => {
      const url: string | undefined = notification?.data?.url;
      if (url) this.router.navigateByUrl(url);
    });
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

  async requestAndSubscribe(): Promise<boolean> {
    if (!this.isSupported) return false;
    try {
      const { publicKey } = await firstValueFrom(
        this.http.get<{ publicKey: string }>(`${environment.apiUrl}/notifications/vapid-key`)
      );
      const sub = await this.swPush.requestSubscription({ serverPublicKey: publicKey });
      const raw = sub.toJSON() as { endpoint: string; keys: { p256dh: string; auth: string } };
      await firstValueFrom(
        this.http.post(`${environment.apiUrl}/notifications/subscriptions`, {
          endpoint: raw.endpoint,
          p256dh: raw.keys.p256dh,
          auth: raw.keys.auth,
        })
      );
      this.permission.set('granted');
      return true;
    } catch {
      this.permission.set(Notification.permission);
      return false;
    }
  }

  async unsubscribe(): Promise<void> {
    if (!this.isSupported) return;
    const sub = await firstValueFrom(this.swPush.subscription);
    if (!sub) return;
    const raw = sub.toJSON() as { endpoint: string; keys: { p256dh: string; auth: string } };
    try {
      await firstValueFrom(
        this.http.delete(`${environment.apiUrl}/notifications/subscriptions`, {
          body: { endpoint: raw.endpoint, p256dh: raw.keys.p256dh, auth: raw.keys.auth },
        })
      );
    } catch { /* ignore */ }
    await this.swPush.unsubscribe();
    this.permission.set('default');
  }
}
