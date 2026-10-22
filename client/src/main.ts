import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';

// iOS Safari ignores `user-scalable=no` in the viewport meta; cancelling its pinch gesture
// is the only way to keep zoom blocked there (developer's call, 2026-10-22).
document.addEventListener('gesturestart', (e) => e.preventDefault());

bootstrapApplication(App, appConfig)
  .catch((err) => console.error(err));
