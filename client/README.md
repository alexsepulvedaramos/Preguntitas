# Vaya Preguntita — Angular client

Angular 22 PWA for [Vaya Preguntita](../README.md). Standalone components, Signals, native control flow, Tailwind CSS 4 and [Spartan UI](https://www.spartan.ng/).

## Prerequisites

- Node.js 20+ and npm
- The API running locally (see [server/README.md](../server/README.md)) — `environment.development.ts` points to `http://localhost:5212/api`. To develop against the deployed API instead, temporarily set `apiUrl` to the production URL.

## Development

```bash
npm install
npm start        # ng serve → http://localhost:4200
```

## Tests & build

```bash
npm test         # unit tests (Vitest)
npm run build    # production build → dist/
```

## Structure & conventions

```
src/app/
├── core/        # auth (guards, interceptor), layout, shared services
├── features/    # auth, groups (detail/vote/results/history/settings), profile
└── shared/      # reusable UI components
```

- **UI copy is Spanish; code is English** (identifiers, comments, commits).
- **Design system** lives in `src/styles-lime.css` (Tailwind 4 `@theme` tokens: Sage + Electric Lime palette, `DM Serif Display` display font, class-based dark mode). Full rules in the [spec §14](../docs/specs/vaya-preguntita.md).
- **Spartan UI** provides the primitives (buttons, cards, dialogs, forms) — don't hand-roll what it already ships.
- **PWA**: service worker config in `ngsw-config.json`; web-push subscription handled by `PushNotificationService`.

Deployment is automatic: Vercel builds this folder on every push to `master`.
