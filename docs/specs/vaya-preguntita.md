# Vaya Preguntita — Unified Technical & Agent Guide

> **Purpose:** Single source of truth for developers and AI coding agents (Claude, Copilot, etc.).
> **Last updated:** 6 October 2026 · **Status:** MVP feature-complete; post-MVP bug-fix & optimization pass in progress (§13; ramas 14, 15, 19, 20 done).
> **Language policy:** This document and all code/comments are written in English. UI-facing copy is Spanish (it is a Spanish-language app).

---

## Table of Contents

1. [Product Overview](#1-product-overview)
2. [Architecture Stack](#2-architecture-stack)
3. [Repository Structure](#3-repository-structure)
4. [The Daily Lifecycle (Core Loop)](#4-the-daily-lifecycle-core-loop)
5. [Question Types — Complete Spec](#5-question-types--complete-spec)
6. [The Base Pack & Thematic Packs](#6-the-base-pack--thematic-packs)
7. [Data Models & Key DTOs](#7-data-models--key-dtos)
8. [API Endpoint Reference](#8-api-endpoint-reference)
9. [Validation Rules](#9-validation-rules)
10. [Real-time Strategy](#10-real-time-strategy)
11. [Limits & Abuse Protection](#11-limits--abuse-protection)
12. [Current Status](#12-current-status)
13. [Roadmap & Branch Plan](#13-roadmap--branch-plan)
14. [Project Conventions & Git Guidelines](#14-project-conventions--git-guidelines)
15. [Agent Instructions](#15-agent-instructions)

---

## 1. Product Overview

**Vaya Preguntita** is a Spanish-language social game for groups of friends. Every day each group answers **one** question *about its own members*, and members can see **who voted for whom** — the point is to spark debate, banter, and *pique sano* (friendly rivalry).

**Core mechanics:**

- A user authenticates, lands on the home screen, and sees the groups they belong to. They can create a group or join one with an invitation code.
- Inside a group, there is **one daily question**. It becomes active at the group's configured time (`DailyQuestionTime`) and stays open for **exactly 24 hours**, until the next day's question takes over.
- Each day, **one rotating member is the *selector*** for the *following* day's question. They may pick a question from the group's pool, pick from the global **base pack**, or create a new one on the spot. If they do nothing, the system has already auto-selected one.
- All members vote on the active daily question (the UI adapts to the question type). After voting, the member sees **live aggregated results, including who voted for what**.
- A **history** view lets any member browse past days and see the question + results for any date, whether or not they voted that day.
- Group admins can edit the group's name, description, and daily time, manage members, and toggle which thematic packs feed the rotation (rama 15, §6.5).

**North-star (Phase 2):** an in-question **chat/debate thread** — generating conversation is the real goal of the game.

---

## 2. Architecture Stack

### 2.1 Frontend (The Client)

| Property | Value |
|---|---|
| Framework | Angular v19+ (standalone components, Signals, native control flow) |
| Language | TypeScript |
| UI Library | **Spartan UI** (shadcn/ui port). An official Spartan skill is available at `agents/skills/spartan` — use it when building UI. |
| Styling | Tailwind CSS v4 (`postcssrc.json` + `@theme` block in `styles.css`) |
| Icons | `ng-icon` |
| Hosting | **Vercel (free tier)** |

**Design System:**
- Colors: Sage (primary) + Electric Lime accent + Warm Sand
- Typography: `DM Serif Display` italic (display headings) + `DM Sans` (UI text)
- Dark mode: class-based (`:root.dark`)
- Logo: `LogoComponent` (`sm` | `lg`) + `LogoWordmarkComponent`
- Full rules (tokens, states, accessibility) — see [§14 Frontend Design System & Style Guide](#frontend-design-system--style-guide)

**Spartan UI patterns:**
- Native elements with directive decorators: `<button hlmBtn>`
- Element-selector components: `<hlm-card>`
- Consolidated import arrays: `Hlm*Imports`
- Build UI with the Spartan skill at `agents/skills/spartan`; do not hand-roll primitives that Spartan already provides.

### 2.2 Backend (The Server)

| Property | Value |
|---|---|
| Framework | **ASP.NET Core (.NET 10) Web API** |
| Language | C# |
| Pattern | MVC — RESTful JSON; controllers orchestrate, services hold logic |
| ORM | Entity Framework Core (Code-First) |
| Validation | FluentValidation |
| Object Mapping | AutoMapper |
| Background work | `BackgroundService` (`DailyPreselectionService`, ticks every minute) |
| Hosting | **Render (free Web Service)** + UptimeRobot keep-alive |

### 2.3 Data Layer (The Memory)

| Property | Value |
|---|---|
| Database | PostgreSQL |
| Hosting | **Supabase (free tier, Europe region)** |
| JSONB | `Question.Metadata` (owned type) |

### 2.4 Production Constraint — Free Tiers Only

The entire production stack runs on **free tiers (Render + Vercel + Supabase)** and there is **no budget for paid upgrades**. Every design decision must respect this:

- Keep DB rows, queries, and storage modest; avoid unbounded growth.
- Enforce the limits in [§11](#11-limits--abuse-protection) to prevent abuse from exhausting free quotas.
- Prefer cheap mechanisms (polling/refresh button) before heavier infra (SignalR) — see [§10](#10-real-time-strategy).

---

## 3. Repository Structure

```
/
├── agents/skills/spartan/                # Official Spartan UI skill (use for frontend)
├── docs/specs/
│   └── vaya-preguntita.md                # THIS FILE — single source of truth
├── server/
│   ├── VayaPreguntita.slnx
│   └── VayaPreguntita.API/
│       ├── Controllers/                  # Auth, Groups, Daily, Questions
│       ├── Services/                     # GroupsService, DailyService, QuestionsService, JwtTokenService
│       ├── BackgroundServices/           # DailyPreselectionService
│       ├── Entities/                     # EF Core entities
│       ├── DTOs/                         # API boundary objects (Auth, Groups, Daily, Questions)
│       ├── Enums/                        # QuestionType, QuestionSource, VoteResult, SelectResult
│       ├── Helpers/                      # QuestionMetadataBuilder, ResultsBuilder
│       ├── Profiles/                     # AutoMapper (MappingProfile, MetadataResolver)
│       ├── Validators/                   # FluentValidation
│       ├── Migrations/
│       └── Program.cs
└── client/                               # Angular v19+ app
    ├── src/app/
    │   ├── core/                         # auth, layout, services, models, enums
    │   ├── features/                     # auth, groups, questions, (daily → to build)
    │   └── shared/
    └── styles.css                        # Tailwind @theme block (design tokens)
```

**Key rule:** Question-type rules live in `Question.Metadata` (JSONB). Never put question-type logic in controller bodies — delegate to services, validators, and the static helpers.

---

## 4. The Daily Lifecycle (Core Loop)

This is the heart of the app. Read it carefully — the previous implementation activated manually-selected questions *immediately*, which is **wrong**.

### 4.1 Golden rule — the daily time is sacred

A question **always activates exactly at the group's `DailyQuestionTime` (T)** and closes 24 h later, at the next T. This guarantees every question gets a full, equal 24-hour voting window. **Nothing — not even a manual selection — may activate a question earlier than T.**

**Bootstrap exception:** the very *first* question of a brand-new group is the one case that activates immediately instead of waiting for T — when a group reaches 2 members (`GroupsService.JoinGroupAsync`), that question goes live right away so members aren't stuck for up to 24h with nothing to do. It still *closes* at the next real T (today's, if it hasn't passed yet — otherwise tomorrow's), not a full 24h later, so the normal cadence resumes at the very next T instead of skipping a cycle. The next cycle's question is preselected normally for that same T (`PreselectForGroupAsync` is called a second time, dated for the real next-T day; the bootstrap entry itself is dated one day earlier so the two don't collide on the `(GroupId, Date)` key — `CalculateSelector`'s day-count math is adjusted to tolerate that). From that point on every cycle is again a full, equal T-to-T window. This fires once per group, never on a later selection.

> Time zone for the MVP is fixed at **UTC+2** (Spain). A nullable `Group.TimeZoneId` column (default `Europe/Madrid`) is added **now** for forward-compatibility, but MVP logic ignores it and uses UTC+2. Per-group IANA time-zone *logic* is Phase 2.

### 4.2 Selection is for the *next* day

The rotating **selector** chooses the question that will activate at the **next** T — i.e. they pick *tomorrow's* question during *today's* 24-hour window. They are never picking the question that is currently being voted on.

### 4.3 Timeline (per group, once it has ≥ 2 members)

```
          T (day D)                         T (day D+1)                      T (day D+2)
            │                                  │                                │
   ┌────────┴───────────────────────┐ ┌────────┴───────────────────────┐ ┌─────┴──────
   │  Question D is ACTIVE           │ │  Question D+1 is ACTIVE         │ │ ...
   │  → members vote                 │ │  → members vote                 │
   │                                 │ │                                 │
   │  Selector(D+1) picks Question   │ │  Selector(D+2) picks Question   │
   │  D+1 (pool / base / new);       │ │  D+2; until T(D+2) it can       │
   │  until T(D+1) it can change.    │ │  change.                        │
   └─────────────────────────────────┘ └─────────────────────────────────┘
```

At each tick of `DailyPreselectionService`:

1. **Activate** any `DailyEntry` for *today* whose `ActivatedAt == null` and whose group time T has now passed: set `ActivatedAt` and `Question.DateActivated`. (Dynamic base-pack fields were already resolved at clone/instantiation time — see §6.3 — so activation does not re-resolve them.)
2. **Preselect** the *next* day for any group that has ≥ 2 members and no entry yet for that date: pick the next selector, auto-select a question (group pool first, else base pack), create a `DailyEntry` with `ActivatedAt == null`, `IsAutoSelected = true`.

### 4.4 The selector

```csharp
var ordered = members.OrderBy(m => m.JoinedAt).ToList();
var selector = ordered[daysSinceGroupCreation % ordered.Count];
```

- The creator joins first (`JoinedAt` earliest), so they are the **first selector**.
- MVP keeps this simple modulo rotation. It can skip/repeat a member when the roster changes — acceptable for MVP. **Fair rotation (turn pointer / per-member turn counter) is Phase 2.**

### 4.5 There is always a question

Once a group has ≥ 2 members, **every day has a question** — the base pack (§6) is the guaranteed fallback when the user pool is empty. `no_question` is therefore **not** a normal in-game state; it only represents the pre-start condition (group has < 2 members, no cycle yet).

### 4.6 Voting rules

- A member votes **once** per question; the vote is **final** (no changes in the MVP — changing a vote via coins / written justification is Phase 2).
- **No late voting:** once a question closes at T, it is frozen.
- Results are **not anonymous** — the breakdown of who voted for what/whom is shown (`VoterDto`, `OptionResultDto.TargetUser`).
- After a member votes, they see the **live aggregate** (refresh button in MVP; SignalR push in Phase 2). Results of a still-open question are visible to members who have already voted.
- **Result visibility model:** on the *live* day you must vote to see results (drives participation). The **history archive is open** in the MVP — any member sees any past day's results, voted or not. *(Phase 2: gate history behind participation, allowing a **late vote** to unlock — pairs with the coin system; must decide whether late votes count in the tally. See §16.)*
- The selector/creator votes too, as a normal member.
- Self-votes are allowed unless the creator blacklisted the user (Superlative).

### 4.7 `daily/current` response shape (redesigned)

The endpoint must surface **both** the voting state (today) and the selection state (tomorrow), because they coexist:

```jsonc
{
  "today": {
    "status": "voting" | "results" | "no_question",
    "question": QuestionToVoteDto | null,
    "userHasVoted": false,
    "results": QuestionResultDto | null,   // present when userHasVoted
    "closesAt": "2026-06-23T10:00:00Z"      // next T
  },
  "selection": {
    "date": "2026-06-23",                   // the day being selected (next T)
    "activatesAt": "2026-06-23T10:00:00Z",
    "selectorUserId": 42,
    "selectorUsername": "alex",
    "isCurrentUserSelector": true,          // drives the "te toca elegir" indicator
    "pendingQuestion": QuestionToVoteDto | null,  // ONLY when isCurrentUserSelector — see note
    "isAutoSelected": true
  }
}
```

`selection` is `null` before the group has started a cycle (< 2 members). The whole flow is
driven off `DailyEntry.ActivatedAt` state, not the calendar date: `today` reflects the most
recently **activated** entry (so it stays correct across the T boundary), and `selection`
reflects the next, not-yet-activated entry. `today.closesAt` always equals
`selection.activatesAt` (the next T).

**`pendingQuestion` visibility:** the next-day question's full content is returned **only to
the selector** (`isCurrentUserSelector == true`). Other members see who the selector is and
whether something is auto-selected, but not the question text — preserving the surprise until
it activates at T.

The Angular `DailyComponent` uses `@switch` on `today.status` to render the voting/results sub-component, and shows the selection panel when `selection.isCurrentUserSelector` is true. A `CountdownComponent` renders `today.closesAt` as a live "siguiente pregunta en…" label so the next T is always visible, including right after the bootstrap exception (§4.1) when the first cycle's window isn't a full 24h.

### 4.8 Changing the daily time mid-cycle (rama 20)

An admin can edit `Group.DailyQuestionTime` at any moment. Because `closesAt`/`activatesAt` are always computed from the group's **live** `DailyQuestionTime` (never a value cached on the `DailyEntry`), the new time is honoured the moment it is saved. Its effect depends on whether **today's** question has already activated:

- **Today's question has *not* activated yet** (the pending entry is dated today): it activates at the **new** T. The time is sacred (§4.1) — it still never activates *before* the new T — but the new T is what it waits for, so moving the time simply reschedules today's still-pending question. This already falls out of the live computation; no special handling.
- **Today's question has *already* activated** (today's slot is consumed; the pending entry is dated tomorrow): the change applies **from the next cycle**. We do **not** re-anchor the current cycle or close the open question early — that would cut its 24-hour window short and drop the votes of members who hadn't voted yet (§4.6, no late voting). So the countdown legitimately shows "1d …h" to the next T at the new time.

To keep this transparent, `PUT /groups/{id}` returns `GroupResponse.DailyTimeChangeAppliesFromTomorrow` — `true` only when the time changed **and** a question has already activated during today's local (UTC+2) day (measured off `ActivatedAt`, not the entry's `Date`, so the bootstrap entry still counts). The settings screen shows an informational toast — *"El nuevo horario se aplicará a partir de mañana…"* — in that case.

---

## 5. Question Types — Complete Spec

All six types are **in MVP scope** (UI + backend). Metadata is stored in the `Question.Metadata` JSONB column.

### 5.1 The Superlative (Group Poll) — *dynamic*

> *"¿Quién es más probable que acabe en la cárcel?"* → pick 1 person.

| Metadata | Type | Description |
|---|---|---|
| `AllowNobody` | `bool` | If `true`, inject a "Nadie" option (sentinel id `0`) |
| `BlacklistedUserIds` | `List<int>` | Users excluded from being picked |

- **Vote:** `{ "SelectedTargetUserId": 42 }` · `0` = "Nadie" (only when `AllowNobody`).
- **Validation:** reject if the target is blacklisted. Members are resolved dynamically — never store names at creation.

### 5.2 The Deathmatch (Team vs Team) — *static (or auto-resolved for base pack)*

> *"Pelea a muerte: [Juan y Marta] vs [Luis y Ana]. ¿Quién gana?"*

| Metadata | Type | Description |
|---|---|---|
| `Teams` | `List<List<int>>` | One inner list of user IDs per team (stored via `HasConversion` ↔ JSONB) |

- **Vote:** `{ "SelectedTargetUserIds": [1, 2] }` — must match exactly one configured team.
- **Validation:** a user cannot appear in more than one team; ≥ 2 teams of ≥ 1 member.
- **Base-pack variant:** teams are auto-generated from a random split of the group's members at activation (§6.3).

### 5.3 The Scale (Subjective Rating) — *numeric, rates a person or any subject*

> *"Del 1 al 10, ¿cómo de 'rayao' está hoy el Aguacate?"* · *"¿Qué nota le pones a Titanic?"*

| Metadata | Type | Default |
|---|---|---|
| `TargetUserId` | `int?` | — (the person being rated; **optional** — when null the subject is just the question text) |
| `RangeMin` | `int` | 1 |
| `RangeMax` | `int` | 10 |

- **Vote:** `{ "NumericValue": 7 }` — a raw integer in `[RangeMin, RangeMax]`, **not** an option id.
- **Target is optional:** a Scale can rate a group member (`TargetUserId` set) *or* any free subject described by the text (`TargetUserId` null). The create form offers a "¿Es sobre alguien del grupo?" toggle.
- **Base-pack variant:** when a template defines a target, `TargetUserId` is auto-assigned to a random member at activation (§6.3).

### 5.4 The Secret Pairing (Matchmaking) — *dynamic*

> *"¿Qué dos personas del grupo harían mejor pareja?"*

| Metadata | Type | Value |
|---|---|---|
| `MinSelections` | `int` | 2 |
| `MaxSelections` | `int` | 2 |

- **Vote:** `{ "SelectedTargetUserIds": [12, 37] }` — exactly 2 distinct active members.

### 5.5 The Custom Poll (Classic Poll) — *static*

> *"¿A qué hora quedamos para cenar?"*

| Metadata | Type | Description |
|---|---|---|
| `Options` | `Option` rows | Stored as `Option` entities (not in metadata) |
| `MinSelections` | `int` | 1 = single-select |
| `MaxSelections` | `int` | > 1 = multi-select (**MVP supports multi-select**) |
| `AllowOther` | `bool` | If `true`, voters may add their own free-text answer ("Otro") |

- **Vote:** `{ "SelectedOptionIds": [3] }` — count must be in `[MinSelections, MaxSelections]`; every id must belong to the question.
- **"Otro" answer:** when `AllowOther`, the vote may also carry `{ "FreeText": "..." }`. The free-text answer counts as one selection toward `[MinSelections, MaxSelections]`. Free-text answers are surfaced in results under `FreeTextResponses` (who wrote what), separate from the option bars.

### 5.6 The Open Text (Open-ended) — *free-text*

> *"¿Cuál es tu mejor recuerdo del viaje?"*

- No structured metadata — just the question text.
- **Vote:** `{ "FreeText": "..." }` — a non-empty free-text answer (≤ 280 chars), stored in `Vote.FreeText`.
- **Results:** the list of `FreeTextResponses` (`{ username, text }`), not aggregate bars.

### Summary

| Type | Nature | Vote field | Constraint |
|---|---|---|---|
| Superlative | Dynamic | `SelectedTargetUserId` | Blacklist check; `0` = Nadie |
| Deathmatch | Static / auto | `SelectedTargetUserIds` | Must match one exact team |
| Scale | Static / auto | `NumericValue` | Integer in `[RangeMin, RangeMax]`; target optional |
| Secret Pairing | Dynamic | `SelectedTargetUserIds` | Exactly 2 ids |
| Custom Poll | Static | `SelectedOptionIds` (+ optional `FreeText`) | Count in `[MinSelections, MaxSelections]`; `FreeText` only when `AllowOther` |
| Open Text | Static | `FreeText` | Non-empty, ≤ 280 chars |

---

## 6. The Base Pack & Thematic Packs

### 6.1 Why it exists

Requirement: **there must always be a question.** The **base pack** is a global, always-on set of seed questions (several of every type) that any group can draw from. It is the fallback for auto-selection and an extra source the selector can pick from.

### 6.2 Model — global template + clone-on-use (MVP)

- **`Pack`** — a named collection of templates. Seeds the always-on **"Base"** pack plus 14 thematic packs (rama 15, §6.5). `IsActiveByDefault` is the global master switch; per-group enable/disable lives in `GroupDisabledPack` (§6.5).
- **`QuestionTemplate`** — a global question *template* (no votes, no group): `Text`, `Type`, type config, optional template options, `PackId`. Templates are **never voted on directly**.
- When a template is chosen for a group's day (auto or manual), the system **clones** it into a concrete `Question` row inside that group (`GroupId` set, `Source = Pack`, `CreatorId = null`). That `Question` holds the votes. This keeps votes isolated per group and `Question.GroupId` non-nullable.

> Schema change: `Question.CreatorId` becomes **nullable** (`null` ⇒ came from a pack, no human author).

**Creator display convention:** the backend keeps `CreatorId = null` for pack-sourced questions and exposes it as-is. The **frontend** maps that `null` to a label such as *"Vaya Preguntita"* / *"El equipo de Vaya Preguntita"* instead of showing it empty. (Rendered in the UI ramas 5/7/8; the field stays genuinely nullable.)

### 6.3 Auto-resolution of dynamic base questions

Superlative, Secret Pairing, and Custom Poll templates are self-contained. **Scale and Deathmatch are not** (they need a target person / teams). For base-pack instances these are resolved **automatically at clone/instantiation time** from the group's current active members:

- **Scale:** `TargetUserId` ← a random active member.
- **Deathmatch:** `Teams` ← a random split of active members into 2 teams (sizes balanced).

Resolution uses the membership snapshot at the moment of instantiation (clone time), which is why minor staleness (a member joining before T) is accepted in the MVP. Implemented in `Helpers/TemplateCloner.cs`; the seed packs are loaded at startup by `Data/PackSeeder.cs` (idempotent **per template** — find-or-create each pack by `Name` and each template by `(PackId, Text)`, additive only).

### 6.4 Selection sources

When the selector opens the picker they can choose from: (a) the group's **user-created pool** (`!IsUsed`), and (b) **templates from the group's enabled packs** (§6.5). Or they create a brand-new question inline. Both lists are **cursor-paginated** with a shared type filter and a pack filter — `GET /groups/{id}/questions/pool` (`QuestionPageDto`) and `GET /groups/{id}/packs/templates` (`PackTemplatePageDto`), each `{ Items, HasMore }`, page size default 12 / max 50, `before` cursor on the last seen `Id`. The pool list renders above the pack list to preserve user-pool priority (§6.6). *(This replaced the old unpaginated `GET /daily/selection-sources`, now removed.)*

### 6.5 Thematic packs & per-group enable/disable (rama 15)

Beyond **Base**, 14 thematic packs ship seeded and active by default: Humor negro, Supervivencia y apocalipsis, Vida nocturna y resaca, Dilemas, Citas/relaciones/red flags, Crimen y misterio, Comida, Deportes y competencia, Polémicas y bandos, Confesiones y vergüenzas, Nostalgia y cringe, Hipotéticos, El reparto del grupo, and Guarradas y dilemas asquerosos.

A **`GroupDisabledPack`** row (composite key `GroupId, PackId`) marks a pack disabled for a group; absence means enabled. `Pack.IsActiveByDefault` is the global master switch; effective per-group enablement is `pack.IsActiveByDefault && !GroupDisabledPacks.Any(GroupId, PackId)`. `PacksController` at `api/groups/{groupId}/packs`:
- `GET /` — packs with `{ id, name, description, enabled }` for the group (any member).
- `PATCH /{packId}` — `{ enabled }`, **admin-only**; rejected (400) if disabling would leave the group with **zero** enabled packs (§6.1 invariant).

Admin UI: a per-pack switch list in `GroupSettingsComponent` (optimistic update, revert on error).

### 6.6 Template reuse policy (rama 15)

Replaces the old time-based cooldown. **A template is only ever reused when the group is exhausted** — there is no unused pool question *and* no never-used template across the group's enabled packs. Per group, `usedTemplateIds = Questions.Where(GroupId, TemplateId != null).Select(TemplateId)`; a template is *available* if its `Id` ∉ `usedTemplateIds` and its pack is enabled. Shared in `Helpers/TemplateReusePolicy.cs` across all three call sites: the picker templates list (only available ones; once exhausted, all enabled-pack templates become browsable again, least-recently-used first via `DailyEntry.ActivatedAt`), selection validation (`ResolveSelectedQuestionAsync` → `SelectResult.TemplateAlreadyUsed` unless exhausted), and the preselection fallback. Pool-first priority is unchanged: auto-preselection draws from the unused user pool while any remains, cloning a pack template only when the pool is empty.

### 6.7 Phase 2

Pack-aware preselection weighting, and a bundled **"Reparto/Casting"** question type that assigns a whole cast (N characters → N members) in a single question — today the reparto pack uses individual Superlatives instead (§16).

---

## 7. Data Models & Key DTOs

### 7.1 Entities (current + planned)

- **User** — `Id, Username, Email, PasswordHash, AvatarUrl, FrameColor, StreakFrameAutoApplied, DateJoined`. `FrameColor` is a hex colour, `"streak"` (ring follows the streak tier — the default; `null` is read as `"streak"`) or `"none"` (rama 19). Refresh tokens live in `UserRefreshToken`, not on `User` (multi-device support).
- **UserRefreshToken** — `Id, UserId, TokenHash, ExpiresAt, CreatedAt`. One row per device/session; up to 5 concurrent sessions per user (least recently used — earliest `ExpiresAt` — evicted on new login). `Jwt:RefreshTokenDays` = 365, **sliding**: every refresh pushes `ExpiresAt` forward, so a device only has to log in again after a year without opening the app. The client only ends a session when `/refresh` answers 400/401; network errors, timeouts and 5xx (e.g. during a Render redeploy) keep the stored tokens.
- **Group** — `Id, Name, Description, InvitationCode, DailyQuestionTime, TimeZoneId (nullable, default 'Europe/Madrid'; MVP logic uses UTC+2), DateCreated, CreatorId`, plus `Members`, `Questions`, `DailyEntries`. **Names are not unique** (labels only). `AdminId` was removed in rama 10 — admin identity lives on `GroupMember.IsAdmin`.
- **GroupMember** — composite key `(GroupId, UserId)`, `JoinedAt` (drives rotation), `IsAdmin` (bool; exactly one member per group is admin at all times), `NotificationsMuted`, and the rama 19 streak columns `CurrentStreak, BestStreak, LastStreakEntryId, StreakDangerNotifiedEntryId`. The stored `CurrentStreak` is only *effective* while `LastStreakEntryId` is the open entry or the previously activated one (`StreakService`).
- **Question** — `Id, Text, Type, Source, IsUsed, DateCreated, DateActivated, Metadata (JSONB), GroupId, CreatorId (→ nullable), Options, Votes`.
- **Option** — poll option (`Id, Text, QuestionId`).
- **Vote** — `Id, DateResponded, QuestionId, UserId`, and the polymorphic answer fields: `SelectedOptionId`, `SelectedTargetUserId`, `NumericValue`, `FreeText` (used by Open Text answers and Custom Poll "Otro" answers). Unique index `(QuestionId, UserId)`.
- **DailyEntry** — `Id, Date, IsAutoSelected, PreselectedAt, ActivatedAt (nullable), GroupId, QuestionId, SelectorUserId`. Unique index `(GroupId, Date)`. `SelectorUserId` is set at preselection time and is the authoritative source for who may call `POST /daily/select` — the backend uses the stored value instead of re-running `CalculateSelector` at selection time, so member rejoins (which change `JoinedAt` and shift the rotation) don't break the selector authorization check.
- **Pack** — `Id, Name, Description, IsActiveByDefault`. Seeds **Base** + 14 thematic packs (rama 15).
- **QuestionTemplate** — `Id, Text, Type, Metadata, PackId`, optional template options. Global, voteless.
- **GroupDisabledPack** *(new, rama 15)* — composite key `(GroupId, PackId)`. Presence = the pack is disabled for that group; absence = enabled. Effective enablement = `Pack.IsActiveByDefault && !GroupDisabledPacks.Any(GroupId, PackId)`.

### 7.2 `QuestionMetadata` (owned type → JSONB)

```csharp
public class QuestionMetadata
{
    // Superlative
    public bool AllowNobody { get; set; }
    public List<int> BlacklistedUserIds { get; set; } = [];
    // Scale
    public int? RangeMin { get; set; }
    public int? RangeMax { get; set; }
    public int? TargetUserId { get; set; }
    // Secret Pairing / Custom Poll
    public int MinSelections { get; set; } = 1;
    public int MaxSelections { get; set; } = 1;
    // Deathmatch (HasConversion ↔ JSONB)
    public List<List<int>> Teams { get; set; } = [];
}
```

### 7.3 `CreateVoteDto` (polymorphic)

```csharp
public class CreateVoteDto
{
    public int? SelectedTargetUserId { get; set; }        // Superlative
    public List<int>? SelectedTargetUserIds { get; set; } // Deathmatch, Secret Pairing
    public List<int>? SelectedOptionIds { get; set; }     // Custom Poll
    public int? NumericValue { get; set; }                // Scale
}
```

### 7.4 Results DTOs (who voted for what)

`ResultsBuilder.Build(question, votes, mapper, usersById?)` produces, per option/target/team/value:

```csharp
public class QuestionResultDto
{
    public QuestionType Type { get; set; }
    public int TotalVotes { get; set; }            // distinct voters (UserId), not Vote rows
    public List<OptionResultDto> Results { get; set; }
}

public class OptionResultDto
{
    public int Id { get; set; }
    public string DisplayText { get; set; }
    public UserDto? TargetUser { get; set; }
    public List<UserDto> TeamMembers { get; set; } // Deathmatch only
    public int VoteCount { get; set; }
    public List<VoterDto> Voters { get; set; }  // id + username — who voted here
    public double Percentage { get; set; }
}
```

Per-type build rules (rama 7, `feat/results-view`):
- **`TotalVotes`** counts distinct `UserId`s, not `Vote` rows — CustomPoll (multi-select) and Secret Pairing both write more than one `Vote` row per logical user vote.
- **CustomPoll:** one row per option, **always all options including zero-vote ones** (rama 15 — matches Deathmatch/Scale, which already show every option/value; an unvoted option is information, not clutter); sorted by `VoteCount` descending.
- **Superlative:** one row per distinct target (`null` → "Nadie"); sorted by `VoteCount` descending.
- **Secret Pairing:** each voter writes 2 `Vote` rows (one per predicted partner) sharing their `UserId`. Results are grouped by the **pair** (the unordered set of the 2 target ids), not by individual target — `"Naiara + Fockhaman"` is one combined row, not two separate 50% rows. Sorted by `VoteCount` descending.
- **Scale:** one row per value across the full `[RangeMin, RangeMax]`, including zero-vote values (it's a distribution, not a ranking — left in ascending numeric order).
- **Deathmatch:** one row per team, always both (even at 0 votes — a head-to-head comparison shouldn't hide a side); `TeamMembers` resolved server-side from `Question.Metadata.Teams` via the `usersById` lookup `DailyService.CalculateResultsAsync` builds for Deathmatch questions; sorted by `VoteCount` descending.

### 7.5 Static helpers

| Helper | Purpose |
|---|---|
| `QuestionMetadataBuilder.Build(dto)` | `CreateQuestionDto` → `QuestionMetadata` |
| `ResultsBuilder.Build(question, votes, mapper)` | Aggregate votes into result DTOs (incl. voters) |

### 7.6 Enums

```csharp
public enum QuestionSource { UserCreated = 0, Pack = 1 }
public enum QuestionType  { CustomPoll, Superlative, Deathmatch, Scale, SecretPairing, OpenText }
// VoteResult: Success, NoActiveQuestion, AlreadyVoted, InvalidPayload
// SelectResult: Success, NotYourTurn, QuestionNotFound, AlreadyActivated
```

---

## 8. API Endpoint Reference

Legend: ✅ done · 🔧 needs change · 🆕 to build · 🅿️ Phase 2

### Auth — ✅

| Method | Endpoint | Notes |
|---|---|---|
| `POST` | `/api/auth/register` | rate-limited |
| `POST` | `/api/auth/login` | rate-limited |
| `POST` | `/api/auth/refresh` | rotates refresh token |
| `POST` | `/api/auth/logout` | rate-limited |
| `GET` | `/api/auth/check-username` · `/check-email` | availability checks |
| `POST` | `/api/auth/google` | 🅿️ Google OAuth |

### Groups

| Method | Endpoint | Status | Notes |
|---|---|---|---|
| `GET` | `/api/groups` | ✅ | groups of the current user |
| `POST` | `/api/groups` | ✅ | create; creator = admin = first member |
| `GET` | `/api/groups/{groupId}` | ✅ | details |
| `PUT` | `/api/groups/{groupId}` | ✅ | admin: edit name/description/time |
| `POST` | `/api/groups/join` | ✅ | `{ invitationCode }` |
| `PUT` | `/api/groups/{groupId}/admin` | ✅ | transfer admin |
| `GET` | `/api/groups/{groupId}/members` | ✅ | `{ id, username, avatarUrl, frameColor, joinedAt, isAdmin, isCurrentUser, notificationsMuted, currentStreak }` |
| `GET` | `/api/groups/{groupId}/members/{userId}/stats` | ✅ | member card (rama 19): current/best streak, votes, participation %, times selector, questions created |
| `DELETE` | `/api/groups/{groupId}/members/me` | ✅ | leave group |
| `DELETE` | `/api/groups/{groupId}/members/{userId}` | ✅ | admin: kick member |
| `POST` | `/api/groups/{groupId}/invite-code/regenerate` | ✅ | admin: rotate invitation code |
| `DELETE` | `/api/groups/{groupId}` | 🅿️ | delete group |

### Daily (core flow)

| Method | Endpoint | Status | Notes |
|---|---|---|---|
| `GET` | `/api/groups/{groupId}/daily/current` | ✅ | redesigned → `{ today, selection, myStreak }` (§4.7; `myStreak` added in rama 19); driven off `ActivatedAt` state |
| `POST` | `/api/groups/{groupId}/daily/select` | ✅ | sets the **pending (next-day)** question; never activates early; inline-create runs full §9 validation |
| `POST` | `/api/groups/{groupId}/daily/vote` | ✅ | submit vote; returns `{ results: QuestionResultDto, streak: StreakUpdateDto }` (rama 19 — before/after streak + tier info for the celebration) |
| `POST` | `/api/groups/{groupId}/daily/streak/dismiss-lost` | ✅ | acknowledges the lost-streak notice (`daily/current` → `myStreak.lostStreak`) |
| `GET` | `/api/groups/{groupId}/daily/selection-sources` | 🆕 | questions the selector can pick (pool + base pack) |

### Questions (pool & history)

| Method | Endpoint | Status | Notes |
|---|---|---|---|
| `POST` | `/api/groups/{groupId}/questions` | ✅ | add to pool |
| `GET` | `/api/groups/{groupId}/questions/pool` | ✅ | unused user questions |
| `DELETE` | `/api/groups/{groupId}/questions/{id}` | 🆕 | creator/admin: remove from pool |
| `GET` | `/api/groups/{groupId}/questions?date=YYYY-MM-DD` | ✅ | history detail for one date |
| `GET` | `/api/groups/{groupId}/history` | 🆕 | paginated list of past entries `{ date, questionText, type, totalVotes }` |

### Real-time / Infra

| Method | Endpoint | Status | Notes |
|---|---|---|---|
| `GET` | `/healthz` | ✅ | health check |
| `WS` | `/hubs/daily` | 🅿️ | SignalR live vote updates |

---

## 9. Validation Rules

Validators live in `Validators/` (FluentValidation). Vote-payload validation is enforced in `DailyService` (`ValidateVotePayload`).

### `CreateQuestionDto` — type-specific

| Type | Rule |
|---|---|
| Superlative | `BlacklistedUserIds` must not include the creator; ids must be group members |
| Deathmatch | no user id in more than one team; 2–4 teams, each 1–4 members |
| Scale | `RangeMin < RangeMax` (both `0–100`); `TargetUserId` **optional** — if set, must be a group member |
| Secret Pairing | `MinSelections == MaxSelections == 2` |
| Custom Poll | `2 ≤ Options ≤ 8`; `1 ≤ MinSelections ≤ MaxSelections ≤ Options.Count`; `AllowOther` is a bool flag |
| Open Text | text only (3–200 chars); no structured metadata |
| (all) | `AllowOther` may only be `true` for Custom Poll; question text 3–200 chars |

> The inline-create path of `daily/select` (`NewQuestion`) runs the same FluentValidation as `POST /questions` (chained via `SelectQuestionDtoValidator`).

### `CreateVoteDto` — type-specific

| Type | Rule |
|---|---|
| Superlative | required; not blacklisted; `0` only when `AllowNobody` |
| Deathmatch | must match exactly one configured team |
| Scale | required; integer in `[RangeMin, RangeMax]` |
| Secret Pairing | exactly 2 distinct ids; both active members |
| Custom Poll | total picks (options + optional `FreeText` when `AllowOther`) in `[MinSelections, MaxSelections]`; all ids valid; `FreeText` rejected unless `AllowOther` |
| Open Text | `FreeText` required, non-empty, ≤ 280 chars |

---

## 10. Real-time Strategy

| Stage | Mechanism | Effort |
|---|---|---|
| **MVP** | **Refresh button** on the results view (re-fetches `daily/current`) | trivial |
| Phase 2 | **SignalR** hub `/hubs/daily`, per-group rooms, broadcast on vote → live results without reload | medium (JWT over WS, client reconnection) |

Design the vote path so it *can* later emit an event (a single broadcast call), but do not take a hard dependency on WebSocket infra for the MVP. Polling is an acceptable middle step if desired.

---

## 11. Limits & Abuse Protection

Free-tier quotas (Render/Vercel/Supabase) must not be exhausted. Enforce server-side:

| Limit | Value (MVP) |
|---|---|
| Members per group | 30 |
| Groups created per user | 10 |
| Groups joined per user | 30 |
| Unused pool questions per user, per group | 20 |
| Total pool questions per group | 500 |
| Question text length | 3–200 chars |
| Option text length | 1–80 chars |
| Options per Custom Poll | 2–8 |
| Scale range | `0 ≤ Min < Max ≤ 100` |
| Deathmatch | 2–4 teams, 1–4 members each |
| Invitation code | 6 alphanumeric chars |
| Rate limiting | existing `AuthLimiter` (10 / 5 min) on auth; add per-user limiters on `join`, `vote`, `select`, question creation |

> **Group names are not unique** — they are labels; identity is the `Id` + invitation code. No name-existence check on create.

---

## 12. Current Status

### Done

| Item | Status |
|---|---|
| Auth (register/login/refresh/logout, check-username/email), JWT + refresh rotation (hashed) | ✅ |
| `QuestionMetadata` owned type → JSONB; `Teams` via `HasConversion` | ✅ |
| FluentValidation wired; validators for Create{Question,Vote,Option}Dto | ✅ |
| Entities: User, Group, GroupMember, Question, Option, Vote, DailyEntry | ✅ |
| `QuestionSource` + `IsUsed`; QuestionType enum order remapped | ✅ |
| `DailyPreselectionService` (preselect tomorrow + activate today; ≥2-member start gate) | ✅ — rama 3 |
| `QuestionMetadataBuilder`, `ResultsBuilder` (incl. voters) | ✅ |
| Groups: list/create/get/update/join/transfer-admin | ✅ |
| Daily: current / select / vote | ✅ — rama 3 (lifecycle rewrite per §4) |
| Questions: pool / create / by-date | ✅ |
| Migrations through `AddGroupTimeZoneId` | ✅ |
| UTC+2 offset applied to today/tomorrow/T comparisons (`DailyClock` helper); `Group.TimeZoneId` (nullable, default `Europe/Madrid`, unused by MVP logic) | ✅ — rama 0 merged |
| `GET /groups/{id}/members` (`{ id, username, avatarUrl, joinedAt, isAdmin }`); Angular `GroupMember` model + `getGroupMembers` service method | ✅ — rama 1 merged |
| Base pack: `Pack` + `QuestionTemplate` (+`QuestionTemplateOption`) entities, `Question.CreatorId` nullable, idempotent `BasePackSeeder`, `TemplateCloner` (clone-on-use + auto-resolution §6.3), preselection fallback, `GET /daily/selection-sources`, `SelectQuestionDto.TemplateId`; Angular `SelectionSources` model + `getSelectionSources` | ✅ — rama 2 (`feat/base-pack`) |
| Daily lifecycle rewrite: `daily/current` → `{ today, selection }` (driven off `ActivatedAt`); `daily/select` sets the **next-day** question with **no early activation**; inline-create runs full §9 validation (structural + group-membership, shared with `POST /questions`); `daily/vote` returns fresh results; ≥2-member start gate; `DailyClock.ToUtc` for `closesAt`/`activatesAt`; Angular `DailyStatus`/`SelectQuestion` models + `getCurrent`/`select`/`vote` | ✅ — rama 3 (`feat/daily-lifecycle`) |
| Branding & design-system foundation: Tailwind `@theme` tokens, color/spacing/type scales, dark mode + WCAG AA contrast, Spartan component theming & states | ✅ — rama 4 (`feat/design-system`, [#18](https://github.com/alexsepulvedaramos/Preguntitas/pull/18)) |
| Group-detail screen + route `groups/:groupId`; consumes `daily/current`; `@switch` skeleton loading state; "te toca elegir" indicator; refresh button; `JoinGroupAsync` cold-start seeds the first `DailyEntry` once a group reaches 2 members | ✅ — rama 5 (`feat/group-detail`) |
| Voting UI: `app-vote` dispatcher + five per-type sub-components (superlative, deathmatch, scale, secret-pairing, custom-poll); wired into `GroupDetailComponent`'s `'voting'` case. Backend (`CreateVoteDto`, `POST vote` → `QuestionResultDto`) was already complete from rama 3 — this branch only built the Angular UI and fixed `CreateVote`/`QuestionResult` frontend models to match the backend DTOs exactly. `Votes` unique index relaxed to non-unique on `(QuestionId, UserId)` (SecretPairing/multi-select CustomPoll legitimately write more than one row per user per question). Bootstrap exception added (§4.1): `JoinGroupAsync` now activates the group's first question immediately instead of waiting for T, with a second question preselected normally for the real next T; `CountdownComponent` shows a live "siguiente pregunta en…" label off `today.closesAt`, turning red under 5 minutes | ✅ — rama 6 (`feat/voting`) |
| Results UI: `app-results` dispatcher (mirrors `app-vote`) + `ScaleResultComponent`/`DeathmatchResultComponent`/shared `ResultOptionBarComponent` (CustomPoll, Superlative, Secret Pairing render inline — identical row shape); wired into `GroupDetailComponent`'s `'results'` case. Compact proportional-width bars (`width: percentage%`), one of 6 chart colors per row (`--chart-1..6` theme tokens), tap-to-see-voters dialog, up to 3 stacked voter-initial avatars on bars with votes. Scale shows a prominent average headline above a full-range histogram (always one consistent tone, not per-bin colors — it's a distribution, not separate options); zero-vote bars render fully empty (no sliver). Backend fixes that came out of this branch: `TotalVotes` now counts distinct voters (§7.4), `OptionResultDto.TeamMembers` resolves Deathmatch team identity server-side, results sorted by `VoteCount` descending (except Scale), Secret Pairing results merged into combined pair rows instead of two separate per-target rows (§7.4). Header reads "Ha/Han votado X de Y miembros" (X = `TotalVotes`, Y = group member count) | ✅ — rama 7 (`feat/results-view`) |
| Per-type question creation form; selector picker (pool + base-pack + inline create); `DELETE /groups/{id}/questions/{id}`; pool quotas (20 unused/user/group, 500/group total); **`OpenText` question type** (§5.6) — free-text answer, results as a response list; **`CustomPoll.AllowOther`** flag + "Otro" free-text vote option (reuses `Vote.FreeText` + shared `FreeTextResponsesComponent`); **`Scale.TargetUserId` optional** — rate any subject, not just a group member; Deathmatch validator widened to 2–4 teams of 1–4 members; drag-and-drop team builder (`@angular/cdk/drag-drop`); `createQuestionDtoValidator` + `createVoteDtoValidator` tightened (min 3 chars, option max 80 chars, AllowOther only on CustomPoll, OpenText clears all other fields); per-type validation feedback in the creation form (asterisk on required fields, empty-team red-border, inline error text on submit); dialog resets state on every reopen; global slim-scrollbar polish | ✅ — rama 8 (`feat/create-question`) |

| `GET /api/groups/{id}/history` (cursor-based paginated list); `HistoryComponent` at `/groups/:id/history` (date picker + inline results + "Cargar más"); "Ver historial" link in group-detail; `QuestionResultDto.RangeMin`/`RangeMax` for Scale history display | ✅ — rama 9 (`feat/history`) |
| `DELETE /groups/{id}/members/me` (leave group; auto-assigns admin to oldest remaining member; deletes group if last member); `DELETE /groups/{id}/members/{userId}` (admin: kick non-admin member; DailyEntry left as-is); `POST /groups/{id}/invite-code/regenerate` (admin: generate new 6-char code); `GroupSettingsComponent` at `/groups/:id/settings` — edit info (admin), member list with kick + transfer-admin dialogs (admin), invitation code display + copy + regenerate (admin), leave group with contextual confirmation; gear icon in group-detail header; `hlm-toaster` wired in main layout | ✅ — rama 10 (`feat/group-admin`) |

| Login with username **or** email (`identifier` field; `Validators.email` removed; backend already accepted either); login error signal (401 → specific message, other → generic); **word-stagger animation** on question text (CSS `@keyframes vp-word-enter`, `WordStaggerPipe` with `DomSanitizer`, `.vp-question-text` class); **group card redesign** (deterministic coloured initial circle per `id % 6`; conditional status line — "Vota ahora" pill / "¡Te toca elegir!" lime pill / "Nueva pregunta a las HH:MM" / "Pregunta a las HH:MM"); **daily status on group list** (`GET /groups` computes per-group `DailyStatus` in 4 efficient queries without N+1; `GroupResponse.DailyStatus`; priority: `voting` > `selector` > `results` > `no_question`); **pull-to-refresh** (`PullToRefreshDirective` — `window` touch events, resistance curve, fixed indicator below header, `isRefreshing` input drives auto-hide via `effect()`; refresh button hidden on mobile `hidden sm:inline-flex`); **profile drawer** (initials avatar with icon fallback; username + email; JWT claim extraction covers both compact and full-URI form); footer `hidden sm:block` on mobile; Google OAuth buttons commented out pending implementation; history date picker `[min]`/`[max]` bounds; result bars width-normalised (max-vote option = 100%); all-voters summary dialog (click any bar); `deathmatch-result` and `scale-result` pass `[allResults]` to bars (fixes missing voter avatars); Google button and "¿Olvidado tu contraseña?" hidden in login/register | ✅ — rama 11 (`feat/design-polish`) |

| Push notifications (Web Push / VAPID): `DevicePushSubscription` + `NotificationPreferences` entities + migration (`AddNotificationsSupport`); `INotificationService` / `NotificationService` (Lib.Net.Http.WebPush); `NotificationsController` (`GET /vapid-key`, `POST/DELETE /subscriptions`, `GET/PUT /preferences`); `PATCH /groups/{id}/members/me/mute`; 4 notification types — `new_question` (fired at T for all members), `selector_turn` (fired at preselection time + 3 h reminder if still auto-selected), `user_voted` (fire-and-forget on `VoteAsync`), `new_message` (fire-and-forget on `SendMessageAsync`); stale subscription cleanup (410/404); VAPID keys in Render env vars (`Vapid__PublicKey/PrivateKey/Subject`). Angular: `PushNotificationService` (SwPush wrapper, VAPID fetch, subscribe/unsubscribe, notification-click routing, 7-day dismiss cooldown); `NotificationPreferencesService`; `PushNotificationPromptComponent` nudge banner; "Notificaciones" section in `/profile` (master toggle + 4 per-type switches); "Silenciar este grupo" switch in group settings; `SelectQuestionDialogComponent.triggerOpen()` (auto-opens after voting if current user is selector). Quick-wins: register form shows 409 conflict error; join-group shows "Ya eres miembro" toast. **Subscription-sync fix (2026-10-06):** most users had push permission granted but no server-side subscription (only 9/42 users had a `DevicePushSubscriptions` row), and the profile toggle keyed off `Notification.permission` alone, so it could never re-subscribe. Now: the toggle reflects permission **and** an actual browser push subscription; on app start / login, if permission is granted and the user hasn't opted out on this device (`notif_push_opted_out` in localStorage, set by turning the toggle off), the client silently re-posts its subscription (upsert by endpoint) or creates a new one, also replacing subscriptions created with a different VAPID key; subscribe failures show a toast instead of being swallowed; the nudge prompt only shows to logged-in users. | ✅ — `feat/notifications` |

| Portfolio & onboarding pass: product-first root `README.md` (live demo link, features, architecture diagram, honest roadmap, engineering practices); `server/README.md` (local setup moved from root) + real `client/README.md`; `index.html` → `lang="es"`, meta description + Open Graph/Twitter cards (WhatsApp/LinkedIn link previews); animated auth showcases (developer-picked formats, 2026-07-16): login → `RotatingQuestionComponent` (real seeded questions cycling with the existing word-stagger animation + one-line pitch; returning users need speed, not selling), register → `ResultsShowcaseComponent` (fake looping who-voted-for-whom result bars with fictional names; new visitors need the product's magic demonstrated); both respect `prefers-reduced-motion`; dead code removed (unused `styles-ambar/arcade/neon.css`, `features/questions/question-list`, commented routes and font experiments) | ✅ — `feat/portfolio-polish` |

| Per-group voting streaks & streak frames (rama 19): `GroupMember` streak columns + `User.StreakFrameAutoApplied` + `NotificationPreferences.StreakDanger/StreakDangerHoursBefore` + nullable `ChatMessage.UserId` (system messages) in migration `AddMemberStreaks` (also sets `FrameColor = 'streak'` where it was `null`); `StreakService` (vote registration, effective streak, history recompute, startup backfill, "streak in danger" pushes from `DailyPreselectionService`); `POST daily/vote` → `{ results, streak }`; `daily/current.myStreak` + `POST daily/streak/dismiss-lost`; `GET groups/{id}/members/{userId}/stats`, `GET users/me/group-stats`, `UserProfileDto.HighestStreak`; automatic chat messages on tier changes/milestones. Titles (`User.HighestStreakEver`, `User.SelectedTitleKey`, `PUT users/me { titleKey }`) and the group crown (`GroupMemberDto.HasCrown`). Angular: SVG streak frames in seven materials (`StreakFrameComponent`, wood and stone static), crown for the group's best streak, title picker in the profile, `GroupStreaksService`, member card (`MemberCardComponent` — drawer on mobile / dialog on desktop, own all-groups card from `/profile`), full-screen `StreakCelebrationComponent` (dark scene, lazy in-house embers and confetti), lost-streak alert, streak badges in settings, system messages in chat, "Racha"/"Ninguno" frame options, streak-danger switch + lead-time picker, profile section-title polish. | ✅ — rama 19 (`feat/streak-frames`) |
| Pull-to-refresh touch fixes (2026-10-06): the group-detail pull-to-refresh listened to every touch on the window, so swiping the member-card drawer down reloaded the group instead of closing it, and lists inside overlays (the question picker) couldn't scroll back up — the gesture cancelled the scroll whenever the page behind was at the top. It now ignores touches inside overlays/dialogs/drawers and inside inner lists scrolled away from their top; the member-card drawer gets `touch-action: none` (its group list keeps `pan-y`) so drag-to-close works on touch screens. | ✅ |
| Unexpected-logout fix (2026-10-07): the client cleared the session on *any* failed `/refresh` — so when the 15-min access token had expired, a transient failure on the next refresh logged the user out: reopening/unlocking the mobile PWA before the network was back, or hitting the API during a Render redeploy (502s while the new instance boots; the backend itself is kept awake by an UptimeRobot ping to `/healthz` every 10 min, so idle cold starts are not a factor); two tabs refreshing at once could also rotate the same token and the loser then called `/logout` with the winner's new token. Now only a 400/401 from `/refresh` ends the session (locally, no `/logout` call); transient failures keep the tokens and restore the user from the expired token's claims; refreshes are serialised across tabs with the Web Locks API and retried once with a token another tab rotated mid-flight. Refresh tokens last 365 days sliding (was 90) and the session cap evicts the least recently used device instead of the oldest login. | ✅ |
| Daily-time-change transparency (§4.8): editing `Group.DailyQuestionTime` mid-cycle — confirmed the live `closesAt`/`activatesAt` already reschedule today's still-pending question to the new T; when today already activated, the change applies next cycle (no re-anchor / no early close). `PUT /groups/{id}` now returns `GroupResponse.DailyTimeChangeAppliesFromTomorrow` (time changed **and** a question activated during today's local UTC+2 day, measured off `ActivatedAt`); group-settings shows an informational toast. | ✅ — rama 20 (`fix/daily-question-time-change`) |

### Pending (MVP)

MVP feature-complete (ramas 0–12 merged; rama 13, SignalR, optional and not started). Of the post-MVP bug-fix & optimization pass (§13), ramas 14–15 are merged; rama 20 is implemented on `fix/daily-question-time-change`; rama 19 is implemented (streak frames); ramas 16–18 remain pending. Phase 2 backlog in §16.

---

## 13. Roadmap & Branch Plan

Each branch is **backend + its Angular UI**, cut from `master`, merged before the next. The **design-system** branch (rama 4) is frontend-only and may run in parallel with the backend ramas 1–3. Gamification, chat, SignalR, OAuth, avatars, per-group time-zone logic, fair rotation, and multi-language gameplay are **Phase 2**. *(Thematic packs + per-group toggle shipped in rama 15.)*

| # | Branch | Scope |
|---|---|---|
| 0 ✅ | `fix/daily-time-utc-offset` | Apply configurable **UTC+2** offset to all "today/tomorrow"/T comparisons in `DailyPreselectionService` and `DailyService`; add nullable `Group.TimeZoneId` column (default `Europe/Madrid`, unused by MVP logic) + migration. **Merged ([#13](https://github.com/alexsepulvedaramos/Preguntitas/pull/13)).** |
| 1 ✅ | `feat/group-members` | `GET /members` (+ `isAdmin`); Angular models/service. **Unblocks person-based voting.** **Merged ([#15](https://github.com/alexsepulvedaramos/Preguntitas/pull/15)).** |
| 2 ✅ | `feat/base-pack` | `Pack` + `QuestionTemplate` entities, migration (`Question.CreatorId` nullable), seed the **Base** pack (several of each type), clone-on-use + auto-resolution (§6.3), preselection fallback, `daily/selection-sources`. **Implemented on `feat/base-pack`** (migration pending apply). |
| 3 ✅ | `feat/daily-lifecycle` | Rewrite `daily/current` (`{today, selection}`) and `daily/select` (next-day, **no early activation**); validate inline-create; align `DailyPreselectionService`; ≥2-member start; first selector = creator. **Implemented on `feat/daily-lifecycle`.** |
| 4 ✅ | `feat/design-system` | **Branding & design foundation (frontend-only; can run in parallel with ramas 1–3, must land before the screens).** Consolidate Tailwind `@theme` tokens; define color/spacing/type scales; dark mode + WCAG AA contrast; Spartan component theming & states (hover/focus/disabled/loading/empty); logo usage. The unified visual language every screen inherits. **Merged ([#18](https://github.com/alexsepulvedaramos/Preguntitas/pull/18)).** |
| 5 ✅ | `feat/group-detail` | Group-detail screen + route `groups/:groupId`; consumes `daily/current`; `@switch` skeleton; "te toca elegir" indicator; refresh button. **Implemented on `feat/group-detail`.** |
| 6 ✅ | `feat/voting` | Five voting sub-components; build `CreateVoteDto`; `POST vote` → results. Backend was already complete from rama 3; this branch built the Angular UI (`app-vote` dispatcher + per-type components) and fixed two frontend DTOs that had drifted from the backend contract. **Implemented on `feat/voting`.** |
| 7 ✅ | `feat/results-view` | Per-type results visualization incl. **who voted for what**. **Implemented on `feat/results-view`.** |
| 8 ✅ | `feat/create-question` | Per-type create form (options/range/teams/blacklist/target); selector picker (pool + base + inline create); `DELETE` pool question; pool quotas (§11); **`OpenText` type** (§5.6); **`CustomPoll.AllowOther`** + "Otro" free-text vote; **`Scale.TargetUserId` optional**; Deathmatch 2–4 teams + drag-and-drop builder; per-type validation feedback; dialog reset on reopen. **Implemented on `feat/create-question`.** |
| 9 ✅ | `feat/history` | `GET /api/groups/{id}/history` cursor-based paginated list (`HistoryEntryDto`: date, questionText, type, totalVotes; 20/page, `before` cursor); `QuestionResultDto` extended with `RangeMin`/`RangeMax` (Scale display); `HistoryComponent` at `/groups/:id/history` — date picker jump-to-date, inline expandable results, "Cargar más" pagination; "Ver historial" link in group-detail; **open archive** — results shown regardless of whether the user voted. **Implemented on `feat/history`.** |
| 10 ✅ | `feat/group-admin` | Admin panel (name/description/time, members, transfer admin); leave/kick/regenerate-code. **Confirmed behaviors:** admin leaving → auto-assign to oldest remaining member; last member leaving → delete group; kicked selector's pending DailyEntry left unchanged. **Schema change:** `Group.AdminId` removed; `GroupMember.IsAdmin` (bool) is now the source of truth for admin identity; migration backfills existing admins. **`GroupMemberDto.IsCurrentUser`** added (set server-side by integer ID comparison) so the Angular settings page can derive `isAdmin` without touching JWT claims. **Selector bug fix:** `SelectQuestionAsync` now reads `pendingEntry.SelectorUserId` instead of re-running `CalculateSelector` at selection time — prevents 403s when a user rejoins after being kicked (new `JoinedAt` would otherwise shift the rotation). **Spartan portal pattern:** `hlm-alert-dialog-content` must be inside `<ng-template hlmAlertDialogPortal>` to open in a CDK overlay where `BrnDialogRef` is provided. **Implemented on `feat/group-admin`.** |
| 11 ✅ | `feat/design-polish` | **Final unification & UI QA pass** across all screens: responsive layouts, empty/loading/error states, micro-interactions/motion, copy tone, and a checklist against the design rules. The "pulcro y con personalidad" pass. **Implemented on `feat/design-polish`.** |
| 12 ✅ | `feat/notifications` | Web Push / VAPID notification system. Backend: `DevicePushSubscription` + `NotificationPreferences` entities + `AddNotificationsSupport` migration; `INotificationService` / `NotificationService` (Lib.Net.Http.WebPush v3.3.1); `NotificationsController` (VAPID key, subscriptions, preferences); `PATCH /groups/{id}/members/me/mute`; 4 notification types: `new_question` at T for all members, `selector_turn` at preselection + 3 h reminder, `user_voted`, `new_message`; stale-subscription cleanup on 410/404; VAPID keys in Render env vars. Angular: `PushNotificationService` + `NotificationPreferencesService`; nudge prompt banner; global preferences in `/profile`; per-group mute toggle in settings; auto-open selector dialog after voting. Quick-wins: 409 error on register, "Ya eres miembro" toast on join. **Implemented on `feat/notifications`.** |
| 13 | `feat/realtime-signalr` | *(optional)* SignalR hub + Angular client; replaces the refresh button. |

### Post-MVP bug-fix & optimization pass (confirmed 2026-06-30)

Triaged from a combined list of developer-reported bugs + the §16 backlog. Several smaller fixes are grouped per branch (developer preference: fewer, denser branches over one-bug-one-branch). Streak/frame design and other functionally-relevant decisions below are now confirmed scope, not raw ideas — superseded entries removed from §16.

| # | Branch | Scope |
|---|---|---|
| 14 ✅ | `fix/results-bar-and-live-countdown` | Result-bar label legibility: the label overlay (`result-option-bar.component`) spans the full track width independent of the colored fill's `displayWidth()`, with fixed `text-white` and no backdrop — illegible when the fill is narrower than the label. Fix: `text-shadow` outline on the label text (no DOM/layout restructure). Countdown live update: `CountdownComponent` has no output; reaching zero just shows "Siguiente en cualquier momento" with no refetch until manual reload. Fix: emit an `output()` on zero, `GroupDetailComponent` listens and calls `refresh()` automatically. **Implemented on `fix/results-bar-and-live-countdown`.** |
| 15 ✅ | `fix/question-pool-and-permissions` | **Bug fixes:** Deathmatch history showed team colours but no member names (`GetByDateAsync` never built `usersById`) — extracted `ResultsBuilder.BuildDeathmatchUsersById`; "who voted" dialog listed voters as a comma paragraph → vertical list; CustomPoll results now include 0-vote options (§7.4). **Base-pack lifecycle:** exhaustion-based template reuse replacing the 50-day cooldown (§6.6; `SelectResult.RecentlyUsedTemplate`→`TemplateAlreadyUsed`); creator attribution on edited clones (`CreatorId ??= userId`); block editing/teams-override of a question owned by another user. **Per-group packs:** `GroupDisabledPack` entity + `AddGroupDisabledPack` migration + `PacksController` (list / admin toggle / paginated templates) + admin toggle UI (§6.5). **Picker:** cursor pagination + type/pack filters; new `GET /packs/templates` replaces `GET /daily/selection-sources` (§6.4). **Content:** `BasePackSeeder` generalized to idempotent `PackSeeder`; 14 new thematic packs + 12 Base additions (~257 questions). **Implemented on `fix/question-pool-and-permissions`.** |
| 16 | `feat/scale-1-10` | Fix Scale question type to a fixed **1–10** range (remove configurable `Min`/`Max`); migration, validator, and frontend (`scale-create`, `scale-vote`, `scale-result`) updates; revise §5.4, §9, §11 (range/limits) accordingly once implemented. **Bug (developer-reported 2026-07-16): pack Scale questions always get a random target person.** `TemplateCloner` auto-resolution (§6.3) unconditionally assigns a random active member as `TargetUserId` to every pack Scale clone, even when the template text is phrased in second person for self-evaluation — e.g. "¿Cómo de bien conduces?" shipped with "Sobre: Henar", so some members rated themselves and others rated Henar, mixing the semantics of the results. **Confirmed direction:** pack Scale templates default to **no target** (each member answers for themselves / about the subject in the text — reads better); templates that genuinely target a person must say so explicitly, and the UI should render the name **inline in the question text** with highlighted styling (e.g. "¿Cómo de bien conduce **{María}**?") instead of the detached small "Sobre: X" line. Mechanics (per-template opt-in flag + text placeholder interpolation) to be designed within this rama. |
| 17 | `fix/avatar-dark-mode-and-join-flow` | DiceBear "Garabatos" (`croodles-neutral`) preset is near-invisible in dark mode — black linework on a transparent SVG with no theme-aware backing circle (picker grid uses `bg-muted`, `UserAvatarComponent` uses `bg-card`; neither guarantees contrast for this style). Join flow: registering via an invite link doesn't auto-join the inviting group afterward (only works today when logging into an existing account via the link) — low priority, but closes the navigation loop. PWA: add `"id"` to `public/manifest.webmanifest` to stop Chrome's persistent "tap to copy this app's URL" notification on the installed app. **Push follow-ups (found 2026-10-06 while fixing the subscription-sync bug):** (a) logout doesn't remove the device's `DevicePushSubscription` — on a shared device the previous account keeps receiving notifications until another account logs in and its startup sync takes the subscription over; unsubscribe server-side (DELETE by endpoint) on logout without touching the browser permission. (b) `NotificationService` sends with `TimeToLive = 3600` — a phone offline/dozing > 1 h silently drops the notification; reconsider TTL (e.g. up to the next daily T for `new_question`/`selector_turn`). (c) Notifications are dispatched fire-and-forget (`_ = notificationService.Send…`); an exception before the per-subscription loop (e.g. the recipients query) is lost with no log — wrap each dispatch so failures are logged. |
| 18 | `feat/mobile-group-header` | Experimental, mobile only: inside a group's routes (detail/history/settings) the global app header (logo/theme/user-menu) and group-detail's own back/history/settings row currently stack as two sticky bars — merge into one. The group-list page header is untouched. |
| 19 | `feat/streak-frames` | Per-group voting streak (not global, not login-based): `GroupMember` gains a streak counter, incremented in `VoteAsync` when the member has voted on consecutive active daily questions for that group, reset on a missed day. **The frame only appears once the streak reaches 3 consecutive days** (days 1–2 show no frame). Animated avatar-ring tiers by streak length: 3–6 days *(ember)* → 7–29 *(small orange flame)* → 30–99 *(intense orange flame)* → 100–181 *(blue flame)* → 182–364, i.e. 6 months+ *(purple flame)* → 365, the cap *(gold flame)*. Streak count surfaced next to each member's name in `GroupSettingsComponent`'s member list. Also: minor profile-page polish (section-title size/font). Longevity frames and frame purchasing are out of scope here — Phase 2 (§16). **Decisions confirmed 2026-10-06:** (1) **Frozen days:** a day with no active daily question in the group (e.g. the group dropped below 2 members, §4.3) neither increments nor resets the streak — it stays frozen; only a missed *active* question resets it. (2) **Where the ring shows:** every place inside a group where a member's avatar appears (daily question, results/voters, chat, history, settings member list) renders that member's streak ring **for that group**. Outside a group (profile page, header avatar) the user's own avatar shows their **highest current streak across all their groups** (current, not all-time best — the ring drops if that streak is lost); inside a group, their streak in that group. (3) **Ring = frame-colour option:** the streak ring is not layered on top of `FrameColor` — it's one more choice in the existing frame-colour picker. A user picks either a fixed colour (current palette) or **"Racha"** (frame follows their current streak tier; below 3 days → no frame). The existing `FrameColor` field gains that streak option. (4) **Backfill:** when the rama ships, current streaks are computed once from existing vote history (data migration / startup backfill), not started from zero. **Group stats (added 2026-10-06):** the group info/settings page shows per-member statistics — at least **total votes cast in the group** next to each member alongside their streak. **Member detail card (confirmed 2026-10-06):** to keep the member list light, `GroupSettingsComponent`'s list shows only avatar (with streak ring), name and a small streak badge (e.g. "🔥 12"). Tapping **any member avatar inside a group** opens a member card — Spartan drawer on mobile, dialog on desktop — with: large avatar + animated ring and the tier name (e.g. "Llama azul"); current streak and **best-ever streak** in that group; total votes cast in the group; participation % (votes ÷ active daily questions since the member joined); times as selector; questions created in the group. All derived from existing data except best-ever streak, which needs a new `GroupMember` column updated together with the current streak. *(Optional, only if it doesn't bloat the rama: open your own card from the profile page with your stats per group.)* **Streak celebration (confirmed 2026-10-06):** after a vote that increments the streak, show a brief Duolingo-style full-screen animation with the updated streak count and the days left to the next tier (on days 1–2, the days left to the first ring). **Days 1–2 reward (confirmed 2026-10-06, option B):** no ring before day 3 (unchanged), but days 1–2 still get the full-screen celebration (e.g. "Día 1 · ¡Racha empezada!") plus progress dots towards the first ring (●○○ → ●●○), visible to the user themself. The vote response must carry the before/after streak (and tier info) so the client can play it; tap to dismiss, and honour `prefers-reduced-motion` with a static version. **Further confirmed scope (2026-10-06):** (a) **Streak unit = daily cycle**, not calendar day — one cycle runs from T to the next T (§4.1); voting at 00:30 on a question activated at 21:00 counts for that cycle. (b) **Milestones** at days 14, 50, 200 and 300 (in addition to tier changes) get a special celebration (e.g. "¡2 semanas!") with confetti but no new ring art — keeps rewards frequent without multiplying ring designs. (c) **"Streak in danger" push:** a few hours before the cycle closes, notify members with a streak ≥ 3 who haven't voted yet (e.g. "🔥 Tu racha de 12 días en *Grupo* termina a las 21:00"); new notification type, toggleable in `NotificationPreferences` (+ migration). (d) **Lost-streak message:** next time the user opens the group after losing a streak, a gentle notice ("Has perdido tu racha de 12 días · Tu récord: 30"). (e) **Milestone chat messages:** an automatic system message in the group chat when a member reaches a tier change or milestone (e.g. "🔥 Ana lleva 30 días seguidos"). **Implementation approach:** rings are **pure CSS** (masked conic-gradient ring + small inline-SVG flame tongues, animating only `transform`/`opacity` since 10–15 avatars can be on screen; static under `prefers-reduced-motion`), built as one wrapper around `UserAvatarComponent` driven by a tier class — no animation library. The celebration screen is a CSS-animated overlay (ring growth, count-up, progress bar) plus `canvas-confetti` (~10 kB) **lazy-loaded** only when shown. Lottie is not used (≈250 kB player + custom animation work); could be revisited later, lazy-loaded for the celebration only. **✅ Done (2026-10-06). Decisions confirmed during implementation:** (i) **Danger push timing is configurable per user** (global, in profile → Notificaciones): 1, 2, 3, 4, 6 or 8 h before the cycle closes, **default 3 h**; on by default; respects group mute; only members with a live streak ≥ 3 who haven't voted, sent once per cycle. (ii) **Frame default = "Racha":** users with no chosen colour (`null`) follow their streak (migration sets them to `"streak"`; new users default to it); a fixed colour is **switched to "Racha" automatically once**, the first time the user reaches a 3-day streak in any group (`User.StreakFrameAutoApplied`) — later choices are always respected. "Ninguno" is stored as `"none"`. (iii) **The counter keeps counting past 365**; the gold flame is the top ring. (iv) **Days 1–2 progress dots** live on the user's own member card (and the celebration), not elsewhere. (v) **Leaving and rejoining** a group rebuilds the streak from vote history (cycles spent outside count as missed, so the record survives but the current streak usually doesn't). (vi) The "lost streak" notice shows for lost streaks of ≥ 2 days, once, until dismissed. (vii) Tier names: Brasa, Llama pequeña, Llama intensa, Llama azul, Llama morada, Llama dorada. (viii) The own card from the profile page (optional) **is included**, as is the profile section-title polish. (ix) Confetti is a ~2 kB in-house canvas burst, lazy-loaded with the celebration, instead of `canvas-confetti` — avoids a new dependency while the client lockfile is out of sync (rama 22). (x) Avatars inside a result bar aren't tappable (they sit inside the bar's own button, which opens the vote summary); the member card opens from the settings member list and from the avatars in the vote-summary dialog. Chat messages have no avatars, so no ring there. **Revised after the design review (2026-10-06 — supersedes the flame tiers, tier names, milestone 14 and the "pure CSS ring" approach above):** (1) **Seven frame materials instead of flame rings**, each with its own title: 3–6 *Madera* — «Cotilla en prácticas»; 7–14 *Piedra* — «Fuente anónima»; 15–29 *Bronce* — «Fuente bien informada»; 30–99 *Plata* — «Carne de tertulia»; 100–181 *Oro* — «Oráculo del salseo»; 182–364 *Zafiro* — «Colaborador de Sálvame»; 365+ *Amatista* — «La Vieja del Visillo». Titles are gender-neutral unless the character is unique. (2) **Wood and stone are humble:** no shine, no glow, no animation. From bronze up, a light sweeps the rim; ornaments add up with the tier (studs, a top gem from gold, cut facets over a slowly turning nebula for sapphire and amethyst, side gems and light rays for amethyst). Full frames (particles, ornaments, a day-count ribbon on the largest size) render on the large avatar sizes (profile, member card, celebration); list sizes get a light version (rim, shine, gem). Frames are SVG built by `StreakFrameComponent` from constants (`FRAME_LOOKS`), animating only transform/opacity, static under `prefers-reduced-motion`. (3) **Crown** = not a tier: it goes to the member with the **group's best current streak** (from day 1), on top of any material and on every avatar inside the group; ties go to **whoever reached that streak first** (earliest vote on the entry that got them there). Not shown outside the group. (4) **Milestones:** 21 («¡3 semanas!»), 50, 200 and 300. (5) **Titles are unlocked forever:** reaching a tier in any group unlocks its title permanently (`User.HighestStreakEver`, kept even after leaving the group). The profile has a «Título» picker — «Automático» (highest unlocked, the default), any unlocked title, or «Ninguno»; with no streak of 3 ever, no option is available. The chosen title shows under the name on the profile, the user menu, the member card and the group's member list. (6) The celebration is a dark full-screen scene in both themes: rays in the tier colour, the frame "forged" in place, the count popping up, embers rising, confetti on tier-ups and milestones; it names the title unlocked and the frame material. (7) **Free-text answers show their author's avatar** (with frame and crown, tap → member card) beside each answer bubble — Open Text is one of the most used question types, so it's where frames get seen; `FreeTextResponseDto` gains `UserId`, `AvatarUrl`, `FrameColor`. |
| 20 | `fix/daily-question-time-change` | **Bug (developer-reported):** changing a group's `DailyQuestionTime` in settings to a near-future time (e.g. "in 1 minute") doesn't recompute `closesAt`/`activatesAt` correctly — the countdown shows something like "1d 1m" instead of "1m". Suspected cause: today's `DailyEntry`/`ActivatedAt` (and the cached UTC offset math in `DailyClock`) is computed off the *old* `DailyQuestionTime` and not re-derived when the group's time changes mid-cycle, so the next-T calculation still anchors to the stale T instead of the new one. Needs investigation into `DailyClock` + `DailyPreselectionService`/`DailyService`'s `closesAt`/`activatesAt` computation and how (or whether) an in-flight cycle should re-anchor when `DailyQuestionTime` is edited. **Investigation outcome:** the suspected "stale cached T" was slightly off — `closesAt`/`activatesAt` already compute off the group's *live* `DailyQuestionTime`; what wasn't re-anchored is the pending `DailyEntry`'s **date**. **Decision (confirmed 2026-07-16, §4.8):** if today's question hasn't activated yet, the pending question just reschedules to the new T (already works); if today already activated, the change applies **from the next cycle** (no re-anchor, no early close — preserves the full 24h window and members' unused votes), and the settings UI shows an informational toast. `GroupResponse.DailyTimeChangeAppliesFromTomorrow` drives that message. **Implemented on `fix/daily-question-time-change`.** |
| 21 | `feat/public-onboarding` | **Part of the portfolio milestone (2026-10-06).** **Friendly for the context-free visitor.** Public landing at `/` (no auth): product pitch, "cómo funciona" in 3 steps, CTA to register (Spanish first; translated once rama 23 lands). Group-list empty state upgraded to explain the ≥2-member start gate ("invita a un amigo para que empiece el juego"). |
| 22 | `tests/core-domain-and-ci` | *(low priority — developer decision 2026-07-16)* Meaningful automated tests where the domain is hardest: `DailyPreselectionService` (selector rotation, ≥2-member gate, preselect-tomorrow/activate-today), `ResultsBuilder` (per-type aggregation, Secret Pairing pair-merge, distinct-voter counts), `TemplateCloner` (clone-on-use + auto-resolution), pool quotas. CI: new GitHub Actions workflow running build + tests for **both stacks on every PR** (client is not built in CI today); status badges in the README. **Known blockers (2026-10-06):** `client/src/app/features/groups/services/groups.spec.ts` imports `Groups`, which `groups.service` no longer exports, so `ng test` doesn't compile; `client/package-lock.json` is out of sync with `package.json` (`@nx/*`, `eslint` versions), so `npm ci` fails — both must be fixed before a client CI job can run. |
| 23 | `feat/i18n-ui` | **Play in English and German — UI layer** (confirmed 2026-07-16, promoted from Phase 2 §16). Runtime translation of all Angular copy (Transloco or equivalent — runtime switching, not compile-time `$localize` builds); `User.PreferredLanguage` (`es`/`en`/`de`) + migration; **default from the browser language** (`navigator.language` / `Accept-Language`) at first visit/registration — explicitly **not IP geolocation** (unreliable, needs a geo service; browser language is the standard mechanism); unsupported browser languages fall back to `en` *(assumption — flip to `es` if preferred)*; language picker in profile and on the auth pages; localized backend validation/error messages. |
| 24 | `feat/i18n-content` | **Play in English and German — content layer.** **Cultural adaptation, not literal translation (confirmed 2026-10-06, portfolio milestone):** the humour must survive — Spanish pop-culture references are replaced with local equivalents rather than translated word for word (e.g. the streak titles «Colaborador de Sálvame» and «La Vieja del Visillo», and pack questions that rely on Spanish TV, customs or idioms, get EN/DE versions with the same intent and tone); questions with no sensible equivalent are dropped or rewritten per language. Streak titles, milestone labels and chat system messages are part of this layer. Translation storage for `QuestionTemplate` + `Pack` (per-language text, e.g. JSONB translations map keyed by language code — keeps row counts free-tier-flat); translate all ~307 seeded questions + pack names/descriptions to EN and DE; template-based daily questions render in **each member's own language**; **user-created questions stay in the author's language** (no auto-translation — free tier, no paid APIs). |
| 25 | `feat/demo-experience` | *(low priority — developer decision 2026-07-16)* Demo experience so a solo visitor (recruiter, curious stranger) can see an active question, the vote flow and results with simulated data without needing a second member — implementation approach (frontend-only mock walkthrough vs seeded read-only group) to be decided at branch time; frontend-only is the free-tier-friendliest. **Confirmed 2026-07-16:** includes a **screenshot carousel** in an "acerca de"/info section and/or a **guided first-login tour** (typical onboarding walkthrough shown once after the first login) — the carousel belongs here, not on the rama 21 landing or the auth pages. **Promoted 2026-10-06 (portfolio milestone):** no longer low priority; it must include an **interactive demo during account creation** — right after registering (before/without a group), the new user plays a simulated day: votes on a sample question, sees results with fictional members, sees how selection, streaks and frames work. |
| 26 | `feat/about-and-releases` | **Portfolio milestone (confirmed 2026-10-06, see below).** A proper in-app **"Acerca de / Cómo funciona"** section: what the game is, the daily cycle, question types, streaks/frames/titles/crown explained with live examples (reusing the real components, e.g. the frames). Plus **versioned release notes**: the app gets a version number (semver, tagged on `master`), a **"Novedades" page** listing each release with what changed (written for players, not developers — like the rama 19 release-notes page), and a "what's new" badge/notice the first time a user opens the app after a new release. |

**Next step — portfolio milestone (confirmed 2026-10-06):** everything needed for Vaya Preguntita to stand as a viable portfolio project: (1) a good explanations section and versioned release notes (**rama 26**); (2) an interactive demo when creating an account (**rama 25**, promoted); (3) English and German translation with the cultural adaptations needed to keep it funny (**ramas 23–24**); (4) the public landing for context-free visitors (**rama 21**, added to the milestone 2026-10-06), so a recruiter landing on the site understands the product before signing up. Rama 16 (Scale 1–10) still lands before rama 24 so the content isn't translated twice.

**Development priority order (confirmed 2026-07-16; updated 2026-10-06 — 20 and 19 done; portfolio milestone first):** ~~19~~ ✅ → **26 → 21 → 25 → 23 → 16 → 24** *(portfolio milestone)* → 17 → 18 → 22 → 13 *(optional)* → Phase 2. Rationale (pre-milestone; the portfolio milestone now goes first): core-loop bug first (20); cheap UX fixes incl. the invite-link join gap (17); Scale 1–10 (16) lands before content gets translated ×3; public landing (21) is the highest-ROI product win for strangers; then the confirmed-priority i18n pair (23–24); engagement (19) and cosmetic work (18) next; tests+CI (22) and the demo experience (25) explicitly demoted to low priority by the developer; SignalR (13) stays optional.

**Deferred:** caching/perf hardening (reduce repeated queries, cache layer, regulate per-platform usage/limits, cap history depth) — needs the developer's real scaling forecast and intrinsic hosting limits before it can be scoped into a branch; tracked in §16, not yet assigned a rama number.

**Phase 2 backlog:** bundled "reparto/casting" question type · "adivina el autor" · points/ranking · priority-boost + in-app currency · frame purchasing + longevity frames (streak frames confirmed and moved to rama 19, §13) · **streak freeze** (Duolingo-style item that forgives one missed cycle; ties into coins) · **group streak** (consecutive cycles where every member voted — collective goal) · in-question chat/debate · Google OAuth · per-group time-zone logic · activity-aware selector rotation · history late-vote-to-unlock · monthly statistics · email verification & password recovery · HttpOnly-cookie refresh tokens · invitation QR · SSR/SEO · caching. *(Multi-language gameplay and automated tests/CI were promoted out of Phase 2 into ramas 22–24 on 2026-07-16.)* **Full developer wishlist in §16.**

---

## 14. Project Conventions & Git Guidelines

### Language
- **All** identifiers, comments, XML/JSDoc, and commit messages → **English**.
- UI-facing strings → **Spanish**.

### Branching
```
master        # protected — no direct pushes
feat/  fix/  refactor/  docs/  chore/
```

### Commits — Conventional Commits
```
feat: add GET /groups/{id}/members endpoint
fix: never activate daily question before group time
refactor: extract base-pack cloning into PackService
docs: consolidate spec into vaya-preguntita.md
```
Atomic commits — one logical change each; push after each approved task.

### Architecture rules
- **DTOs at API boundaries** — never return EF entities.
- **No DB/business logic in controllers** — services only; controllers orchestrate.
- **All schema changes via EF migrations** — never edit Supabase manually.
- **AutoMapper profiles** for mapping (`MappingProfile.cs`); no manual mapping in controllers.
- Keep Angular components small; push logic into services; use **Signals** and native control flow (`@if`/`@for`/`@switch`).
- Build UI with the **Spartan skill** (`agents/skills/spartan`) and Tailwind utility classes; custom CSS only in the `@theme` token block.

### Frontend Design System & Style Guide

Source of truth for tokens: `client/src/styles.css` (`@theme` block + `:root`/`:root.dark`). **Never hardcode raw Tailwind palette classes (`text-green-500`, `bg-red-600`, hex/rgb values) or one-off colors in components** — always use a semantic token below. If a needed color/state has no token yet, add it to `styles.css` first.

**Color tokens (semantic, light + dark pair in `styles.css`):**
| Token | Usage |
|---|---|
| `background` / `foreground` | Page base |
| `card` / `card-foreground`, `popover` / `popover-foreground` | Elevated surfaces (cards, dialogs, menus) |
| `primary` / `primary-foreground` (+ `primary-hover`) | Sage — brand identity, secondary actions |
| `accent` / `accent-foreground` (+ `accent-hover`, `accent-text`) | Electric Lime — primary CTA, focus ring, selection |
| `secondary` / `secondary-foreground`, `muted` / `muted-foreground` | Neutral surfaces, de-emphasized text |
| `sand` / `sand-foreground` | Warm Sand — badges, supporting accents |
| `success` / `success-foreground` | Confirmation feedback (e.g. "copiado al portapapeles") |
| `destructive` / `destructive-foreground` | Errors, destructive actions |
| `border`, `input`, `ring` | Borders, input outlines, focus ring (ring = `accent`) |
| `sidebar-*`, `logo-*` | Sidebar chrome and logo mark — don't reuse for other UI |

**Dark mode:** class-based (`:root.dark`, toggled by `ThemeService`). Every token is defined for both modes in `styles.css` — when adding a token, define both variants and verify WCAG AA contrast (text vs. its background) in each mode; don't assume light-mode contrast carries over.

**Typography:**
- `font-display` (`DM Serif Display`, italic) — headings, branding, the daily question text (`.vp-question-text` utility). Loaded via `<link>` in `index.html` (not CSS `@import`, for `preconnect`/perf).
- `font-sans` (`DM Sans`) — all UI text, body copy, form labels.
- Sizes: default Tailwind `text-*` scale, no custom font-size tokens — pick the smallest size that satisfies the hierarchy rather than introducing new scale steps.

**Spacing & shape:** default Tailwind v4 spacing scale (4px base unit) — no custom spacing tokens. Border radius via `--radius` (`1.125rem`, mapped to `rounded-default`) for cards, dialogs, inputs, and primary buttons; don't introduce other radius values.

**Interactive states — apply to every interactive element (buttons, inputs, links acting as controls):**
- **Hover:** the token's paired `-hover` variant when one exists (`primary-hover`, `accent-hover`); otherwise `hover:bg-muted` / `hover:bg-secondary`.
- **Focus:** `focus-visible:ring-2 focus-visible:ring-ring` (never plain `focus:`) — keyboard-only focus, no mouse-click ring. The base layer also enforces a global `outline-ring/60` fallback.
- **Disabled:** `disabled:opacity-50 disabled:pointer-events-none`.
- **Transitions:** `transition-colors` for simple state changes, `transition-all` only when size/shadow also animates (e.g. `.vp-poll-option`).
- **Loading:** disable the trigger and swap its label/icon for a spinner state; never let two competing loading affordances show at once.
- **Empty states:** centered, `text-muted-foreground` secondary line under a short primary message; see `group-list.component.html` for the reference pattern.

**Components:** build with **Spartan** (`Hlm*` directive/element components from `agents/skills/spartan`) — never hand-roll a primitive Spartan already provides. Customize only via `class` overrides layered on top of `hlmBtn`/`hlmInput`/etc., following the states above.

**Logo:** `LogoComponent` (`sm` | `lg`, icon mark) and `LogoWordmarkComponent` (text lockup) — colors always via `--logo-*` tokens, never hardcoded.

**Accessibility:** icon-only controls need both `aria-label` and an `sr-only` text node (see `ThemeComponent`); decorative-only elements get `aria-hidden`. Don't render an interactive-looking element (hover/cursor-pointer) that has no real action — remove it instead of faking it (see footer "Privacy/Terms" removal precedent).

---

## 15. Agent Instructions

> Applies to all AI coding agents working on this codebase.

### Source of truth
Read this document before writing code. If a behavior is **not** covered here, ask the developer — do not invent it. When the developer confirms a functionally-relevant behavior, **update this document** so it never needs re-asking.

### Do
- Implement the spec exactly: every question type, payload, and validation rule.
- Use DTOs, FluentValidation (in `Validators/`), and AutoMapper profiles.
- Generate a descriptively-named migration for every schema change.
- English only in code; Spanish only in UI copy.
- Use the Spartan skill for frontend work.

### Don't
- Don't store member names in question metadata at creation (dynamic types resolve at vote time).
- Don't activate a daily question before the group's time T (**the time is sacred**).
- Don't let pack templates hold votes — always clone into a group `Question`.
- Don't put business/query logic in controllers.
- Don't modify the DB schema without a migration, and **don't run `dotnet ef database update` autonomously** — confirm first.
- Don't implement Phase 2 items (gamification, chat, SignalR, OAuth, avatars) without explicit confirmation.

### Asking vs proceeding

| Situation | Action |
|---|---|
| Fully specified here | Proceed |
| Implied but not explicit | Proceed with the most conservative reading; note the assumption |
| Not covered at all | Ask first |
| Schema change / migration apply | Ask before applying |
| New dependency | Ask before installing |

---

## 16. Developer Wishlist & Backlog (raw ideas)

Captured from the developer's running notes (2026-06-22). Legend: 🟢 MVP · 🟡 MVP-small (polish on existing flow) · 🔵 Phase 2 · ✅ already in code.

### Auth & security
- ✅ **Login with username** — `Login` already accepts username *or* email via `Identifier`; just surface it in the UI.
- ✅ **"Email/username already registered" message** — backend already returns `409 Conflict`; shown in the register form (`feat/notifications`).
- 🔵 **Email verification** — needs an email provider; adds `User.EmailVerified`.
- 🔵 **Password recovery** — needs an email provider.
- 🔵 **HttpOnly-cookie refresh tokens** — move refresh token out of JS-accessible storage (security hardening).
- 🔵 **Change JWT `Issuer`/`Audience`** once a real domain exists (do together with CORS/domain setup).

### Groups & invitations
- ✅/🟢 **Group names are NOT unique** — labels only, no existence check (decided).
- ✅ **"You're already in this group" on join** — `JoinGroupAsync` returns `(found, alreadyMember)` tuple; UI shows "Ya eres miembro de este grupo." toast (`feat/notifications`).
- 🟢 **Change admin** — backend `PUT /groups/{id}/admin` done; UI in `feat/group-admin` (rama 9).
- ✅ **Investigate slow groups fetch** — checked during rama 1: `GetUserGroupsAsync` uses AutoMapper `ProjectTo` directly on the `IQueryable`, which compiles to a single flat SQL query (no `Members` collection loaded) — no N+1. The slowness is almost certainly the Render free-tier cold start (~50 s wake).
- 🔵 **Invitation QR code** — generate from the invite code.
- 🔵 **Tweak invite dialog** — group-name display + invite-button colors (polish).
- 🔵 **Extract `ShareGroup` component** out of `GroupCard` (refactor).

### UI/UX
- 🟢 **Branding & design system** — unified Tailwind `@theme` tokens, color/spacing/type scales, dark mode + WCAG AA contrast, Spartan component theming & states, logo usage, personality. See ramas 4 (foundation) & 11 (polish).
- 🟡 **Toast notifications** — implement early (Spartan toast); base for the "already registered" / "already in group" / error messages.
- ✅ **Theme switch** — `theme.service` + `theme.component` exist; wire the toggle.
- 🔵 **Evaluate "Impeccable"** for the UI.
- ✅ **User avatar image** — `User.AvatarUrl` + Supabase Storage upload, DiceBear preset grid, circular canvas crop, `UserAvatarComponent` with `frameColor` ring. Profile settings page at `/profile`.
- 🟡 **Adaptive frame-color palette (light/dark)** — currently `User.FrameColor` stores a hex value (`#8b5cf6`). Plan: migrate to a colour *key* (`"purple"`, `"blue"`, …) stored in DB; define CSS custom properties with distinct light/dark variants in `styles.css` (e.g. `--frame-purple: #7c3aed` in `:root`, `--frame-purple: #a78bfa` in `.dark`); `UserAvatarComponent` resolves key → `var(--frame-purple)`. This ensures good contrast in both themes without client-side branching. Requires a small EF migration + profile-page picker update. Deferred post-MVP-presentation; current hex palette is acceptable for the demo. (Distinct from the streak-tier ring confirmed in rama 19, §13 — that one is animated and driven by per-group streak length, not a user-chosen static color.)
- 🟢 **Multi-language gameplay (i18n)** — **promoted to ramas 23–24 (§13), confirmed 2026-07-16**: play in EN and DE; default language from the browser (`navigator.language` / `Accept-Language`), not IP; per-user `PreferredLanguage` override; translated seeded templates server-side; user-created questions stay in the author's language.

### Daily, rotation & gamification
- 🟢 **Per-group time zone column** — `Group.TimeZoneId` added now (rama 0); logic stays UTC+2 until Phase 2.
- 🔵 **History gating + late voting** — require having answered to see past results; let users answer *late* to unlock. Pairs with coins. **Open decision:** do late votes count in the tally? (MVP leaves history open — §4.6.)
- 🔵 **Activity-aware selector rotation** — weight rotation by recent participation; a member inactive for weeks shouldn't keep being picked and defaulting to an auto-question. Replaces the MVP modulo rotation (§4.4).
- 🔵 **"Race to select"** — if the selector hasn't chosen in the last 10 min before T, notify everyone; the first to pick a question wins coins and becomes the selector. Needs notifications + coins.
- 🔵 **Re-enable `Vote.GuessedCreatorId`** when "adivina el autor" ships (fields already stubbed in `Vote`).
- 🔵 **Monthly statistics** per group/member.
- ✅ **Notification system** (push) — Web Push / VAPID; 4 types (`new_question`, `selector_turn`, `user_voted`, `new_message`); global preferences + per-group mute; nudge prompt; auto-open selector after voting (`feat/notifications`).

### Infra / ops
- 🔵 **Free domain** via name.com + GitHub; then update Issuer/Audience, CORS `AllowedOrigins`, and frontend API base URL.
