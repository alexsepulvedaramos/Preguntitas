# Vaya Preguntita

> **Una pregunta al día sobre tu grupo de amigos.** Vote, discover who voted for whom, and let the friendly banter begin.

![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)
![Angular 22](https://img.shields.io/badge/Angular-22-DD0031?logo=angular&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-Supabase-316192?logo=postgresql&logoColor=white)
![Tailwind CSS 4](https://img.shields.io/badge/Tailwind_CSS-4-38B2AC?logo=tailwind-css&logoColor=white)
![PWA](https://img.shields.io/badge/PWA-installable-5A0FC8)
[![CI](https://github.com/alexsepulvedaramos/Preguntitas/actions/workflows/ci.yml/badge.svg)](https://github.com/alexsepulvedaramos/Preguntitas/actions/workflows/ci.yml)
[![Deploy Backend](https://github.com/alexsepulvedaramos/Preguntitas/actions/workflows/deploy-backend.yml/badge.svg)](https://github.com/alexsepulvedaramos/Preguntitas/actions/workflows/deploy-backend.yml)

**🎮 Live demo:** [vayapreguntita.vercel.app](https://vayapreguntita.vercel.app)
*(runs on free-tier hosting — the very first request may take up to a minute while the API wakes up)*

---

## What is it?

**Vaya Preguntita** is a Spanish-language social game for groups of friends, built end-to-end as a full-stack project. Every day, each group answers **one question about its own members** — it activates at the group's configured time and stays open for exactly 24 hours. Everyone votes, and the results show **who voted for what**: the whole point is to spark debate and *pique sano* (friendly rivalry).

Each day one rotating member becomes the **selector** for the next day's question: they can pick from the group's own pool, browse 15 thematic packs, or create a question on the spot. If they do nothing, the system has already auto-selected one — **there is always a question**.

## Screenshots

<p align="center">
  <img src="docs/screenshots/login.png" width="210" alt="Login" />
  <img src="docs/screenshots/groups.png" width="210" alt="Tus grupos" />
  <img src="docs/screenshots/group-detail.png" width="210" alt="Pregunta del día y resultados" />
  <img src="docs/screenshots/history.png" width="210" alt="Historial" />
</p>

## Features

- 📅 **Daily lifecycle engine** — a background worker preselects tomorrow's question and activates today's at each group's configured time, per group, with rotating selector turns.
- 🗳️ **Six question types**, each with its own voting UI and results visualization (see below).
- 👀 **Transparent results** — live aggregates including exactly who voted for whom, revealed after you vote.
- 💬 **Group chat** right under the daily question, with push notifications for new messages.
- 📦 **15 thematic question packs** (300+ curated questions in Spanish), toggleable per group, with clone-on-use templates and auto-resolution of member-dependent questions.
- 🔔 **Web Push notifications** (VAPID) — new question, your turn to pick, votes on your question; per-type preferences and per-group mute.
- 📱 **Installable PWA** — dark mode, pull-to-refresh, mobile-first layouts.
- 🕰️ **History archive** — browse any past day's question and results, cursor-paginated.
- 👑 **Group administration** — invite by code or link, kick members, transfer admin, configure the daily question time.
- 🔐 **JWT auth with rotating refresh tokens**, hashed at rest.

## Question types

| Type | What it asks | Results view |
|---|---|---|
| **Superlative** | "¿Quién del grupo es más probable que…?" — vote a member | Member ranking with voter avatars |
| **Deathmatch** | Team vs team showdown (2–4 teams, drag-and-drop builder) | Vote split per team |
| **Scale** | Rate a member — or anything — on a numeric range | Average headline + histogram |
| **Secret Pairing** | Match two members together | Combined pair ranking |
| **Custom Poll** | Classic poll, optional "Otro" free-text answer | Option ranking |
| **Open Text** | Open-ended question | Free-text response wall |

## Architecture

```mermaid
flowchart LR
    subgraph Vercel["Vercel · free tier"]
        A["Angular 22 PWA<br/>Signals · Tailwind 4 · Spartan UI"]
    end
    subgraph Render["Render · free tier"]
        B["ASP.NET Core API<br/>.NET 10 · EF Core · FluentValidation"]
        C["DailyPreselectionService<br/>background worker"]
    end
    subgraph Supabase["Supabase · free tier"]
        D[("PostgreSQL<br/>JSONB question metadata")]
    end
    A -- "REST + JWT" --> B
    B --- C
    B --> D
    B -. "Web Push (VAPID)" .-> A
```

**Free-tier-only by design.** The entire production stack (Vercel + Render + Supabase) runs on free tiers with zero budget for upgrades. That constraint shapes the engineering: bounded row growth, per-user/per-group quotas and abuse limits, polling before SignalR, and an idempotent seeder that keeps startup cheap.

## Repository structure

```
/
├── client/                        # Angular 22 PWA (standalone components, Signals)
├── server/
│   ├── VayaPreguntita.API/        # ASP.NET Core (.NET 10) REST API
│   ├── VayaPreguntita.API.Tests/  # xUnit domain + integration tests (Testcontainers)
│   └── Dockerfile                 # Render deployment image
├── docs/specs/vaya-preguntita.md  # Living spec — single source of truth
└── .github/workflows/             # CI on every PR; backend deploy on master
```

## Status & roadmap

The **MVP is feature-complete and live** (daily lifecycle, all question types, voting, results, history, group admin, notifications, thematic packs), and so is the post-MVP pass: public landing page and interactive demo, per-group voting streaks with animated avatar frames, versioned release notes, grouped push notifications, and core-domain tests with CI on every PR. Next up:

1. **Play in English & German** — UI and all 300+ seeded questions, culturally adapted rather than translated word for word, defaulting to the browser's language with a per-user override (ramas 23–24)
2. Real-time updates with SignalR instead of pull-to-refresh (rama 13, optional)

**Phase 2 backlog** includes richer in-question debate threads (evolving the shipped chat), Google OAuth, points & rankings, monthly stats, and email verification/password recovery. The full plan lives in the [spec §13/§16](docs/specs/vaya-preguntita.md).

## Engineering practices

- **Spec-driven development** — an 800-line [living spec](docs/specs/vaya-preguntita.md) is the single source of truth for behavior, data model, API contract, validation rules and the branch plan. Every feature branch updates it before the PR opens.
- **One branch per rama** (backend + its Angular UI), cut from `master`, Conventional Commits throughout.
- **English code, Spanish UI** — identifiers, comments and commits in English; all user-facing copy in Spanish.
- **Clean boundaries** — DTOs at the API edge (never EF entities), business logic in services only, FluentValidation for input, every schema change through an EF migration.
- **Tested where the domain is hardest** — the daily cycle runs against a real PostgreSQL (Testcontainers) at pinned instants through an injected `TimeProvider`: preselection and activation at the daily time, selector rotation, the 2-member start gate, pool → pack fallbacks, per-type vote validation, streaks, results aggregation and pool quotas. The client has Vitest specs.
- **CI/CD** — GitHub Actions builds and tests both stacks on every PR; pushes to `master` also deploy the backend to Render (the client deploys on Vercel).

## Getting started

| I want to… | Go to |
|---|---|
| Run the API locally | [server/README.md](server/README.md) |
| Run the Angular client locally | [client/README.md](client/README.md) |
| Understand the product & data model | [docs/specs/vaya-preguntita.md](docs/specs/vaya-preguntita.md) |

## License

All rights reserved — published for portfolio viewing and evaluation only. See [LICENSE](LICENSE).
