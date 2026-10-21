# Vaya Preguntita — Unified Technical & Agent Guide

> **Purpose:** Single source of truth for developers and AI coding agents (Claude, Copilot, etc.).
> **Last updated:** 12 October 2026 · **Status:** MVP feature-complete; post-MVP pass done (§13; ramas 14–27; SignalR rama 13 dropped for polling); Phase 2 planned (§13 "Phase 2 plan") — ramas 28 (invite QR), 29 (capacity study) and 30 (gameplay design, §17) done.
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
16. [Developer Wishlist & Backlog (raw ideas)](#16-developer-wishlist--backlog-raw-ideas)
17. [Phase 2 Gameplay Design (rama 30)](#17-phase-2-gameplay-design-rama-30)

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
- Typography: `DM Serif Display` italic (display headings) + `Quicksand` (UI text) — full rules in §14 "Frontend Design System & Style Guide"
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
- Prefer cheap mechanisms (visibility-aware polling) over heavier infra (WebSockets/SignalR — dropped, see rama 13) — see [§10](#10-real-time-strategy).

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

**Bootstrap exception:** the very *first* question of a brand-new group is the one case that activates immediately instead of waiting for T — when a group reaches 2 members (`GroupsService.JoinGroupAsync`), that question goes live right away so members aren't stuck for up to 24h with nothing to do. It still *closes* at the next real T (today's, if it hasn't passed yet — otherwise tomorrow's), not a full 24h later, so the normal cadence resumes at the very next T instead of skipping a cycle. The next cycle's question is preselected normally for that same T (`PreselectForGroupAsync` is called a second time, dated for the real next-T day; the bootstrap entry itself is dated one day earlier so the two don't collide on the `(GroupId, Date)` key). The bootstrap turn always goes to the **creator** (it spends their round-1 turn, §17.4); the regular entry is the first random draw. From that point on every cycle is again a full, equal T-to-T window. This fires once per group, never on a later selection.

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
2. **Preselect** the *next* day for any group that has ≥ 2 members and no entry yet for that date: draw the next selector (shuffled rounds, §4.4), auto-select a question (group pool first, else base pack), create a `DailyEntry` with `ActivatedAt == null`, `SelectionKind = Auto`.

### 4.4 The selector

**Shuffled rounds (rama 33, §17.4; replaced the MVP's `daysSinceGroupCreation % members` modulo).** Everyone is selector exactly once per round, in an order nobody can predict: each turn is drawn at preselection time (`SelectorRotation.Draw`), at random among the members still due in the group's current round.

- The **creator** is always the first selector (the bootstrap question, §4.1), and that counts as their round-1 turn.
- No back-to-back turns across a round boundary when the group has ≥ 3 members.
- `DailyEntry.SelectorUserId` is written by the draw and is the only source of truth for whose turn it is. Until the tick has drawn the next selector (the ≤ 1-minute window right after T), nobody can choose: `daily/select` answers 403 and `daily/current` returns `selection: null`.
- Keeping the auto-selected question (`POST daily/select/confirm`, "Me vale esta") counts as choosing.
- **Race to select (rama 40, §17.5):** at T − 10 min an unchosen question is up for grabs ("¡Elige tú!"). The winner's round turn stays intact; the drawn selector's is spent.

### 4.5 There is always a question

Once a group has ≥ 2 members, **every day has a question** — the base pack (§6) is the guaranteed fallback when the user pool is empty. `no_question` is therefore **not** a normal in-game state; it only represents the pre-start condition (group has < 2 members, no cycle yet).

### 4.6 Voting rules

- A member votes **once** per question. The vote was **final** in the MVP; since rama 43 (§17.10) it can be **changed once** while the question is open (daily or flash), for 50 🪙 or a vote change from the inventory.
- **No late voting:** once a question closes at T, it is frozen.
- Results are **not anonymous** — the breakdown of who voted for what/whom is shown (`VoterDto`, `OptionResultDto.TargetUser`).
- After a member votes, they see the **live aggregate** (kept fresh by visibility-aware polling every 30 s plus the refresh button, §10). Results of a still-open question are visible to members who have already voted.
- **Result visibility model:** on the *live* day you must vote to see results (drives participation). The **history archive is open** in the MVP — any member sees any past day's results, voted or not. *(Rama 39, §17.7: history gated behind participation; a **late vote** unlocks it and counts in the tally, but not for streak, coins or monthly awards. **Done in rama 39** — the archive is no longer open.)*
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
    "isAutoSelected": true                  // SelectionKind == Auto: not picked, created or confirmed yet
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
| `RangeMin` | `int` | always 1 (fixed — rama 16) |
| `RangeMax` | `int` | always 10 (fixed — rama 16) |

- **Fixed 1–10 range (rama 16, confirmed 2026-10-11):** every Scale — user-created, pack, existing — is rated 1–10. The create form has no range inputs; the server sets the bounds itself (`QuestionMetadata.ScaleMin`/`ScaleMax`) and `CreateQuestionDto` no longer carries `RangeMin`/`RangeMax` (an old client that still sends them is ignored, not rejected). The bounds stay stored and returned so the vote/result components read them as data. The `LockScaleRangeTo1To10` migration rewrote the one pre-existing custom-range question (1–100) to 1–10 and rescaled its votes with `round(1 + (v − min) × 9 / (max − min))`.
- **Vote:** `{ "NumericValue": 7 }` — a raw integer in `[RangeMin, RangeMax]` (i.e. 1–10), **not** an option id.
- **Target is optional:** a Scale can rate a group member (`TargetUserId` set) *or* any free subject described by the text (`TargetUserId` null). The create form offers a "¿Es sobre alguien del grupo?" toggle.
- **Base-pack variant:** pack templates never rate a member — they are self-assessments (*"¿qué tan buen conductor te consideras?"*) or plain opinions, so the clone keeps `TargetUserId` null and the vote screen shows no "Sobre …" line (confirmed 2026-10-08: a random target made first-person texts unreadable).

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

- **`Pack`** — a named collection of templates. Seeds the always-on **"Base"** pack plus 17 thematic packs (14 from rama 15, 3 from the October 2026 content pass; §6.5). `IsActiveByDefault` is the global master switch; per-group enable/disable lives in `GroupDisabledPack` (§6.5).
- **`QuestionTemplate`** — a global question *template* (no votes, no group): `Key` (stable catalog identifier, §6.8), `Text`, `Type`, type config, optional template options, `PackId`, `IsRetired`. Templates are **never voted on directly**.
- When a template is chosen for a group's day (auto or manual), the system **clones** it into a concrete `Question` row inside that group (`GroupId` set, `Source = Pack`, `CreatorId = null`). That `Question` holds the votes. This keeps votes isolated per group and `Question.GroupId` non-nullable.

> Schema change: `Question.CreatorId` becomes **nullable** (`null` ⇒ came from a pack, no human author).

**Creator display convention:** the backend keeps `CreatorId = null` for pack-sourced questions and exposes it as-is. The **frontend** maps that `null` to a label such as *"Vaya Preguntita"* / *"El equipo de Vaya Preguntita"* instead of showing it empty. (Rendered in the UI ramas 5/7/8; the field stays genuinely nullable.)

### 6.3 Auto-resolution of dynamic base questions

Superlative, Secret Pairing, Custom Poll, Scale, and Open Text templates are self-contained (a pack Scale is a self-assessment or a plain opinion; it never rates a member — §5.3). **Deathmatch is not** (it needs teams). For base-pack instances it is resolved **automatically at clone/instantiation time** from the group's current active members:

- **Deathmatch:** `Teams` ← a random split of active members into 2 teams (sizes balanced).

Resolution uses the membership snapshot at the moment of instantiation (clone time), which is why minor staleness (a member joining before T) is accepted in the MVP. Static template metadata (including Custom Poll `AllowOther`) is copied verbatim into the clone. *(Bug fixed on `fix/template-cloner-allow-other`: clones used to drop `AllowOther`. Clones created before the fix keep `AllowOther = false` — confirmed by the developer, no backfill.)* Implemented in `Helpers/TemplateCloner.cs`; the seed packs are synced at startup by `Data/PackSeeder.cs` from the JSON pack catalog (§6.8).

### 6.4 Selection sources

When the selector opens the picker they can choose from: (a) the group's **user-created pool** (`!IsUsed`), and (b) **templates from the group's enabled packs** (§6.5). Or they create a brand-new question inline. Both lists are **cursor-paginated** with a shared type filter and a pack filter — `GET /groups/{id}/questions/pool` (`QuestionPageDto`) and `GET /groups/{id}/packs/templates` (`PackTemplatePageDto`), each `{ Items, HasMore }`, page size default 12 / max 50, `before` cursor on the last seen `Id`. The pool list renders above the pack list to preserve user-pool priority (§6.6). *(This replaced the old unpaginated `GET /daily/selection-sources`, now removed.)*

### 6.5 Thematic packs & per-group enable/disable (rama 15)

Beyond **Base**, 17 thematic packs ship seeded (Humor negro and Guarradas start disabled for new groups): Humor negro, Supervivencia y apocalipsis, Vida nocturna y resaca, Dilemas, Citas/relaciones/red flags, Crimen y misterio, Comida, Deportes y competencia, Polémicas y bandos, Confesiones y vergüenzas, Nostalgia y cringe, Hipotéticos, El reparto del grupo, Guarradas y dilemas asquerosos, and — added in the October 2026 content pass — Qué prefieres: absurdos, Talentos del grupo, and Viajes y planes.

A **`GroupDisabledPack`** row (composite key `GroupId, PackId`) marks a pack disabled for a group; absence means enabled. `Pack.IsActiveByDefault` is the global master switch; effective per-group enablement is `pack.IsActiveByDefault && !GroupDisabledPacks.Any(GroupId, PackId)`. `PacksController` at `api/groups/{groupId}/packs`:
- `GET /` — packs with `{ id, name, description, enabled }` for the group (any member).
- `PATCH /{packId}` — `{ enabled }`, **admin-only**; rejected (400) if disabling would leave the group with **zero** enabled packs (§6.1 invariant).

Admin UI: a per-pack switch list in `GroupSettingsComponent` (optimistic update, revert on error).

### 6.6 Template reuse policy (rama 15)

Replaces the old time-based cooldown. **A template is only ever reused when the group is exhausted** — there is no unused pool question *and* no never-used template across the group's enabled packs. Per group, `usedTemplateIds = Questions.Where(GroupId, TemplateId != null).Select(TemplateId)`; a template is *available* if its `Id` ∉ `usedTemplateIds`, its pack is enabled and it is not retired (§6.8). Shared in `Helpers/TemplateReusePolicy.cs` across all three call sites: the picker templates list (only available ones; once exhausted, all enabled-pack templates become browsable again, least-recently-used first via `DailyEntry.ActivatedAt`), selection validation (`ResolveSelectedQuestionAsync` → `SelectResult.TemplateAlreadyUsed` unless exhausted), and the preselection fallback. Pool-first priority is unchanged: auto-preselection draws from the unused user pool while any remains, cloning a pack template only when the pool is empty.

### 6.7 Phase 2

Pack-aware preselection weighting, and a bundled **"Reparto/Casting"** question type that assigns a whole cast (N characters → N members) in a single question — today the reparto pack uses individual Superlatives instead (§16).

### 6.8 Pack catalog (JSON) & template retirement

The pack content lives in `server/VayaPreguntita.API/Data/Packs/*.json` (one file per pack, embedded in the assembly; format and editing rules in `Data/Packs/README.md`). `PackSeeder.SyncCatalogAsync` runs at startup after `MigrateAsync` and syncs it into the database:

- **Identity is `QuestionTemplate.Key`** (unique, never changes), not the text. Editing a wording or moving a template between packs updates the same row, so `Question.TemplateId` references of clones already living in real groups stay valid.
- **Packs** are found-or-created by `Name`; `Description` and `DisabledByDefault` follow the catalog. `Order` fixes the creation order.
- **Rows are never deleted.** An entry marked `"retired": true` sets `QuestionTemplate.IsRetired`: it is never offered in the picker, never auto-selected and never counts as available for the reuse policy (§6.6). Removing the flag brings it back.
- **Rows created before the catalog existed** have no `Key`. The first sync *adopts* them by matching `(pack, text)` — or the entry's one-time `legacyPack` / `legacyText` hints when the wording or pack has changed since — and stamps the key.
- A template's `Type` cannot change in place (the sync fails loudly): add a new entry and retire the old one.
- The catalog is validated on load (unique keys and texts, poll option counts, selection limits, unknown properties…); an invalid catalog aborts the sync without breaking startup (logged as a warning).

Retiring a template is a content decision, not a data migration: it needs no EF migration, only a catalog edit and a deploy.

---

## 7. Data Models & Key DTOs

### 7.1 Entities (current + planned)

- **User** — `Id, Username, Email, PasswordHash, AvatarUrl, FrameColor, StreakFrameAutoApplied, PreferredLanguage, Coins, DateJoined`. `Coins` is the global coin balance (rama 34, check `>= 0`). `FrameColor` is a hex colour, `"streak"` (ring follows the streak tier — the default; `null` is read as `"streak"`) or `"none"` (rama 19). `PreferredLanguage` is `"es"`, `"en"` or `"de"` (rama 23; non-null, default `"es"` — accounts created before rama 23 got Spanish): the UI language the account follows on every device and the language its push notifications are written in. Refresh tokens live in `UserRefreshToken`, not on `User` (multi-device support).
- **UserRefreshToken** — `Id, UserId, TokenHash, PreviousTokenHash, ExpiresAt, CreatedAt`. One row per device/session; up to 5 concurrent sessions per user (least recently used — earliest `ExpiresAt` — evicted on new login). `Jwt:RefreshTokenDays` = 365, **sliding**: every refresh pushes `ExpiresAt` forward, so a device only has to log in again after a year without opening the app. The client only ends a session when `/refresh` answers 400/401; network errors, timeouts and 5xx (e.g. during a Render redeploy) keep the stored tokens. Rotation keeps the token the row held before as `PreviousTokenHash`, which `/refresh` and `/logout` still accept — with no time limit — until the client refreshes with the current token; so a refresh whose response never reached the device (app closed or locked mid-request, dropped connection) does not end the session, even if the device only retries days later. Replaying the previous token rotates again but keeps the same fallback.
- **Group** — `Id, Name, Description, InvitationCode, DailyQuestionTime, TimeZoneId (nullable, default 'Europe/Madrid'; MVP logic uses UTC+2), DateCreated, CreatorId`, plus `Members`, `Questions`, `DailyEntries`. **Names are not unique** (labels only). `AdminId` was removed in rama 10 — admin identity lives on `GroupMember.IsAdmin`.
- **GroupMember** — composite key `(GroupId, UserId)`, `JoinedAt` (drives rotation), `IsAdmin` (bool; exactly one member per group is admin at all times), `NotificationsMuted`, and the rama 19 streak columns `CurrentStreak, BestStreak, LastStreakEntryId, StreakDangerNotifiedEntryId`. The stored `CurrentStreak` is only *effective* while `LastStreakEntryId` is the open entry or the previously activated one (`StreakService`). Rama 35 (§17.9, migration `AddStreakFreeze`) adds `StreakFreezes` (0–2), `LastFreeFreezeMonth` and `LastRepairMonth` (local month as `yyyyMM`), `FreezeUsedEntryId` and `FreezeGiftFromUserId` (notices, cleared on dismiss), `LostStreakValue` + `LostStreakEntryId` (repair window).
- **Question** — `Id, Text, Type, Source, IsUsed, DateCreated, DateActivated, Metadata (JSONB), GroupId, CreatorId (→ nullable), Options, Votes`.
- **Option** — poll option (`Id, Text, QuestionId`).
- **Vote** — `Id, DateResponded, QuestionId, UserId`, and the polymorphic answer fields: `SelectedOptionId`, `SelectedTargetUserId`, `NumericValue`, `FreeText` (used by Open Text answers and Custom Poll "Otro" answers). Unique index `(QuestionId, UserId)`.
- **DailyEntry** — `Id, Date, SelectionKind, PreselectedAt, ActivatedAt (nullable), GroupId, QuestionId, SelectorUserId`. Unique index `(GroupId, Date)`. `SelectorUserId` is drawn at preselection time (§4.4) and is the only source for who may call `POST /daily/select` / `select/confirm`. `SelectionKind { Auto, Confirmed, Picked, Created }` (rama 33, replaced the `IsAutoSelected` bool; migration `AddShuffledSelectorRotation`: `true → Auto`, `false → Picked`).
- **CoinTransaction** (rama 34, §17.2) — `Id, UserId (cascade), Amount (±), Reason (CoinReason), GroupId (nullable, set null on group delete), RefId (nullable), CreatedAt`. Unique index `(UserId, Reason, RefId)`, index `(UserId, CreatedAt)`. `DailyEntry.FirstVoterUserId` (nullable) records who claimed the first-voter bonus.
- **Race to select** (rama 40, §17.5; migration `AddRaceToSelect`) — `DailyEntry.RaceOpenedAt` (nullable, set at T − 10 min while still `Auto`); `NotificationPreferences.RaceToSelect` (bool, default true).
- **MonthlySummary** (rama 41, §17.12; migration `AddMonthlySummary`) — `Id, GroupId (cascade), Month (date, first local day), Payload (JSONB), CreatedAt, NotifiedAt (nullable)`. Unique index `(GroupId, Month)`. Payload stores user ids only (names/avatars resolved when read). `NotificationPreferences.MonthlySummary` (bool, default true).
- **UserItem** (rama 42, §17.10; migration `AddShopCosmetics`) — `UserId (cascade), ItemKey (≤ 40, a ShopCatalog key), PurchasedAt`, PK `(UserId, ItemKey)`: a cosmetic owned forever. The same migration adds the equipped slots to **User**: `RingSkinKey, AccessoryKey, NameColorKey, ThemeKey` (nullable, ≤ 40; each an owned key of that kind). `BadgeKey` was dropped and shop titles retired by `RetireBadgesAndShopTitles` (rama 46, §17.15). `User.FrameColor` also accepts `"seniority"`.
- **VoteChange** (rama 43, §17.10; migration `VoteChangeNudgesAndRetireAccessories`) — `QuestionId (cascade), UserId (cascade), ChangedAt`, PK `(QuestionId, UserId)`: a member changed their vote on that question; the key is the once-per-question guard (coins or inventory alike). The same migration adds **Nudge** — `Id, GroupId (cascade), QuestionId (cascade), SenderUserId (cascade), TargetUserId (cascade), Message (≤ 80, nullable), SentAt`, index `(GroupId, TargetUserId, SentAt)` for the 24-h rule — and `NotificationPreferences.Nudge` (default true); it drops `User.AccessoryKey` (§17.15 item 7).
- **Selector rotation** (rama 33) — `Group.SelectorRound` (int, starts at 1) + `GroupMember.LastSelectorRound` (int). The migration starts every existing group at round 1 with everyone due, except the member already holding the pending turn.
- **Pack** — `Id, Name, Description, IsActiveByDefault`. Seeds **Base** + 17 thematic packs (14 from rama 15, 3 added October 2026).
- **QuestionTemplate** — `Id, Key, Text, Type, Metadata, PackId, IsRetired`, optional template options. Global, voteless. `Key` is the stable catalog identifier (§6.8).
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
| `POST` | `/api/auth/register` | rate-limited; optional `preferredLanguage` (the device's UI language; unsupported/missing → `es`); response carries `preferredLanguage` |
| `POST` | `/api/auth/login` | rate-limited; response carries the account's `preferredLanguage` (it wins over the device's) |
| `POST` | `/api/auth/refresh` | rotates refresh token |
| `POST` | `/api/auth/logout` | rate-limited |
| `GET` | `/api/auth/check-username` · `/check-email` | availability checks |
| `POST` | `/api/auth/google` | 🅿️ Google OAuth |

**Language (rama 23):** the client sends its UI language as `Accept-Language` on every API call; player-facing error messages answer in it (`es`/`en`/`de`, anything else → `es`; `UseRequestLocalization` sets only the UI culture — number/date formatting stays invariant) from the `Localization/ServerText` catalog. Developer-facing contract errors (structural validator rules the UI never lets through) stay in English. `GET /api/users/me` returns `preferredLanguage`; `PUT /api/users/me { preferredLanguage }` changes it (400 if unsupported). Push notifications are written in each recipient's `PreferredLanguage`.

### Groups

| Method | Endpoint | Status | Notes |
|---|---|---|---|
| `GET` | `/api/groups` | ✅ | groups of the current user. 1.24.0: each carries `hasNewActivity` (§16 unread-activity dot) |
| `POST` | `/api/groups/{groupId}/seen` | ✅ | 1.24.0 — the caller has the group open: sets `GroupMember.LastSeenAt` (clears the unread-activity dot); 204, 404 not a member |
| `POST` | `/api/groups` | ✅ | create; creator = admin = first member |
| `GET` | `/api/groups/{groupId}` | ✅ | details |
| `PUT` | `/api/groups/{groupId}` | ✅ | admin: edit name/description/time |
| `POST` | `/api/groups/join` | ✅ | `{ invitationCode }` |
| `PUT` | `/api/groups/{groupId}/admin` | ✅ | transfer admin |
| `GET` | `/api/groups/{groupId}/members` | ✅ | `{ id, username, avatarUrl, frameColor, joinedAt, isAdmin, isCurrentUser, notificationsMuted, currentStreak }` |
| `GET` | `/api/groups/{groupId}/members/{userId}/stats` | ✅ | member card (rama 19): current/best streak, votes, participation %, times selector, questions created; rama 36: `guessesMade`, `guessesCorrect`; rama 47: `duelsWon`, `duelsDrawn`, `duelsLost` (§17.15 item 6) |
| `DELETE` | `/api/groups/{groupId}/members/me` | ✅ | leave group |
| `DELETE` | `/api/groups/{groupId}/members/{userId}` | ✅ | admin: kick member |
| `POST` | `/api/groups/{groupId}/invite-code/regenerate` | ✅ | admin: rotate invitation code |
| `DELETE` | `/api/groups/{groupId}` | 🅿️ | delete group |

### Daily (core flow)

| Method | Endpoint | Status | Notes |
|---|---|---|---|
| `GET` | `/api/groups/{groupId}/daily/current` | ✅ | redesigned → `{ today, selection, myStreak }` (§4.7; `myStreak` added in rama 19); driven off `ActivatedAt` state. Rama 36: `today` gains `voterIds` (who has voted, never their answers), `memberCount` and `guess` (§17.6, once you have voted); `selection.selectorUserId`/`selectorUsername` are only sent to the selector themself. Rama 40: `selection.raceOpen` (during the race `pendingQuestion` goes to everyone, §17.5). Rama 44: `flashes` — the open flash questions, oldest first (§17.11) |
| `POST` | `/api/groups/{groupId}/daily/select` | ✅ | sets the **pending (next-day)** question; never activates early; inline-create runs full §9 validation. Pool/pack pick → `SelectionKind.Picked`, inline create → `Created` (editing it in place keeps `Created`). Rama 40: returns `{ coins }` (non-null only for the race winner, §17.5); during the race any member may call it, 409 "Alguien se te ha adelantado" once someone has chosen |
| `POST` | `/api/groups/{groupId}/daily/select/confirm` | ✅ | rama 33 — the selector keeps the auto-selected question ("Me vale esta"): `Auto → Confirmed`, idempotent (an already-picked entry is left as is); 403 if not the drawn selector. Rama 40: same `{ coins }` response and race rules as `select` |
| `POST` | `/api/groups/{groupId}/daily/vote` | ✅ | submit vote; returns `{ results: QuestionResultDto, streak: StreakUpdateDto, coins: { earned, balance, capReached } }` (rama 19 — before/after streak + tier info for the celebration; rama 34 — coins paid, §17.2); rama 36 — `guess`, the \"¿Quién la escribió?\" step or null |
| `PUT` | `/api/groups/{groupId}/daily/vote` | ✅ | rama 43 — change my vote on today's open question (§17.10), once, body = `CreateVoteDto`; `?useOwned=true` spends one from the inventory → `{ results, balance, inventory }`; 400 not voted / invalid, 409 already changed / not enough coins / none owned. `daily/current.today.canChangeVote` (and `flashes[].canChangeVote`) says whether it is still available |
| `GET` | `/api/users/me/coins/history?before=` | ✅ | rama 34 — the coin ledger, newest first, 20 per page (§17.2) |
| `POST` | `/api/groups/{groupId}/daily/guess` | ✅ | rama 36 — `{ creatorId }` or `{ skip: true }` → `{ guess, coins }`; once per question, only after voting and while it is open; 400 not guessable / vote first / not a candidate, 409 already guessed (§17.6) |
| `POST` | `/api/groups/{groupId}/daily/guess/hint` | ✅ | rama 36 — author hint, 20 🪙 → `{ guess, balance, inventory }` with half of the wrong candidates removed; buying it again charges nothing; 409 not enough coins, 400 not available (§17.10). Rama 45: `?useOwned=true` spends one from the inventory instead (409 none left) |
| `POST` | `/api/groups/{groupId}/daily/streak/dismiss-lost` | ✅ | acknowledges the lost-streak notice (`daily/current` → `myStreak.lostStreak`) |
| `POST` | `/api/groups/{groupId}/daily/streak/dismiss-freeze` | ✅ | rama 35 — acknowledges "your freeze saved your streak" (`myStreak.freezeSavedStreak`) |
| `POST` | `/api/groups/{groupId}/daily/streak/dismiss-gift` | ✅ | rama 35 — acknowledges "X gave you a freeze" (`myStreak.freezeGiftFrom`) |
| `POST` | `/api/shop/purchase` | ✅ | rama 35 — `{ item: "streakFreeze" \| "streakRepair", groupId, targetUserId? }` (a freeze with another member as `targetUserId` is a gift; your own id as `targetUserId` → 400) → `{ balance, myStreak, inventory }`; 409 not enough coins / freeze limit (yours or the receiver's) / repair not available or already used this month; 400 target not in the group; 403 not a member (§17.9). Rama 45: `useOwned: true` takes the freeze or repair from the inventory instead of paying (409 none left). |
| `GET` | `/api/shop/inventory` | ✅ | rama 45 — the items you hold: `{ flashQuestion, streakFreeze, authorHint, streakRepair, voteChange, nudge }` (the last two since rama 43) (also in `GET users/me` as `inventory`) (§17.10) |
| `POST` | `/api/shop/inventory` | ✅ | rama 45 — `{ item }` (`InventoryItem` int: 0 flash, 1 freeze, 2 hint, 3 repair, 4 vote change, 5 nudge) buys one at its normal price → `{ balance, inventory }`; 409 not enough coins (§17.10) |
| `GET` | `/api/shop` | ✅ | rama 42 — the cosmetics catalog: `{ cosmetics: [{ key, kind, price, rarity, owned }] }` (`kind`: 0 ring skin, 1 accessory, 2 name colour, 5 theme — 3 badge and 4 title are retired, rama 46; `rarity` 0–3) (§17.10) |
| `POST` | `/api/shop/cosmetics` | ✅ | rama 42 — `{ key }` buys a cosmetic once → `{ balance, ownedCosmetics }`; 409 already owned / not enough coins; 400 unknown key. Equipping is `PUT users/me` with `ringSkinKey`, `accessoryKey`, `nameColorKey`, `themeKey` (`""` takes it off; 400 not owned) (§17.10; badges and shop titles retired in rama 46, §17.15) |
| `POST` | `/api/groups/{groupId}/flash` | ✅ | rama 44 — buy and launch a flash question at once (`CreateQuestionDto`, §9 validation; 100 🪙 since rama 46 (was 500), one per member per group per local day) → `{ flash, balance, inventory }`; 409 not enough coins / already launched one today, 400 group not started or question not valid for the group (§17.11). Rama 45: `?useOwned=true` launches it with one from the inventory (409 none left) |
| `POST` | `/api/groups/{groupId}/flash/{flashId}/vote` | ✅ | rama 44 — vote a flash question: while open → the fresh flash state (no streak, coins or push); once closed → late vote from the history (`IsLate`, 204) |
| `PUT` | `/api/groups/{groupId}/flash/{flashId}/vote` | ✅ | rama 43 — change my vote on an open flash, same rules → `{ flash, balance, inventory }`; 400 once it has closed |
| `GET` | `/api/groups/{groupId}/nudges/targets?flashId=` | ✅ | rama 43 — members who haven't voted the question (you excluded; no `flashId` = today's): `[{ userId, status }]` with `status` one of `available`, `recent`, `unreachable`. `unreachable` covers no push device, nudges off and the group muted, on purpose without telling them apart |
| `POST` | `/api/groups/{groupId}/nudges` | ✅ | rama 43 — `{ targetUserId, flashId?, message?, useOwned }` (message ≤ 80) → `{ balance, inventory }`; 409 already voted / unreachable / nudged in the last 24 h / not enough coins / none owned |
| `GET` | `/api/groups/{groupId}/flash/{flashId}` | ✅ | rama 44 — history detail of a flash question, gated like a daily one (§17.7); `author` = who launched it |
| `GET` | `/api/groups/{groupId}/monthly-summaries` | ✅ | rama 41 — the group's summaries, newest first, last 12 months: `[{ month: "YYYY-MM", questions }]` (§17.12) |
| `GET` | `/api/groups/{groupId}/monthly-summaries/{YYYY-MM}` | ✅ | rama 41 — one month: `{ month, questions, participation, awards: [{ key, value, winners: [{ person, questionText }] }], members: [{ person, answered, eligible, percent, bestRun }], myMatch: { people, count } \| null }`; `myMatch` is the caller's only; 404 no summary for that month (§17.12) |
| `GET` | `/api/groups/{groupId}/daily/selection-sources` | 🆕 | questions the selector can pick (pool + base pack) |

### Questions (pool & history)

| Method | Endpoint | Status | Notes |
|---|---|---|---|
| `POST` | `/api/groups/{groupId}/questions` | ✅ | add to pool |
| `GET` | `/api/groups/{groupId}/questions/pool` | ✅ | unused user questions |
| `DELETE` | `/api/groups/{groupId}/questions/{id}` | 🆕 | creator/admin: remove from pool |
| `GET` | `/api/groups/{groupId}/questions?date=YYYY-MM-DD` | ✅ | history detail for one **activated** date (a preselected future one is 404). Gated (rama 39, §17.7): until you vote it, `locked: true` with empty `results`/`freeTextResponses` (text, `totalVotes` and author stay), plus `lateVoteQuestion` (a `QuestionToVoteDto`) once it has closed |
| `POST` | `/api/groups/{groupId}/history/{date}/vote` | ✅ | rama 39 — late vote on a closed question (`CreateVoteDto`, same §9 validation; targets must be current members). Stored with `Vote.IsLate`; no streak, coins or push. Returns the unlocked detail. 404 no activated entry · 400 still open / already voted / invalid |
| `GET` | `/api/groups/{groupId}/history` | 🆕 | paginated list of past entries `{ date, questionText, type, totalVotes }`. Rama 44: closed flash questions are merged in by launch date with `flashId` (open them with `GET flash/{flashId}`) |

### Real-time / Infra

| Method | Endpoint | Status | Notes |
|---|---|---|---|
| `GET` | `/healthz` | ✅ | health check |

---

## 9. Validation Rules

Validators live in `Validators/` (FluentValidation). Vote-payload validation is enforced in `DailyService` (`ValidateVotePayload`).

### `CreateQuestionDto` — type-specific

| Type | Rule |
|---|---|
| Superlative | `BlacklistedUserIds` must not include the creator; ids must be group members |
| Deathmatch | no user id in more than one team; 2–4 teams, each 1–4 members |
| Scale | no range fields (fixed 1–10, set server-side); `TargetUserId` **optional** — if set, must be a group member |
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

**Visibility-aware polling (rama 27, release 1.11.1) — frontend only, no backend change.** The game is asynchronous (one question per day) and chat is lightly used, so a push channel isn't worth its cost; SignalR (rama 13) was dropped on 2026-10-12 (JWT over the query string, 15-min token reconnects, hub group authorization, PWA sockets dying in the background and redeploys dropping connections on Render's free tier).

| What | Mechanism |
|---|---|
| Group chat | re-fetch `chat` every **15 s** while the chat component is mounted and the page is visible |
| Daily state | re-fetch `daily/current` every **30 s** only while the group is in the `voting` or `results` state, or has an open flash question (rama 44), and the page is visible (vote count / result bars update by themselves) |
| Hidden page | **no requests**; when the page becomes visible again both refresh immediately and the interval resumes |
| Manual | refresh button and pull-to-refresh stay as they are |

Rules: refreshes are *silent* (no skeleton/spinner), never overlap (`exhaustMap`), never reset a vote in progress or close an open dialog, never touch a message being typed, and keep the scroll position unless the reader is already at the bottom of the chat. The helper is `visiblePolling(intervalMs)` in `client/src/app/shared/utils/visible-polling.ts`.

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
| Scale range | fixed `1–10` (rama 16) |
| Deathmatch | 2–4 teams, 1–4 members each |
| Invitation code | 6 alphanumeric chars |
| Rate limiting | existing `AuthLimiter` (10 / 5 min) on auth; add per-user limiters on `join`, `vote`, `select`, question creation |

> **Group names are not unique** — they are labels; identity is the `Id` + invitation code. No name-existence check on create.

### 11.1 Capacity on the free tiers (rama 29, measured 2026-10-12)

**Question answered:** how many people can play on the current infrastructure, which limit breaks first, and what budget must new features respect. Numbers marked *measured* come from a throwaway local rig (below); *estimated* ones are arithmetic on top of them or on vendor docs — re-check vendor numbers at the start of any rama that depends on them, they change.

**Verdict.** The first wall is **Render's 0.1 vCPU**, not RAM, database size or bandwidth. One API instance serves comfortably **~60 simultaneously visible players** (≈6 req/s), degrades at ~120 (≈12 req/s, daily p95 ≈ 1.4 s) and saturates at ~250 (≈25 req/s, p50 of 7–13 s). With a typical once-or-twice-a-day usage pattern that is roughly **a few hundred daily-active players (≈ 30–50 groups of 10)** — *estimated*, assuming a 10–15 % concurrency peak around the daily question time. Beyond that the answer is "optimize (rama 31) or pay", not "tune a knob".

#### Load per player
A player with the group screen open and the tab visible costs **0.1 req/s** (chat every 15 s + `daily/current` every 30 s, §10); a hidden tab costs nothing. `daily/current` responds with ~2 kB; chat with a few hundred bytes at most (2 B when empty). Outbound traffic is therefore ≈ **0.3 MB per visible player-hour** (*estimated*).

#### Measured rig (all local, never production)
Throwaway PostgreSQL 15 container + the real API image built from `server/Dockerfile` run with `--cpus 0.1 -m 512m` (Render free web service shape), 30 groups × 10 members (300 users, 190 votes), open-loop requests with a 2:1 chat/daily mix over 20 s per step. Caveats: the Postgres is local (no network round trip, unlike Supabase — requests wait longer but CPU use is similar), a Docker CPU quota is a proxy for Render's, and the first step includes JIT warm-up.

| Offered load | ≈ visible players | chat p95 | `daily/current` p95 | Verdict |
|---|---|---|---|---|
| 3 req/s | 30 | 3.5 s (warm-up outlier; p50 0.16 s) | 4.8 s (same) | ok once warm |
| 6 req/s | 60 | 0.27 s | 0.64 s | **comfortable** |
| 12 req/s | 120 | 0.72 s | 1.4 s | degraded |
| 25 req/s | 250 | 8.8 s | 17 s | **saturated** |

Same API on the host without the CPU cap: 400 req/s (~4 000 visible players) at p95 90 ms / 170 ms — the code is not slow, the CPU slice is tiny. Memory stayed at ~150 MiB of 512 MiB.

#### Limit inventory (in the order they bite)

| # | Limit | Free-tier number | Our footprint | Status |
|---|---|---|---|---|
| 1 | **Render CPU** | 0.1 vCPU, 512 MB, single instance, spins down after 15 min without traffic (~1 min to wake) | saturates at ≈12–25 req/s (above) | **First wall** |
| 2 | **Render instance hours** | 750 h/month per workspace; always-on = ~720 h | the UptimeRobot keep-alive uses ~96 % of it | Only **one** always-on free service fits; a second API/worker would suspend both |
| 3 | **Render outbound bandwidth** | counts against a monthly quota the docs don't state (check the dashboard); overage suspends the service without a payment method | ≈0.3 MB per visible player-hour → a few GB/month only at thousands of DAU | Watch; unlikely before #1 |
| 4 | **Supabase connections** | not published for the free plan | Npgsql's default `Maximum Pool Size` is 100; against a Postgres with `max_connections=100` the idle pool alone locked out even `psql` for 15 s after a 300-user burst (*measured*) | **Action for rama 31:** cap the pool in the connection string (~10–15) and prefer the pooler endpoint |
| 5 | **Supabase DB size** | 500 MB | ~10 MB with the schema, the base pack and 300 users (*measured*); a group-day (entry + question + options + 10 votes) ≈ 3–4 kB (*estimated*) → ≈1.3 MB per group-year | Not a limit before thousands of groups; chat/votes grow unbounded, so cap history depth in rama 31 |
| 6 | **Supabase egress** | 5 GB/month (+5 GB cached) | API↔DB traffic is the same order as the responses | Not a limit before #1 |
| 7 | **Supabase pause** | paused after 1 week of inactivity | the 1-minute tick queries the DB as long as the API is awake | Safe while the keep-alive runs; a paused DB needs a manual restore |
| 8 | **Supabase Storage** | 1 GB, 50 MB per file | avatars (cropped, small) | Not a limit |
| 9 | **Vercel Hobby** | ~100 GB fast data transfer, ~1 M edge requests/month (third-party figures; confirm on vercel.com/pricing) | static bundle only (~569 kB uncompressed initial JS, rama 31 shrinks it) | Not a limit |
| 10 | **Web Push fan-out** | outbound HTTP from the same 0.1 vCPU | `NotificationService` sends fire-and-forget from the tick; groups sharing the same daily time all fire in the same minute | **Not measured** — a burst of activation pushes at a popular time (e.g. 12:00) competes with request handling; measure before relying on it |

#### Budget for new features (gameplay ramas 33+ and rama 31)
- **Steady-state ceiling: ~8 req/s per instance** at peak (≈ 80 visible players). All polling together must stay under it; adding a poll to every visible player costs 1 req/s per 10 players.
- **No new polling faster than 30 s**, always `visiblePolling`, and never on a screen the player does not have open. Prefer folding new data into `daily/current` over adding a second endpoint polled in parallel.
- **No new per-minute work that scales with total history.** The tick must stay O(groups due now). Known offender: `StreakService.SendDangerRemindersAsync` groups *all* activated `DailyEntries` every minute — fine at 60 rows, a full scan at 365 k (1 000 groups × 1 year); give it an index/narrower query in rama 31.
- **Every new list endpoint needs a cap and an index** (history, monthly stats, rankings): compute on demand only if bounded, else snapshot at month end.
- **Fan-out features (push, group streaks, coins payouts at close)** must be batched per tick, not per player.
- **`daily/current` is the hot path**: it loads group + members + users (`Include`) on every call. Every extra join here is multiplied by the poll rate — measure before adding.

#### Cheap levers if capacity gets tight (candidates for rama 31)
Poll `daily/current` every 60 s once a player has voted (halves its load); stop polling after results are shown and the day is closed; cache the member list per group in memory for the duration of a tick/poll window; `Maximum Pool Size`; trim the `Include` chain in `GetCurrentStatusAsync`; response compression.

#### Reproducing the measurements
Start a disposable Postgres (`docker run -p 55432:5432 postgres:15-alpine`), run the API against it (it migrates and seeds itself on start; use a fake VAPID key pair and a high `RateLimiting:AuthPermitLimit`), register the users/groups through the public API, then drive `daily/current` and `chat` open-loop at stepped rates. The scripts used live in `tools/loadtest/` (`seed.mjs`, `load.mjs`, Node 20+, no dependencies). **Never point them at the production API or database.**

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
| Lost-refresh logout fix (2026-10-16, release 1.22.1): `/refresh` rotated the token in place and the old one stopped working at once, so when the rotated token never reached the device (PWA closed or phone locked mid-request, connection dropped) the device was left holding a dead token and was logged out on its next refresh — confirmed in production as orphaned sessions that kept a live row on the server. The row now keeps `PreviousTokenHash` (migration `AddRefreshTokenRotationGrace`), accepted until the client refreshes with the current token. | ✅ |
| Daily-time-change transparency (§4.8): editing `Group.DailyQuestionTime` mid-cycle — confirmed the live `closesAt`/`activatesAt` already reschedule today's still-pending question to the new T; when today already activated, the change applies next cycle (no re-anchor / no early close). `PUT /groups/{id}` now returns `GroupResponse.DailyTimeChangeAppliesFromTomorrow` (time changed **and** a question activated during today's local UTC+2 day, measured off `ActivatedAt`); group-settings shows an informational toast. | ✅ — rama 20 (`fix/daily-question-time-change`) |
| Pack catalog & template retirement (§6.8): pack content moved from C# builders to embedded JSON (`Data/Packs/*.json`); `PackSeeder` now syncs by a stable `QuestionTemplate.Key` (adopts legacy rows by `(pack, text)`, rewrites/moves in place, never deletes) and `IsRetired` hides templates everywhere (picker, preselection, reuse policy) — migration `AddQuestionTemplateKeyAndIsRetired`. First content pass (confirmed 2026-10-08): 43 weak/duplicate templates retired and 9 reworded in place (6 open-text prompts shortened to a one-sentence answer, 3 rewritten for wider audiences), one of them moved to Humor negro. The same day 74 templates were added: three new packs (Qué prefieres: absurdos, Talentos del grupo, Viajes y planes) and additions to Base, Comida, Confesiones, Deportes, Polémicas and Vida nocturna. Pack Scale questions no longer get a random target member (`TemplateCloner`; §5.3/§6.3) — they are self-assessments. Catalog + seeder + cloner covered by `PackCatalogTests` / `PackSeederTests` / `TemplateClonerTests`. **Merged ([#53](https://github.com/alexsepulvedaramos/Preguntitas/pull/53)); migration applied.** | ✅ |
| About & release notes (rama 26): public `/about` ("Cómo funciona": daily cycle, the six question types, packs, streak frames/titles/crown rendered with the real `UserAvatarComponent`, the register-page results teaser) and `/releases` ("Novedades": every release, newest first, written for players) — both readable without an account, inside the main layout; "Cómo funciona" / "Novedades" entries in the user-menu drawer, plus version + links in the desktop footer. Versioning per §14 "Releases": `client/src/app/core/releases/releases.ts` is the single list (first entry = `APP_VERSION`), history rebuilt from the merge dates (1.0.0 launch 2026-06-24 → 1.5.0). "What's new" state per device in localStorage (`ReleaseNotesService`): dot on the header avatar + "Nuevo" badge until `/releases` is opened; a one-off toast per release only for devices that had already opened earlier notes. No backend, no migration. | ✅ — rama 26 (`feat/about-and-releases`) |
| Public landing & onboarding (rama 21): `/` is now a guest-only landing (`LandingPageComponent`, own light layout — logo, theme, "Entrar"; hero + register CTA + the looping results teaser; "Cómo funciona" in 3 steps; four sample streak frames with the crown; final CTA + link to `/about`; footer with version/links). `guestGuard` sends signed-in users on to `/groups`; the `**` fallback and the info pages' back-button fallback now go to `/` (landing for guests, `/groups` otherwise); the header logo links to `/` for guests. The PWA `start_url` stays `/groups`. Start gate made explicit (§4.3): the empty group list shows a 3-step "how to start" walkthrough; a group with no question yet (< 2 members) shows "Invita a un amigo para empezar" with a share-invite button (native share sheet, link-copy fallback) and the code; group cards in that state show an "Invita a alguien para empezar" pill. Invite sharing extracted to `features/groups/utils/invite-share.ts` (also fixes the group card's share text, which was in English). Frontend only, no migration. Release 1.6.0. | ✅ — rama 21 (`feat/public-onboarding`) |
| PWA update prompt (release 1.6.1, 2026-10-11): the Angular service worker downloaded new builds but kept serving the cached one until every tab was closed, and an installed PWA on a phone is resumed rather than reopened, so players stayed on old versions. `AppUpdateService` (created by the app root at startup, so it also covers the landing and auth pages) calls `SwUpdate.checkForUpdate()` whenever the app returns to the foreground (`visibilitychange` → visible) and hourly while open; on `VERSION_READY` the root dynamically imports `AppUpdatePromptComponent` — a Spartan card "Hay una versión nueva · Actualizar / Luego" that activates the update and reloads — so its Helm imports never reach the initial bundle (+5 kB at startup); an `unrecoverable` cache reloads straight away. "Luego" hides it until the next app load, which picks the new version up anyway. **Rule (developer, 2026-10-11): never trade the design system for bundle size** — keep Spartan components and shrink the bundle by lazy-loading instead. Players still on ≤ 1.6.0 need one last full close of the app to receive it. | ✅ |
| Interactive demo (rama 25, release 1.7.0): public `/demo` (`DemoPageComponent`, own light layout like the landing, no guard — guests and signed-in users) plays one simulated day in the fictional group «Los de siempre» with mock data and no API calls: optional nickname → today's Superlative («¿Quién intentaría abrazar a un zombie en un apocalipsis?», real Base template) with the live countdown → scripted live results (3 of 4 friends vote for the visitor, Gus votes Carlos; results show right after voting, as in the real app §4.6) with the real vote-summary dialog → "Mañana eliges tú" light mock with 3 Base pack questions → "Dos días después…" → the real `StreakCelebrationComponent` forging the wood frame → the crown passes to the visitor (Eva lost a 9-day streak), four higher frames with their titles, and a register / "Ir a mis grupos" CTA. Reuse: the per-type voting UI was split out of `VoteComponent` into a presentational `VoteFormComponent` (`app-vote` keeps the POST); `StreakCelebrationComponent` takes optional `username`/`avatarUrl` inputs; the demo provides its own `GroupStreaksService`/`MemberCardService` instances so the fictional group never touches the app-wide caches and avatar taps are inert. All demo copy and data live in `features/demo/demo-content.ts` (ready for ramas 23–24). Entry points: landing (hero + final CTA), empty group-list state, `/about`, and a discreet link under the login/register forms. Frontend only, no migration. | ✅ — rama 25 (`feat/demo-experience`) |
| Push & invite fixes (rama 17, release 1.8.0): one notification per group (custom `sw.js` wrapping `ngsw-worker.js` merges a group's pushes, skips them while the app is visible, the app clears delivered notifications on open/foreground); per-type push TTL; logged fire-and-forget dispatches; logout detaches the device's subscription; `GroupResponse.MyStreak` + "No pierdas tu racha" pill on the group card; invite link register-and-join with auto-join; light backing for croodles avatars; manifest `id`. No migration. | ✅ — rama 17 (`fix/avatar-dark-mode-and-join-flow`) |
| Mobile group header (rama 18, release 1.9.0): on mobile (< `sm`) the global bar is hidden inside a group (`HeaderComponent.inGroup` from the URL) and each group screen's own top row becomes the sticky app bar via the shared `appMobileTopBar` directive (detail, history, settings); desktop unchanged. The remaining global bar is logo left · avatar right; the theme button stays there only for guests. `ThemeService` now holds a `light`/`dark`/`system` preference (`system` = no stored value, follows `prefers-color-scheme` live), picked from a Claro/Oscuro/Sistema toggle group in the user-menu drawer. No backend change. | ✅ — rama 18 (`feat/mobile-group-header`) |
| Scale fixed to 1–10 (rama 16, release 1.9.1): range inputs removed from the create form, `CreateQuestionDto.RangeMin/RangeMax` dropped (server sets 1–10), pack catalog `rangeMin`/`rangeMax` removed; data migration `LockScaleRangeTo1To10` rescaled the only custom-range question and its votes | ✅ — rama 16 |
| Core-domain tests & CI (rama 22): lifecycle code reads the time only through an injected `TimeProvider` (`DailyClock.UtcNow/Today/TimeOfDay(clock)`, `TimeProvider.System` registered in DI) and the background tick moved into a scoped `DailyCycleRunner` that `DailyPreselectionService` runs every minute — no behavior change. Domain tests run the real services against PostgreSQL (Testcontainers, one shared container wiped per test) at instants pinned by a `TestClock`: the cycle tick (preselect after T, activate exactly at T, idempotent, ≥ 2-member gate, selector rotation by join order, pool → never-used enabled-pack template → reuse fallback, 3 h selector reminder, bootstrap on the second join), selection (selector only, never activated early, re-selection returns the old pick to the pool, unused pack clones discarded, template reuse policy, tomorrow's question hidden from non-selectors, `closesAt`/`activatesAt` in UTC), voting (one vote per member, voting after midnight, per-type invalid payloads, stored row shapes), streaks (growth, break, frozen days, rebuild from history) and pool quotas; pure `ResultsBuilder` tests per type (distinct voters, pair merge, 0-vote options, «Nadie», Deathmatch teams, free text). Client: broken scaffold specs fixed (`groups.spec.ts` import, missing providers, `matchMedia` stub in `src/test-setup.ts`) plus specs for `ThemeService`, the group-card status pills, streak tiers and the release list (newest entry = `package.json` version). `.github/workflows/ci.yml` builds and tests both stacks on every PR and on `master`; CI badge in the README. Internal only — no release entry. | ✅ — rama 22 (`tests/core-domain-and-ci`) |
| Play in English and German — UI layer (rama 23, release 1.10.0): runtime i18n without a library in `client/src/app/core/i18n` — `I18nService` (the active language and its dictionary are signals, so templates calling `t()` re-render on a switch; `t(key, params)` fills `{name}` placeholders and picks `one`/`other` plurals with `Intl.PluralRules`; `tHtml` escapes params for `[innerHTML]`; `formatDate` in the UI locale), typed dictionaries `translations/{es,en,de}.ts` (`es` defines the shape, `en`/`de` are typed `Translation` so a missing or extra key fails the build, and template keys are type-checked; one lazy chunk per language, loaded before the first render by `provideAppInitializer`), `I18nTitleStrategy` (route `title`s are keys), an `Accept-Language` interceptor, `LanguagePreferenceService` and `LanguagePickerComponent` (compact globe dropdown on the landing, `/demo` and the guest header — so also the auth pages; full-width toggle group in the user-menu drawer and the profile). **Which language:** the account's `User.PreferredLanguage` once signed in (applied from the login/register response and the profile fetch) → else the language picked on this device (localStorage `vp_lang`) → else the browser's (`navigator.languages`, never IP) → else **Spanish**. Registration stores the device's language; the picker saves to the account (`PUT users/me`). Backend: migration `AddUserPreferredLanguage`, request localization + `ServerText` (§8), push notifications per recipient language (payload `lang` also picks the wording of the grouped text in `sw.js`). Release notes carry es/en/de for every entry. Dates (history, Novedades) and the history calendar (weekday/month names, weeks from Monday) follow the UI language. The Superlative "Nadie" row, which the API labels in Spanish (`id 0`, no target), is translated client-side. **Not in this rama (rama 24, content):** pack questions, pack names/descriptions, streak titles, milestone labels and chat system messages stay Spanish. Tests: `ServerTextTests`, `AppLanguagesTests`, `LocalizationIntegrationTests` (server); `i18n.service.spec`, `languages.spec`, `translations.spec` — same keys and placeholders in every dictionary, release notes complete (client). | ✅ — rama 23 (`feat/i18n-ui`) |
| Play in English and German — content layer (rama 24, release 1.11.0): every active pack question (338) and the 18 pack names/descriptions in EN and DE, adapted rather than translated (§13 row 24). Translations live in `Data/Packs/en|de/*.json` keyed by template key and are validated against the Spanish entry (`PackCatalog`; a poll keeps the same options in the same order); `PackCatalogTests` fails if an active template lacks a language. Packs, templates, template options, questions and options carry a JSONB language → text map (`NameTranslations`/`DescriptionTranslations`/`TextTranslations`); clones copy their template's, the startup sync keeps untouched clones in step (history translates too) and rewording a clone in the selection dialog drops them. Question, option, result, history and pack-picker texts are served in the request's language (`ContentTranslations`, Spanish fallback); the `new_question` push goes out in each recipient's language. Streak titles and milestone labels per language (`StreakTiers` with `LocalizedText`; client `streaks.titles.*` keys) — EN ends on «The Curtain Twitcher», DE on «Die Gardinenspionin»; streak chat messages store `ChatMessage.StreakUsername/StreakDays` and are written in each reader's language (older ones keep their Spanish text). User-created questions stay in their author's words. Migration `AddContentTranslations`. | ✅ — rama 24 (`feat/i18n-content`) |
| Live refresh by polling (rama 27, release 1.11.1; replaces the dropped SignalR rama 13): `visiblePolling(intervalMs)` helper (ticks only while `document.visibilityState === 'visible'`, immediate tick on return) drives a 15 s chat refresh in `GroupChatComponent` and a 30 s `daily/current` refresh in `GroupDetailComponent` (voting/results only); all through `exhaustMap`, silent, stale-poll guard so a vote made mid-request isn't undone; specs with fake timers for visible/hidden/destroy | ✅ — rama 27 |
| Invitation QR (rama 28, release 1.12.0): `InviteQrComponent` (`features/groups/components/invite-qr`) draws the group's invite link (`/join/{code}`, auto-join from rama 17) as an inline SVG QR (error correction M, 4-module quiet zone, always dark-on-white so it scans in dark mode). The generator (`qrcode-generator`, CommonJS, ~21 kB) is dynamically imported so it stays out of the initial bundle. Shown in the group-card invite dialog, the group-detail "invite a friend" prompt (pre-start state) and the admin's invite-code section in group settings. Tapping it opens a full-screen dialog (group name, big QR, code) that holds a Screen Wake Lock while open; web pages can't raise the screen brightness, so that part is out of scope until a native shell (rama 32). No in-app scanner: the phone camera opens the link. Invite dialog polish (§16): group name as an accent chip, new heading/subtitle, invite button on the `secondary` variant. | ✅ — rama 28 |
| Shuffled selector rotation (rama 33, release 1.13.0): `SelectorRotation` helper (random draw among members with `LastSelectorRound < Group.SelectorRound`; new round excludes the previous selector when ≥ 3 members; joiners get `SelectorRound - 1`; bootstrap = creator), `CalculateSelector` removed; `DailyEntry.SelectionKind` replaces `IsAutoSelected` (migration `AddShuffledSelectorRotation`, data-preserving); `POST daily/select/confirm`; the selector reminder only fires while the entry is still `Auto`; "Me vale esta" button in the selection dialog | ✅ — rama 33 |
| Coins (rama 34, release 1.14.0): `User.Coins` + append-only `CoinTransaction` ledger + `DailyEntry.FirstVoterUserId` (migration `AddCoins`, backfills the first voter of past entries); `CoinService` (row-locked grant, idempotent per `(UserId, Reason, RefId)`, 40-coin daily cap on capped reasons, paged history); vote pays 5 + first-voter 3 (conditional update) + streak tier/milestone rewards every time they are reached; selector paid at activation for any active choice (+5 when written inline); `POST daily/vote` → `coins { earned, balance, capReached }`; `GET users/me/coins/history`; `UserProfileDto.Coins`. Angular: "+N 🪙" / daily-cap toast after voting, "Monedas y tienda" entry with balance badge in the user menu, `/shop` page (wallet, how to earn, "shop opens soon", paged history) | ✅ — rama 34 |
| Streak freeze & repair (rama 35, release 1.15.0): `GroupMember` freeze/repair columns (migration `AddStreakFreeze`); `StreakFreezeRules` (max 2 held, 100 🪙 freeze, 300 🪙 repair, lazy monthly free freeze); close-of-cycle step in the activation tick (`StreakService.CloseCycleAsync`: spend a freeze, else record the loss for repair); `CoinService.SpendAsync` (conditional update + negative ledger row); `CoinReason` 6–8; `POST shop/purchase` (own freeze, gift, repair; member row locked); `POST daily/streak/dismiss-freeze` / `dismiss-gift`; `daily/current.myStreak` gains freezes, notices and `repairableStreak`; the "streak in danger" push says when a freeze will cover the miss. Angular: "your streak in this group" section in the group view (🔥 streak + 🧊 n/2 → buy dialog, repair button), freeze-saved and gift notices, repair button on the lost-streak notice, "gift a freeze" on other members' cards, ledger labels, about-page paragraph | ✅ — rama 35 |
| Guess the author (rama 36, release 1.16.0): `AuthorGuess` table, one row per member and entry (migration `AddAuthorGuesses`; the commented-out `Vote.GuessedCreatorId` was dropped because a vote can span several rows); `AuthorGuessRules` (member-written + author still a member + ≥ 3 members; candidates = everyone but you; deterministic hint hiding `floor(wrong / 2)`); `AuthorGuessService` (`POST daily/guess`, `POST daily/guess/hint`, conditional writes); `CoinReason` 9–10 (`GuessCorrect` +5 capped, `AuthorHint` −20); `daily/current.today` gains `voterIds`, `memberCount`, `guess`, and the vote response `guess`; anonymity: pool/create return `isMine` instead of `creatorId`, the selector's identity goes only to the selector, the history detail names the author only once closed; history list `authorUsername` + `myGuessCorrect`; member stats `guessesMade`/`guessesCorrect`. Angular: "¿Quién la escribió?" card under the question (pick + confirm, skip, hint, reveal, "te han pillado X de Y" for the author), **calmer group screen** (group name + 🔥 chip in the top bar opening the streak panel, orange when at risk; time left as a thin bar sharing a row with "3/6 han votado" and a who-voted popover; streak notices one at a time above the question; selector banner above the question; "Sugerir una pregunta" under it), author and ✓/✗ in the history, guess stat on the member card, ledger labels; **toast fix**: the toaster joins the browser top layer (`TopLayerDirective`) so toasts show above CDK dialogs/drawers, and the gift-freeze error also shows inline on the card | ✅ — rama 36 |
| Race to select (rama 40, release 1.18.0): migration `AddRaceToSelect` (`DailyEntry.RaceOpenedAt`, `NotificationPreferences.RaceToSelect`); `DailyCycleRunner` opens the race at T − 10 min (`RaceLead`) and sends `race_to_select`; `DailyService` race claim (conditional update inside the select/confirm transaction) + 10 🪙 `CoinReason.RaceWon = 11`; `select`/`select/confirm` return `{ coins }`. UI: "¡Elige tú la próxima!" banner with the normal picker, "¡Elige tú!" toggle in the profile. Turn priority was built and then dropped before merging (§17.5) | ✅ — rama 40 |
| Flash question (rama 44, release 1.19.0): migration `AddFlashQuestion` (`FlashEntry` table, unique `(GroupId, LaunchedByUserId, LocalDate)`, `NotificationPreferences.FlashQuestion`); `FlashService` (launch in one transaction with the 500 🪙 charge, `CoinReason.FlashQuestion = 12`; vote on time or late; open list; gated detail) + `FlashController` (`POST flash`, `POST flash/{id}/vote`, `GET flash/{id}`); `daily/current.flashes`; closed flashes merged into the history by date; `flash_question` push; vote validation shared in `VotePayload`. UI: the group screen becomes a deck of open questions ("Hoy" + "⚡ {name}" tabs, red dot on unanswered ones, auto-advance after voting, "Te quedan N por responder"), amber ⚡ card, "⚡ Enviar ahora · 500 🪙" with confirmation in the create-question form, ⚡ marker and flash detail/late vote in the history, "Preguntas flash" preference, merged-notification line in `sw.js` | ✅ — rama 44 |
| Item inventory (rama 45, release 1.20.0): migration `AddUserInventory` (`UserInventory` table); `InventoryService` (buy one with a groupless ledger row + upsert; conditional consume that joins the caller's transaction); `GET`/`POST shop/inventory`; `useOwned` on `shop/purchase` (freeze, gift, repair), `daily/guess/hint` and `flash`; `inventory` in `users/me` and in every purchase/use response. UI: "Objetos" section in `/shop` with confirmation, "Usar · tienes N" in the streak panel, the member card gift, the author hint and the flash button | ✅ — rama 45 |
| Monthly summary (rama 41, release 1.21.0): migration `AddMonthlySummary` (`MonthlySummaries` table, `NotificationPreferences.MonthlySummary`); pure `MonthlySummaryBuilder` (awards, participation, best runs, matches); `MonthlySummaryService` (tick: build last month for up to 5 groups per tick, push with the group's activation or at once if today's already opened; list + detail with the caller's match only) + `MonthlySummariesController`; `monthly_summary` push opening `/groups/{id}?summary=YYYY-MM`. UI: story viewer (progress bars, tap/arrow navigation, reduced-motion → stacked cards, other months on the last slide), 🎉 top-bar button with a dot and a "ready" card on the group page until opened on this device, "Resumen del mes" preference, merged-notification line in `sw.js` | ✅ — rama 41 |
| Shop cosmetics (rama 42, release 1.22.0): migration `AddShopCosmetics` (`UserItems` table + the five equipped slots on `Users`); `ShopCatalog` server constant (ids are ledger `RefId`s) and `CosmeticsService` (buy = conditional spend + ledger row `CoinReason.Cosmetic = 13` + `UserItem`, in one transaction); `GET shop`, `POST shop/cosmetics`; equip/unequip and shop titles through `PUT users/me`; `look` in `users/me`, the member list and the member card; `"seniority"` frame. UI: "Tu estilo" in `/shop` with a sticky try-on preview on your own avatar (themes are tried on the whole app), buy with confirmation, wear/take off; ring skins, accessories, name colour + badge (`app-user-name`) everywhere in a group via `GroupStreaksService`; four full-palette app themes; seniority frame and shop titles in the profile | ✅ — rama 42 |
| Shop and economy revision (rama 46, release 1.25.0): flash question 500 → 100 🪙 (`CoinRewards.FlashQuestionPrice`) with a "Lanzar flash" button under the daily question (the create form in flash-only mode) and a one-off dismissible banner (`OneTimeHintsService`, per device); daily streak bonus +1…+5 🪙 per on-time vote outside the daily cap (`CoinRewards.StreakBonusSteps`, `CoinReason.StreakBonus = 14`, `coins.streakBonus` in the vote response, split toast, line in the streak panel); tier and milestone coins only on a new personal record (`StreakUpdateDto.IsNewRecord`); badges and shop titles retired — migration `RetireBadgesAndShopTitles` refunds them (`CoinReason.CosmeticRefund = 15`), clears shop titles and drops `User.BadgeKey`, `ShopCatalog.Retired` keeps the ledger naming them. Shop page: the balance as a pill in the header and every explanation folded into an "i" popover (developer, 2026-10-21) | ✅ — rama 46 |
| Free duels (rama 47, release 1.26.0, §17.15 item 6): no schema change — a duel is read from data already stored: `DuelRules.Duelists` (a Deathmatch of exactly two one-member teams) + a flash, or a `DailyEntry` whose `SelectionKind` isn't `Auto`. Push: `INotificationService.SendNewQuestionAsync`/`SendFlashQuestionAsync` take a `DuelPush` (chooser + both sides); the sides other than the chooser get "⚔️ Duelo en …" — "Ana te ha retado a un duelo: …" / "Ana os ha puesto a pelear: …" (`ServerText.Push.Duel*`) — instead of the normal push, under the replaced push's preference and the group mute; push kinds `duel` (stands in for `new_question` in `sw.js`, starting a new cycle) and `flash_duel`. Record: `MemberStatsDto.DuelsWon/DuelsDrawn/DuelsLost` per group (member card and the profile's all-groups card), closed duels only. Angular: `DuelNoticeComponent` (`duel.challenge` / `duel.between`) under the team builder in the create form (when it can select or is flash-only), in the flash confirmation for members who can only suggest, in the picker's team step and above "Me vale esta" for an auto-picked 1-vs-1; "Duelos (G · E · P)" row with the `versus` glyph on the stats card | ✅ — rama 47 |
| Change my vote, nudge and accessories retired (rama 43, release 1.27.0, §17.10, §17.15 items 5 and 7): migration `VoteChangeNudgesAndRetireAccessories` (`VoteChanges`, `Nudges`, `NotificationPreferences.Nudge`, accessory refunds as `CosmeticRefund` rows, `User.AccessoryKey` dropped). `VoteChangeService` replaces the member's `Vote` rows keeping the original `DateResponded`, so first voter, streak, coins and the unread dot don't move; ledger `CoinReason.VoteChange = 16` (`RefId` = question). `NudgeService` + `NudgesController`; push kind `nudge` (`ServerText.Push.Nudge*`, kept as sent in `sw.js` merges); ledger `CoinReason.Nudge = 17` (`RefId` = the nudge row); a row lock on the target's `GroupMember` makes the 24-h rule race-free. `InventoryItem.VoteChange = 4`, `Nudge = 5`. Angular: `ChangeVoteComponent` under the results of the daily question and each flash (one-off `changeVote` hint, the vote form inline without repeating the question), nudge button and dialog in `TodayVotersComponent`'s list of who is missing, `nudge` switch in the profile, both items in the shop's "Objetos"; accessory art, shop tab and the avatar's initials shift removed | ✅ — rama 43 |
| History gating + late vote (rama 39, release 1.17.0): `Vote.IsLate` (migration `AddVoteIsLate`); `QuestionsService.GetByDateAsync` gated per caller (`Locked` + `LateVoteQuestion` on `QuestionResultDto`) and limited to activated entries; `DailyService.LateVoteAsync` + `POST history/{date}/vote` (closed = a newer entry activated; no streak, coins or push); late votes excluded from every `StreakService` vote query (rebuild, backfill, member-card participation, crown tie-break); history detail shows "Vota para ver los resultados" and votes in place with `app-vote-form`. Also: a clock icon in front of the group screen's time-left line, and the "Sugerir pregunta" button as an outline button with a + icon | ✅ — rama 39 |
| Capacity study (rama 29, docs only): free-tier limit inventory, measured throughput of the API at Render's 0.1 vCPU / 512 MB, per-player load model and a feature budget — §11.1; reproducible scripts in `tools/loadtest/` | ✅ — rama 29 |

### Pending (MVP)

MVP feature-complete (ramas 0–12 merged; rama 13, SignalR, dropped 2026-10-12 and replaced by polling, rama 27). Post-MVP pass (§13): ramas 14–27 done, plus the pack catalog refactor (23 was built in parallel with 22). Phase 2 (§13 "Phase 2 plan"): ramas 28 (invite QR), 29 (capacity study), 30 (gameplay design, §17) and the gameplay ramas 33–36 done; rama 37 (points/ranking) dropped 2026-10-12, folded into the rama 41 awards; rama 38 (group streak) postponed to the backlog the same day; ramas 39–40 done (turn priority dropped from 40); the flash question (rama 44, brought forward) done; the item inventory (rama 45) done; the monthly summary (rama 41) done; the shop cosmetics (rama 42) done; the shop and economy revision (rama 46) done; the free duels (rama 47, built before 43 on the developer's call, 2026-10-21) done. the change my vote + nudge rama (43) done; remaining sprint order (2026-10-21, §13): 48 → 49 → 50 → 51, then 31, then 52 → 55, then 56, then 32. Phase 2 backlog in §16.

---

## 13. Roadmap & Branch Plan

Each branch is **backend + its Angular UI**, cut from `master`, merged before the next. The **design-system** branch (rama 4) is frontend-only and may run in parallel with the backend ramas 1–3. Gamification, OAuth, avatars, per-group time-zone logic, fair rotation, and multi-language gameplay are **Phase 2**. *(Thematic packs + per-group toggle shipped in rama 15.)*

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
| 13 ❌ | `feat/realtime-signalr` | **Dropped (2026-10-12), superseded by rama 27.** SignalR hub + Angular client. Not worth the complexity for an asynchronous one-question-a-day game with a barely-used chat: JWT over the query string, 15-min token reconnects, hub group authorization, PWA sockets dying in the background, and redeploys dropping connections on the Render free tier. |

### Post-MVP bug-fix & optimization pass (confirmed 2026-06-30)

Triaged from a combined list of developer-reported bugs + the §16 backlog. Several smaller fixes are grouped per branch (developer preference: fewer, denser branches over one-bug-one-branch). Streak/frame design and other functionally-relevant decisions below are now confirmed scope, not raw ideas — superseded entries removed from §16.

| # | Branch | Scope |
|---|---|---|
| 14 ✅ | `fix/results-bar-and-live-countdown` | Result-bar label legibility: the label overlay (`result-option-bar.component`) spans the full track width independent of the colored fill's `displayWidth()`, with fixed `text-white` and no backdrop — illegible when the fill is narrower than the label. Fix: `text-shadow` outline on the label text (no DOM/layout restructure). Countdown live update: `CountdownComponent` has no output; reaching zero just shows "Siguiente en cualquier momento" with no refetch until manual reload. Fix: emit an `output()` on zero, `GroupDetailComponent` listens and calls `refresh()` automatically. **Implemented on `fix/results-bar-and-live-countdown`.** |
| 15 ✅ | `fix/question-pool-and-permissions` | **Bug fixes:** Deathmatch history showed team colours but no member names (`GetByDateAsync` never built `usersById`) — extracted `ResultsBuilder.BuildDeathmatchUsersById`; "who voted" dialog listed voters as a comma paragraph → vertical list; CustomPoll results now include 0-vote options (§7.4). **Base-pack lifecycle:** exhaustion-based template reuse replacing the 50-day cooldown (§6.6; `SelectResult.RecentlyUsedTemplate`→`TemplateAlreadyUsed`); creator attribution on edited clones (`CreatorId ??= userId`); block editing/teams-override of a question owned by another user. **Per-group packs:** `GroupDisabledPack` entity + `AddGroupDisabledPack` migration + `PacksController` (list / admin toggle / paginated templates) + admin toggle UI (§6.5). **Picker:** cursor pagination + type/pack filters; new `GET /packs/templates` replaces `GET /daily/selection-sources` (§6.4). **Content:** `BasePackSeeder` generalized to idempotent `PackSeeder`; 14 new thematic packs + 12 Base additions (~257 questions). **Implemented on `fix/question-pool-and-permissions`.** |
| 16 ✅ | `feat/scale-1-10` | Fix Scale question type to a fixed **1–10** range (remove configurable `Min`/`Max`); migration, validator, and frontend (`scale-create`, `scale-vote`, `scale-result`) updates; revise §5.4, §9, §11 (range/limits) accordingly once implemented. **Bug (developer-reported 2026-07-16): pack Scale questions always get a random target person.** `TemplateCloner` auto-resolution (§6.3) unconditionally assigns a random active member as `TargetUserId` to every pack Scale clone, even when the template text is phrased in second person for self-evaluation — e.g. "¿Cómo de bien conduces?" shipped with "Sobre: Henar", so some members rated themselves and others rated Henar, mixing the semantics of the results. **Confirmed direction:** pack Scale templates default to **no target** (each member answers for themselves / about the subject in the text — reads better); templates that genuinely target a person must say so explicitly, and the UI should render the name **inline in the question text** with highlighted styling (e.g. "¿Cómo de bien conduce **{María}**?") instead of the detached small "Sobre: X" line. Mechanics (per-template opt-in flag + text placeholder interpolation) to be designed within this rama. **Status 2026-10-11:** the random-target bug is **✅ fixed** by the pack-catalog refactor (#53): every pack Scale template is now a self-assessment or plain opinion, `TemplateCloner` never assigns a target (§5.4), so the per-template opt-in flag / inline-name interpolation is no longer needed. Pack templates already default to 1–10 (`PackCatalog`). **Still pending:** only the fixed-range half — user-created Scale questions can still pick a custom `RangeMin`/`RangeMax` (0–100) in `create-question`; locking that to 1–10 is a small change and no longer blocks rama 24. **Confirmed 2026-10-11 (developer): lock it to 1–10 for every Scale question, for simplicity** — remove the range inputs from `create-question` and reject/ignore custom ranges server-side. **Existing questions (confirmed 2026-10-11): convert them to 1–10** — questions/templates with a custom range get `RangeMin = 1`, `RangeMax = 10` in the rama's EF migration. Their existing votes must move with them or results break; *proposed (confirm at branch time)*: rescale each vote proportionally, `round(1 + (v − min) × 9 / (max − min))`, in the same migration. **✅ Done (2026-10-12):** range locked to 1–10 everywhere (§5.3); proportional vote rescale confirmed — in production it touched a single question (#186, 1–100, two votes: 99→10, 85→9). |
| 17 ✅ | `fix/avatar-dark-mode-and-join-flow` | DiceBear "Garabatos" (`croodles-neutral`) preset is near-invisible in dark mode — black linework on a transparent SVG with no theme-aware backing circle (picker grid uses `bg-muted`, `UserAvatarComponent` uses `bg-card`; neither guarantees contrast for this style). Join flow: registering via an invite link doesn't auto-join the inviting group afterward (only works today when logging into an existing account via the link) — low priority, but closes the navigation loop. PWA: add `"id"` to `public/manifest.webmanifest` to stop Chrome's persistent "tap to copy this app's URL" notification on the installed app. **Push follow-ups (found 2026-10-06 while fixing the subscription-sync bug):** (a) logout doesn't remove the device's `DevicePushSubscription` — on a shared device the previous account keeps receiving notifications until another account logs in and its startup sync takes the subscription over; unsubscribe server-side (DELETE by endpoint) on logout without touching the browser permission. (b) `NotificationService` sends with `TimeToLive = 3600` — a phone offline/dozing > 1 h silently drops the notification; reconsider TTL (e.g. up to the next daily T for `new_question`/`selector_turn`). (c) Notifications are dispatched fire-and-forget (`_ = notificationService.Send…`); an exception before the per-subscription loop (e.g. the recipients query) is lost with no log — wrap each dispatch so failures are logged. **Added 2026-10-11 (developer-reported):** (d) **Streak urgency on the group card:** the "Vota ahora" pill on the group list also tells the player what's at stake when they have a live streak in that group (**from 1 day**, e.g. "Vota ahora · 🔥 5 días en juego"); with no streak it stays the plain "Vota ahora". `GroupResponse` gains the caller's current (effective) streak in that group. (e) **Notification grouping & cleanup:** several pending notifications for the same thing must collapse into **one** (e.g. "3 personas han votado en *Grupo*") instead of piling up; **opening the app clears all of the app's notifications** (today, opening through one leaves the others); and **no system notification while the app is open and visible**. Needs a custom service-worker push handler wrapping `ngsw-worker.js`. iOS caveat: Safari may revoke a subscription whose pushes show nothing, so while the app is visible the notification is shown and closed immediately rather than skipped. **✅ Done (2026-10-11, release 1.8.0, [#59](https://github.com/alexsepulvedaramos/Preguntitas/pull/59)):** croodles avatars get a fixed white backing in `UserAvatarComponent` and the picker (`shared/utils/avatar-backdrop.ts`); the invite page offers "Crear cuenta y unirme" / "Ya tengo cuenta", the auth cross-links keep `returnUrl`, and `/join/:code?join=1` joins automatically once signed in; manifest `"id": "/groups"` (= the previous computed id, so installed apps keep their identity); logout DELETEs this device's subscription (best effort, 5 s timeout) and the next login re-syncs even for the same user; TTL per type — `new_question`/`new_message` 24 h, `user_voted` 6 h, `selector_turn`/`streak_danger` until the next T; every dispatch is wrapped and logs failures; `GroupResponse.MyStreak` (effective streak, batched in one query) drives an orange "No pierdas tu racha de N días" pill next to "Vota ahora"; push payloads carry `kind`/`groupId`/`groupName`/`actor`, and `client/public/sw.js` (registered instead of `ngsw-worker.js`, which it imports) merges a group's notifications into **one per group** (tag `group-{id}`; a new question drops the previous cycle's vote lines), skips them while the app is visible (WebKit: show + close), and the app closes all delivered notifications on start and on every return to the foreground. |
| 18 ✅ | `feat/mobile-group-header` | Experimental, mobile only: inside a group's routes (detail/history/settings) the global app header (logo/theme/user-menu) and group-detail's own back/history/settings row currently stack as two sticky bars — merge into one. The group-list page header is untouched. **Added 2026-10-11 (developer):** the cleaner direction is to **drop the global top bar inside a group** altogether (group-detail's own row stays), and to polish the top bar where it remains — the **theme toggle moves into the user-menu drawer**. **Moved up to right after rama 17 (2026-10-11).** **Decisions confirmed 2026-10-11 (developer):** (i) the global bar is hidden inside a group **below `sm` only** — desktop keeps it; (ii) each group screen's own top row (detail: back/history/settings; history and settings: «Volver») becomes the **sticky** app bar on mobile, same look as the global bar (`bg-card/90` + blur + bottom border); (iii) the drawer gets a **Claro / Oscuro / Sistema** toggle group — «Sistema» follows the device's `prefers-color-scheme` live and is the default when nothing is stored (stored `light`/`dark` values carry over); (iv) the remaining global bar is polished to **logo left · avatar right** (same layout as the landing bar); guests, who have no user menu, keep the theme button in the bar. **✅ Done (2026-10-11), release 1.9.0.** |
| 19 ✅ | `feat/streak-frames` | Per-group voting streak (not global, not login-based): `GroupMember` gains a streak counter, incremented in `VoteAsync` when the member has voted on consecutive active daily questions for that group, reset on a missed day. **The frame only appears once the streak reaches 3 consecutive days** (days 1–2 show no frame). Animated avatar-ring tiers by streak length: 3–6 days *(ember)* → 7–29 *(small orange flame)* → 30–99 *(intense orange flame)* → 100–181 *(blue flame)* → 182–364, i.e. 6 months+ *(purple flame)* → 365, the cap *(gold flame)*. Streak count surfaced next to each member's name in `GroupSettingsComponent`'s member list. Also: minor profile-page polish (section-title size/font). Longevity frames and frame purchasing are out of scope here — Phase 2 (§16). **Decisions confirmed 2026-10-06:** (1) **Frozen days:** a day with no active daily question in the group (e.g. the group dropped below 2 members, §4.3) neither increments nor resets the streak — it stays frozen; only a missed *active* question resets it. (2) **Where the ring shows:** every place inside a group where a member's avatar appears (daily question, results/voters, chat, history, settings member list) renders that member's streak ring **for that group**. Outside a group (profile page, header avatar) the user's own avatar shows their **highest current streak across all their groups** (current, not all-time best — the ring drops if that streak is lost); inside a group, their streak in that group. (3) **Ring = frame-colour option:** the streak ring is not layered on top of `FrameColor` — it's one more choice in the existing frame-colour picker. A user picks either a fixed colour (current palette) or **"Racha"** (frame follows their current streak tier; below 3 days → no frame). The existing `FrameColor` field gains that streak option. (4) **Backfill:** when the rama ships, current streaks are computed once from existing vote history (data migration / startup backfill), not started from zero. **Group stats (added 2026-10-06):** the group info/settings page shows per-member statistics — at least **total votes cast in the group** next to each member alongside their streak. **Member detail card (confirmed 2026-10-06):** to keep the member list light, `GroupSettingsComponent`'s list shows only avatar (with streak ring), name and a small streak badge (e.g. "🔥 12"). Tapping **any member avatar inside a group** opens a member card — Spartan drawer on mobile, dialog on desktop — with: large avatar + animated ring and the tier name (e.g. "Llama azul"); current streak and **best-ever streak** in that group; total votes cast in the group; participation % (votes ÷ active daily questions since the member joined); times as selector; questions created in the group. All derived from existing data except best-ever streak, which needs a new `GroupMember` column updated together with the current streak. *(Optional, only if it doesn't bloat the rama: open your own card from the profile page with your stats per group.)* **Streak celebration (confirmed 2026-10-06):** after a vote that increments the streak, show a brief Duolingo-style full-screen animation with the updated streak count and the days left to the next tier (on days 1–2, the days left to the first ring). **Days 1–2 reward (confirmed 2026-10-06, option B):** no ring before day 3 (unchanged), but days 1–2 still get the full-screen celebration (e.g. "Día 1 · ¡Racha empezada!") plus progress dots towards the first ring (●○○ → ●●○), visible to the user themself. The vote response must carry the before/after streak (and tier info) so the client can play it; tap to dismiss, and honour `prefers-reduced-motion` with a static version. **Further confirmed scope (2026-10-06):** (a) **Streak unit = daily cycle**, not calendar day — one cycle runs from T to the next T (§4.1); voting at 00:30 on a question activated at 21:00 counts for that cycle. (b) **Milestones** at days 14, 50, 200 and 300 (in addition to tier changes) get a special celebration (e.g. "¡2 semanas!") with confetti but no new ring art — keeps rewards frequent without multiplying ring designs. (c) **"Streak in danger" push:** a few hours before the cycle closes, notify members with a streak ≥ 3 who haven't voted yet (e.g. "🔥 Tu racha de 12 días en *Grupo* termina a las 21:00"); new notification type, toggleable in `NotificationPreferences` (+ migration). (d) **Lost-streak message:** next time the user opens the group after losing a streak, a gentle notice ("Has perdido tu racha de 12 días · Tu récord: 30"). (e) **Milestone chat messages:** an automatic system message in the group chat when a member reaches a tier change or milestone (e.g. "🔥 Ana lleva 30 días seguidos"). **Implementation approach:** rings are **pure CSS** (masked conic-gradient ring + small inline-SVG flame tongues, animating only `transform`/`opacity` since 10–15 avatars can be on screen; static under `prefers-reduced-motion`), built as one wrapper around `UserAvatarComponent` driven by a tier class — no animation library. The celebration screen is a CSS-animated overlay (ring growth, count-up, progress bar) plus `canvas-confetti` (~10 kB) **lazy-loaded** only when shown. Lottie is not used (≈250 kB player + custom animation work); could be revisited later, lazy-loaded for the celebration only. **✅ Done (2026-10-06). Decisions confirmed during implementation:** (i) **Danger push timing is configurable per user** (global, in profile → Notificaciones): 1, 2, 3, 4, 6 or 8 h before the cycle closes, **default 3 h**; on by default; respects group mute; only members with a live streak ≥ 3 who haven't voted, sent once per cycle. (ii) **Frame default = "Racha":** users with no chosen colour (`null`) follow their streak (migration sets them to `"streak"`; new users default to it); a fixed colour is **switched to "Racha" automatically once**, the first time the user reaches a 3-day streak in any group (`User.StreakFrameAutoApplied`) — later choices are always respected. "Ninguno" is stored as `"none"`. (iii) **The counter keeps counting past 365**; the gold flame is the top ring. (iv) **Days 1–2 progress dots** live on the user's own member card (and the celebration), not elsewhere. (v) **Leaving and rejoining** a group rebuilds the streak from vote history (cycles spent outside count as missed, so the record survives but the current streak usually doesn't). (vi) The "lost streak" notice shows for lost streaks of ≥ 2 days, once, until dismissed. (vii) Tier names: Brasa, Llama pequeña, Llama intensa, Llama azul, Llama morada, Llama dorada. (viii) The own card from the profile page (optional) **is included**, as is the profile section-title polish. (ix) Confetti is a ~2 kB in-house canvas burst, lazy-loaded with the celebration, instead of `canvas-confetti` — avoids a new dependency while the client lockfile is out of sync (rama 22). (x) Avatars inside a result bar aren't tappable (they sit inside the bar's own button, which opens the vote summary); the member card opens from the settings member list and from the avatars in the vote-summary dialog. Chat messages have no avatars, so no ring there. **Revised after the design review (2026-10-06 — supersedes the flame tiers, tier names, milestone 14 and the "pure CSS ring" approach above):** (1) **Seven frame materials instead of flame rings**, each with its own title: 3–6 *Madera* — «Cotilla en prácticas»; 7–14 *Piedra* — «Fuente anónima»; 15–29 *Bronce* — «Fuente bien informada»; 30–99 *Plata* — «Carne de tertulia»; 100–181 *Oro* — «Oráculo del salseo»; 182–364 *Zafiro* — «Colaborador de Sálvame»; 365+ *Amatista* — «La Vieja del Visillo». Titles are gender-neutral unless the character is unique. (2) **Wood and stone are humble:** no shine, no glow, no animation. From bronze up, a light sweeps the rim; ornaments add up with the tier (studs, a top gem from gold, cut facets over a slowly turning nebula for sapphire and amethyst, side gems and light rays for amethyst). Full frames (particles, ornaments, a day-count ribbon on the largest size) render on the large avatar sizes (profile, member card, celebration); list sizes get a light version (rim, shine, gem). Frames are SVG built by `StreakFrameComponent` from constants (`FRAME_LOOKS`), animating only transform/opacity, static under `prefers-reduced-motion`. (3) **Crown** = not a tier: it goes to the member with the **group's best current streak** (from day 1), on top of any material and on every avatar inside the group; ties go to **whoever reached that streak first** (earliest vote on the entry that got them there). Not shown outside the group. (4) **Milestones:** 21 («¡3 semanas!»), 50, 200 and 300. (5) **Titles are unlocked forever:** reaching a tier in any group unlocks its title permanently (`User.HighestStreakEver`, kept even after leaving the group). The profile has a «Título» picker — «Automático» (highest unlocked, the default), any unlocked title, or «Ninguno»; with no streak of 3 ever, no option is available. The chosen title shows under the name on the profile, the user menu, the member card and the group's member list. (6) The celebration is a dark full-screen scene in both themes: rays in the tier colour, the frame "forged" in place, the count popping up, embers rising, confetti on tier-ups and milestones; it names the title unlocked and the frame material. (7) **Free-text answers show their author's avatar** (with frame and crown, tap → member card) beside each answer bubble — Open Text is one of the most used question types, so it's where frames get seen; `FreeTextResponseDto` gains `UserId`, `AvatarUrl`, `FrameColor`. |
| 20 ✅ | `fix/daily-question-time-change` | **Bug (developer-reported):** changing a group's `DailyQuestionTime` in settings to a near-future time (e.g. "in 1 minute") doesn't recompute `closesAt`/`activatesAt` correctly — the countdown shows something like "1d 1m" instead of "1m". Suspected cause: today's `DailyEntry`/`ActivatedAt` (and the cached UTC offset math in `DailyClock`) is computed off the *old* `DailyQuestionTime` and not re-derived when the group's time changes mid-cycle, so the next-T calculation still anchors to the stale T instead of the new one. Needs investigation into `DailyClock` + `DailyPreselectionService`/`DailyService`'s `closesAt`/`activatesAt` computation and how (or whether) an in-flight cycle should re-anchor when `DailyQuestionTime` is edited. **Investigation outcome:** the suspected "stale cached T" was slightly off — `closesAt`/`activatesAt` already compute off the group's *live* `DailyQuestionTime`; what wasn't re-anchored is the pending `DailyEntry`'s **date**. **Decision (confirmed 2026-07-16, §4.8):** if today's question hasn't activated yet, the pending question just reschedules to the new T (already works); if today already activated, the change applies **from the next cycle** (no re-anchor, no early close — preserves the full 24h window and members' unused votes), and the settings UI shows an informational toast. `GroupResponse.DailyTimeChangeAppliesFromTomorrow` drives that message. **Implemented on `fix/daily-question-time-change`.** |
| 21 ✅ | `feat/public-onboarding` | **Part of the portfolio milestone (2026-10-06).** **Friendly for the context-free visitor.** Public landing at `/` (no auth): product pitch, "cómo funciona" in 3 steps, CTA to register (Spanish first; translated once rama 23 lands). Group-list empty state upgraded to explain the ≥2-member start gate ("invita a un amigo para que empiece el juego"). **✅ Done (2026-10-11). Decisions confirmed with the developer:** (i) the landing is **guests only** — signed-in users hitting `/` go to `/groups`; it has its own light layout, not the app shell; (ii) unknown routes redirect to `/`; (iii) the start-gate explanation covers **both** the empty group list and the in-group pre-start state (share-invite button), not only the list. The landing links to `/about` for the detail instead of repeating it. |
| 22 ✅ | `tests/core-domain-and-ci` | *(low priority — developer decision 2026-07-16)* Meaningful automated tests where the domain is hardest: `DailyPreselectionService` (selector rotation, ≥2-member gate, preselect-tomorrow/activate-today), `ResultsBuilder` (per-type aggregation, Secret Pairing pair-merge, distinct-voter counts), `TemplateCloner` (clone-on-use + auto-resolution), pool quotas. CI: new GitHub Actions workflow running build + tests for **both stacks on every PR** (client is not built in CI today); status badges in the README. **Known blockers (2026-10-06):** `client/src/app/features/groups/services/groups.spec.ts` imports `Groups`, which `groups.service` no longer exports, so `ng test` doesn't compile; `client/package-lock.json` is out of sync with `package.json` (`@nx/*`, `eslint` versions), so `npm ci` fails — both must be fixed before a client CI job can run. **✅ Done (2026-10-12)** — details in §12. Blockers: the `groups.spec.ts` import is fixed; the lockfile had already been brought back in sync by later ramas (`npm ci` is clean). To make the time-dependent lifecycle testable, services take an injected `TimeProvider` and the minute tick lives in `DailyCycleRunner` (§14 Architecture rules). |
| 23 ✅ | `feat/i18n-ui` | **Play in English and German — UI layer** (confirmed 2026-07-16, promoted from Phase 2 §16). Runtime translation of all Angular copy (Transloco or equivalent — runtime switching, not compile-time `$localize` builds); `User.PreferredLanguage` (`es`/`en`/`de`) + migration; **default from the browser language** (`navigator.language` / `Accept-Language`) at first visit/registration — explicitly **not IP geolocation** (unreliable, needs a geo service; browser language is the standard mechanism); unsupported browser languages fall back to ~~`en`~~ **`es`** (confirmed 2026-10-12); language picker in profile and on the auth pages; localized backend validation/error messages. **✅ Done (2026-10-12, release 1.10.0), built in parallel with rama 22 at the developer's request.** Decisions confirmed with the developer (2026-10-12): (i) unsupported browser languages fall back to **Spanish**; (ii) **push notifications are translated in this rama**, in each recipient's `PreferredLanguage` (including the grouped text built by `sw.js`); (iii) **all release notes are translated** and every new release entry is written in es/en/de (§14 "Releases"); (iv) the language picker also goes on the **landing, `/demo` and the user-menu drawer**, besides the profile and the auth pages. Implementation decisions: an in-house signal-based `I18nService` with typed TS dictionaries instead of Transloco (compile-time key checks, no new dependency, zoneless-friendly); server messages localized server-side from `Accept-Language` rather than error codes, so the client keeps showing the API's message; the account's language wins over the device's on sign-in. See §12 for the full list. |
| 24 ✅ | `feat/i18n-content` | **Play in English and German — content layer.** **Cultural adaptation, not literal translation (confirmed 2026-10-06, portfolio milestone):** the humour must survive — Spanish pop-culture references are replaced with local equivalents rather than translated word for word (e.g. the streak titles «Colaborador de Sálvame» and «La Vieja del Visillo», and pack questions that rely on Spanish TV, customs or idioms, get EN/DE versions with the same intent and tone); questions with no sensible equivalent are dropped or rewritten per language. Streak titles, milestone labels and chat system messages are part of this layer. Translation storage for `QuestionTemplate` + `Pack` (per-language text, e.g. JSONB translations map keyed by language code — keeps row counts free-tier-flat); translate all ~307 seeded questions + pack names/descriptions to EN and DE; template-based daily questions render in **each member's own language**; **user-created questions stay in the author's language** (no auto-translation — free tier, no paid APIs). **Decisions confirmed 2026-10-12 (developer):** (i) a question with no sensible equivalent in a language is **rewritten** in that language (same type and intent) — never dropped, never left in Spanish; (ii) since everyone in a group votes on the same question whatever their language, a translated poll keeps **the same number of options in the same order, each option meaning the same answer** (enforced by `PackCatalog`), so mixed-language votes add up; (iii) all ~338 active templates and the 18 packs are translated in this rama (retired templates are not). (iv) **streak titles (confirmed 2026-10-12):** EN Gossip in Training · Anonymous Source · Well-Informed Source · Talk-Show Regular · Oracle of the Tea · Tabloid Columnist · The Curtain Twitcher; DE Tratsch-Azubi · Anonyme Quelle · Gut informierte Quelle · Talkshow-Dauergast · Orakel des Klatschs · Klatschpresse-Profi · Die Gardinenspionin. **✅ Done (2026-10-12), release 1.11.0** — details in §12. |
| 25 ✅ | `feat/demo-experience` | *(low priority — developer decision 2026-07-16)* Demo experience so a solo visitor (recruiter, curious stranger) can see an active question, the vote flow and results with simulated data without needing a second member — implementation approach (frontend-only mock walkthrough vs seeded read-only group) to be decided at branch time; frontend-only is the free-tier-friendliest. **Confirmed 2026-07-16:** includes a **screenshot carousel** in an "acerca de"/info section and/or a **guided first-login tour** (typical onboarding walkthrough shown once after the first login) — the carousel belongs here, not on the rama 21 landing or the auth pages. **Promoted 2026-10-06 (portfolio milestone):** no longer low priority; it must include an **interactive demo during account creation** — right after registering (before/without a group), the new user plays a simulated day: votes on a sample question, sees results with fictional members, sees how selection, streaks and frames work. **Scope revised 2026-10-11 (supersedes the two notes above):** (i) **public `/demo` route, no account needed** — linked from the landing ("Pruébalo sin registrarte") and offered as an optional, skippable entry from the empty group-list state after registering; **not** a forced step of account creation (registration is already the friction point for recruiters); (ii) **one simulated day in under ~60 s** with a fictional group and 4–5 fictional members: active question → the user votes → "al día siguiente…" results reveal (who voted for whom) → how tomorrow's question gets chosen → streak goes up, frame changes, crown → final CTA (register / create a group); (iii) **frontend-only** with mock data, reusing the real question/vote/results/avatar components — where they're coupled to HTTP services, split them into input-driven presentational components; no backend, no migration; the seeded read-only group option is discarded; (iv) **dropped:** the screenshot carousel (`/about` already renders the real components) and the guided first-login tour (rama 21's empty-state walkthrough already covers it); (v) demo copy centralized so ramas 23–24 can translate it. **Next rama (developer decision 2026-10-11)** — briefly considered for postponement the same day, then kept as the next thing to build. **Branch-time decisions (confirmed 2026-10-11):** (a) `/demo` has its **own light layout** (like the landing: logo, theme, "Entrar") and **no guard** — open to guests and signed-in users alike; (b) the visitor plays **one Superlative** question ("¿Quién de nosotros…?") voting for a fictional member; (c) first step asks for an **optional nickname** (default "Tú") used in the results, avatar and celebration; (d) results are **scripted so the visitor "wins"** (most fictional members vote for them, plus one funny cross-vote between fictional members), the visitor's own vote added wherever they cast it; (e) "how tomorrow's question gets chosen" is a **light mock** — "Mañana eliges tú" with 3 pack-question cards to pick from, explaining the rotating turn (the real selection dialog is not reused); (f) streaks: a **"3 días después…" jump** plays the real `StreakCelebrationComponent` forging the wood frame, then the group with the crown on the visitor and a glimpse of the higher materials; (g) **entry points:** landing, empty group-list state, `/about` and a discreet link under the login/register forms. Avatar taps (member card) are inert inside the demo. **✅ Done (2026-10-11, release 1.7.0).** Implementation notes: (1) results show **right after voting** (live aggregate, as in the real app §4.6) — the "al día siguiente…" reveal in the earlier draft was dropped; (2) question changed during review to «¿Quién intentaría abrazar a un zombie en un apocalipsis?»; (3) the friends' streaks are scripted so the crown starts on Eva (7 days) and passes to the visitor after the jump. |
| 26 ✅ | `feat/about-and-releases` | **Portfolio milestone (confirmed 2026-10-06, see below).** A proper in-app **"Acerca de / Cómo funciona"** section: what the game is, the daily cycle, question types, streaks/frames/titles/crown explained with live examples (reusing the real components, e.g. the frames). Plus **versioned release notes**: the app gets a version number (semver, tagged on `master`), a **"Novedades" page** listing each release with what changed (written for players, not developers — like the rama 19 release-notes page), and a "what's new" badge/notice the first time a user opens the app after a new release. **✅ Done (2026-10-11). Decisions taken during implementation:** (i) the release list is a typed constant in the client (`core/releases/releases.ts`) — no backend endpoint, no table; (ii) "seen" state is **per device** (localStorage `vp_release_seen` / `vp_release_announced`), not a `User` column — losing it only re-shows the badge; (iii) the toast fires once per release and only for devices that had opened earlier notes — first-time visitors (and everyone at 1.5.0, the first release with notes) get the dot/badge only; (iv) `/about` and `/releases` are **public** (no auth guard) so rama 21's landing and shared links can point to them; (v) versions before 1.5.0 were reconstructed from the PR merge dates: 1.0.0 launch (2026-06-24), 1.1.0 PWA + push (06-29), 1.2.0 thematic packs (06-30), 1.2.1 (07-16), 1.3.0 streaks (10-06), 1.3.1 logout fix (10-07), 1.4.0 pack content pass (10-08), 1.5.0 this rama; (vi) the question-type examples are text cards, the live examples are the streak frames, the crown and the register-page results teaser. |
| 27 ✅ | `feat/live-refresh` | **Live refresh by polling (confirmed 2026-10-12; replaces rama 13).** Frontend only, no migration. Chat re-fetches every 15 s and `daily/current` every 30 s (voting/results only), both only while the page is visible, resuming with an immediate refresh; silent, non-overlapping, vote form/dialogs/typed text untouched. See §10. **Release 1.11.1.** **Implemented on `feat/live-refresh`.** |

### Phase 2 plan (confirmed 2026-10-12)

Developer decisions (2026-10-12): the invitation QR was built **in parallel with rama 27** (live refresh, which replaced the dropped SignalR rama 13); then a **scalability study first**, because it is cheap (docs + measurements) and fixes the row/query/egress budget that the new gameplay must respect; then a **design analysis of all the new gameplay before any of it is coded**; then the gameplay ramas; then **free-tier optimization**; finally **Play Store**. The one-time **25 $ Google Play developer fee is accepted** — the only exception to the free-tier rule (§2.4); no recurring cost is.

| # | Branch | Scope |
|---|---|---|
| 28 ✅ | `feat/invite-qr` | QR of the invite link wherever you can invite (group-card dialog, group-detail pre-start prompt, settings invite-code section), tap → full screen with Screen Wake Lock; invite-dialog polish (group-name chip, heading, button colour). No in-app scanner (the phone camera opens `/join/{code}`); no "download/share QR image" (developer: not needed). `ShareGroup` extraction stays in §16. **✅ Done (2026-10-12), built in parallel with rama 27** — detail in §12. |
| 29 ✅ | `docs/scalability-study` | **How many people can play on the current infrastructure?** Docs + measurements, no product change. (1) Inventory the real limits of each free tier (Render web service RAM/CPU/hours/spin-down, Supabase DB size/egress/connections/pausing, Vercel bandwidth/functions, Supabase Storage for avatars, Web Push) — re-check the current numbers at branch time, they change. (2) Measure the per-player and per-group footprint: requests per player per day (polling/SignalR, daily/current, history, chat), DB rows and bytes per group per day, egress per session, background-service cost per group per tick, SignalR connections per instance. (3) Load-test the API locally / on a non-production instance (k6 or similar — never against production). (4) Output: a **capacity section** in this spec — max active groups/players before each limit is hit, which limit breaks first, and the per-feature budget the gameplay ramas must respect. Unblocks the deferred caching item (it needed exactly this forecast). **✅ Done (2026-10-12)** — results in §11.1: Render's 0.1 vCPU is the first wall (~60 comfortable / ~250 saturated visible players), plus a feature budget for ramas 30/33+/31 and the action items for rama 31 (connection-pool cap, streak-reminder query, history cap). Load scripts in `tools/loadtest/`. |
| 30 ✅ | `docs/gameplay-design` | **Analysis before code** for every new-gameplay item (developer, 2026-10-12: "hacer un buen trabajo de análisis previo"). Covers at least: **coins** (earn sources and sinks, inflation control, balance column vs ledger table, anti-abuse), **"adivina el autor"** (re-enable `Vote.GuessedCreatorId`; only for member-authored questions; scoring; reveal timing; interaction with anonymity), **monthly summaries/statistics** (on-the-fly vs month-end snapshot, what is shown, push at month end), **points/ranking**, **streak freeze**, **group streak**, **race to select**, **history gating + late vote**, **frame purchasing / longevity frames**, **priority boost**, and the **"reparto/casting"** question type. For each: rules, data model, API, UI sketch, cost against the rama 29 budget, open questions resolved with the developer. Output: new spec sections + the split into **implementation ramas 33+** (numbered and ordered here when this rama closes). **✅ Done (2026-10-12):** design in **§17**. Confirmed with the developer: one global coin wallet with a daily earning cap; points kept separate from coins, with a soft monthly ranking; shuffled-round selector rotation; confirming the auto-selected question counts as choosing; guess the author revealed right after voting; late votes count in the tally but not for streaks; Duolingo-style freeze plus 1 free per month; group streak at ≥ 75 %; free seniority frames; a shop whose cosmetics add to the streak ring instead of replacing it (ring skins, avatar accessories, name colour/badge, shop titles, app themes) and fair game items (author hint, change my vote, gift a freeze, streak repair, nudge); +3 🪙 for the first voter; turn priority as a FIFO queue; flash question as an extra 24 h question in its own final rama; monthly wrapped including "tu match". "Reparto/casting" went back to the backlog. Numbers (rewards, prices, cap) are proposals in §17.2, tuned during rama 34. |
| 33 ✅ | `feat/shuffled-rotation` | §17.4 — shuffled-round selector draw (`Group.SelectorRound`, `GroupMember.LastSelectorRound`), `POST daily/select/confirm`, `DailyEntry.IsAutoSelected` → `SelectionKind` (+ migration), "Me vale esta" in the selection dialog. No coins yet. **Release 1.13.0.** |
| 34 ✅ | `feat/coins` | §17.2 — `User.Coins` + `CoinTransaction` ledger, daily cap, rewards for vote / first voter / active selection (+created bonus) / streak tiers & milestones, `coinsEarned` in the vote response + toast, wallet chip, `/shop` shell with ledger history. **Release 1.14.0.** |
| 35 ✅ | `feat/streak-freeze` | §17.9 — buy/hold (max 2 per group), lazy monthly free freeze, consumption in the close-of-cycle tick, "tu congelación salvó tu racha" notice, **gift a freeze**, **streak repair**. **Release 1.15.0.** |
| 36 ✅ | `feat/guess-the-author` | §17.6 — guess stored in an `AuthorGuess` table (not `Vote.GuessedCreatorId`, see §17.6), `POST daily/guess`, guess step after voting, reveal, coins, **author hint** (pay per use); **remove `creatorId` from pool/selection DTOs** (`isMine`) and hide the selector from everyone else. Also: the calmer group screen and the toast-behind-dialogs fix. **Release 1.16.0.** |
| 37 ❌ | ~~`feat/points-ranking`~~ | **Dropped (developer, 2026-10-12).** Points mostly duplicated the streak (1 point per on-time vote) and the coins (which already reward voting, guessing and choosing), and a ranking among friends would mainly sort people by how many questions they write. What was genuinely new — recognising authors whose questions get played and good author-guessers — moves to the rama 41 monthly awards (§17.12). No `ranking` endpoint, no `GroupMember.TotalPoints`, no `pointsEarned`. See §17.3. |
| 38 ⏸️ | ~~`feat/group-streak`~~ | **Postponed to the backlog (developer, 2026-10-12):** not worth it right now on top of the personal streak; kept as a possible future feature. The design in §17.8 stays valid — if it comes back, it is this same scope: 75 % rule at close, group-streak columns, header badge, today's progress line, milestone coins + chat message. |
| 39 ✅ | `feat/history-gating-late-vote` | §17.7 — gated history results, `Vote.IsLate`, `POST history/{date}/vote`, exclusion of late votes from streak/coins/monthly awards. Also: clock icon before the time-left line, "Sugerir pregunta" as a real (outline) button. **Release 1.17.0.** |
| 40 ✅ | `feat/race-to-select` | §17.5 — T−10 race (`RaceOpenedAt`, `race_to_select` push + preference, conditional-update win). **Release 1.18.0.** Turn priority ("Reservar turno") was planned here too and **dropped (developer, 2026-10-12)**: paying to get your question played is the flash question's job (§17.5). |
| 44 ✅ | `feat/flash-question` | §17.11 — `FlashEntry`, launch/vote endpoints, `flashes` list in `daily/current`, ⚡ card in a deck of open questions and history marker, `flash_question` push; bought from the "Sugerir pregunta" dialog with a paid "⚡ Enviar ahora" button. Brought forward from last (developer, 2026-10-12): it replaces turn priority as the way to pay to get your question played. **Release 1.19.0.** |
| 45 ✅ | `feat/shop-inventory` | §17.10 "Item inventory" — buy game items in the shop ahead of time and use them later in any group: flash question, streak freeze (own or gift), author hint, streak repair. `UserInventory` stock per item, `/shop` section to buy them, "Usar · tienes N" in each item's own spot (paying in place stays when you have none). **Added by the developer 2026-10-12**, right after rama 44 and before 41. **Release 1.20.0.** |
| 41 ✅ | `feat/monthly-summary` | §17.12 — `MonthlySummary` snapshot (batched), awards (incl. star author and best author-guesser, which replace the dropped points), participation, "tu match", `monthly_summary` push, story-style viewer. **Release 1.21.0.** |
| 42 ✅ | `feat/shop-cosmetics` | §17.10 — `ShopCatalog` + `UserItem`, equipped slots on `User`, ring skins, avatar accessories, name colour + badge, shop titles (via `SelectedTitleKey`), app themes, seniority frames, `/shop` catalog with live preview. **Release 1.22.0.** |
| 43 ✅ | `feat/vote-change-and-nudge` | §17.10 — "change my vote" (relaxes §4.6), toque/nudge push + `nudge` preference. **Confirmed for development (developer, 2026-10-20), see §17.15:** changing your vote must be **visibly discoverable** on the results card, not a hidden option. **Also (developer, 2026-10-21):** retire the avatar accessories with refunds (§17.15 item 7). Decisions at branch time in §17.10. **Release 1.27.0.** |
| 47 ✅ | `feat/duels` | §17.15 item 6 — free duels: every 1-vs-1 Deathmatch chosen on purpose (flash or the selector's daily pick, with or without the chooser in it) is a duel; notice before sending, a special push for the two duelists replacing the normal one (no new setting), won · drawn · lost record on the profile and member card. **Added by the developer 2026-10-21**, replacing the paid duel item. **Built 2026-10-21, before 43 (developer's call).** |
| 46 ✅ | `feat/shop-economy-revision` | §17.15 — flash question 500 → 100 🪙 and a visible entry point outside "Sugerir pregunta"; badge emojis and shop titles removed (with refunds); streak coin rewards redesigned so continuing a streak always pays more than restarting it. Also (developer, 2026-10-21): the shop shows the balance in the header and folds its explanations into an "i". **Release 1.25.0.** |
| 48 | `feat/reactions` | §17.15 item 6 — free reactions on chat messages and on player-written answers (Open Text, poll "Otro"), one per person per target (WhatsApp-style, changing replaces), plus a toggleable `reaction` push (§16 Chat). Decisions confirmed 2026-10-21 in §17.15 item 6. **Added 2026-10-21 (sprint 1).** |
| 49 | `feat/chat-after-vote` | §16 Chat — a day's chat (and its `new_message` pushes) only after you've voted that question. Open questions listed in §16. **Added 2026-10-21 (sprint 1).** |
| 50 | `feat/flash-variants` | §17.15 item 6 — anonymous flash (~150 🪙), extension +24 h on any flash or duel (~30 🪙), time capsule (~150 🪙, results hidden 1 / 6 / 12 months, push when it opens). Prices, rules and data model settled at branch time. **Added 2026-10-21 (sprint 1).** |
| 51 | `feat/winner-effect` | §17.15 item 6 — winner effect cosmetic (~300–500 🪙): your result bar plays your effect when you are the most-voted option. **Added 2026-10-21 (sprint 1).** |
| 52 | `feat/dead-groups` | §16 — auto-deactivate dead groups + admin reactivation + `group_reactivated` push. **Added 2026-10-21 (sprint 3).** |
| 53 | `feat/pool-tools` | §16 — admin can edit a badly set-up question; suggest a pack question from the "Sugerir" picker; extract `ShareGroup` from `GroupCard`. **Added 2026-10-21 (sprint 3).** |
| 54 | `feat/domain-and-hardening` | §16 — free domain, JWT `Issuer`/`Audience`, CORS and API base URL; refresh token in an HttpOnly cookie. Promoted from Phase 2 (developer, 2026-10-21). Unblocks the TWA option of rama 32. **Added 2026-10-21 (sprint 3).** |
| 55 | `feat/account-recovery` | §16 — password recovery + Google sign-in (shipped together, confirmed 2026-10-11) + email verification; one free-tier email provider. Promoted from Phase 2 (developer, 2026-10-21). **Added 2026-10-21 (sprint 3).** |
| 56 | `fix/polish-pass` | Known small bugs and improvements: the developer's list plus a sweep of the app and code (console errors, build warnings, main flows), collected in §16 and approved before the branch starts. **Added 2026-10-21 (sprint 4, before the Play Store).** |
| 31 | `perf/free-tier-optimization` | **Load speed and resource usage**, tuned to the free-tier limits measured in rama 29. Frontend: initial bundle back under the 500 kB budget (currently ~569 kB, warning), lazy-loading (never by dropping Spartan — design over bundle size), fonts/images, service-worker caching, Lighthouse pass. Backend/DB: query counts and indexes on hot paths, caching layer (absorbs the "Deferred: caching/perf hardening" item), history depth cap, polling/SignalR intervals, background-service work per tick, Render cold start, Supabase egress. Runs after the gameplay ramas so it optimizes the final shape. |
| 32 | `feat/play-store` | Publish on Google Play (25 $ one-time fee accepted). **Starts with an analysis: Capacitor vs TWA (Bubblewrap)** — Capacitor's WebView doesn't support Web Push (backend would move Android to FCM, free) but opens native plugins (e.g. screen brightness for the QR) and iOS later; a TWA keeps the PWA and its Web Push nearly untouched but needs a real domain with Digital Asset Links (ties to the §16 free-domain item). Then: build/signing, store listing (ES/EN/DE), privacy policy, Play policies, release track. |

**Phase 2 priority order (confirmed 2026-10-12):** ~~27~~ ✅ ∥ ~~28~~ ✅ → ~~29~~ ✅ → ~~30~~ ✅ → ~~33~~ ✅ → ~~34~~ ✅ → ~~35~~ ✅ → ~~36~~ ✅ → ~~37~~ ❌ *(dropped, folded into 41)* → ~~38~~ ⏸️ *(postponed to backlog)* → ~~39~~ ✅ → 40 → ~~41~~ ✅ → 42 → 43 → 44 → 31 → 32. **Added 2026-10-20:** ~~46~~ ✅ and 43 (§17.15) before 31. **Added 2026-10-21:** ~~47~~ ✅ (free duels; planned after 43, built first on the developer's call) before 31.

**Sprint order (developer, 2026-10-21 — supersedes the order above for what's left):**
1. **Chat, game and economy** (only approved items): ~~43~~ ✅ → 48 → 49 → 50 → 51.
2. **Performance** (the app feels slower lately): 31.
3. **Groups, admin, auth and security:** 52 → 53 → 54 → 55.
4. **Polish pass** of known small bugs and improvements: 56.
5. **Play Store:** 32 (after 54, the TWA option needs the domain).

Not scheduled (not approved): the §17.15 "under discussion" list, the group streak (§17.8), activity-aware rotation, the "reparto/casting" type.

**Next step — portfolio milestone (confirmed 2026-10-06):** everything needed for Vaya Preguntita to stand as a viable portfolio project: (1) a good explanations section and versioned release notes (**rama 26**); (2) an interactive demo when creating an account (**rama 25**, promoted); (3) English and German translation with the cultural adaptations needed to keep it funny (**ramas 23–24**); (4) the public landing for context-free visitors (**rama 21**, added to the milestone 2026-10-06), so a recruiter landing on the site understands the product before signing up. ~~Rama 16 (Scale 1–10) still lands before rama 24 so the content isn't translated twice.~~ No longer a constraint (2026-10-11): the pack Scale content was already rewritten as self-assessments in #53.

**Development priority order (confirmed 2026-07-16; updated 2026-10-06 — 20 and 19 done; portfolio milestone first):** ~~19~~ ✅ → ~~26~~ ✅ → ~~21~~ ✅ → ~~25~~ ✅ → ~~17~~ ✅ → ~~18~~ ✅ → ~~16~~ ✅ → ~~22~~ ✅ → ~~23~~ ✅ *(built in parallel with 22, 2026-10-12)* → ~~24~~ ✅ → ~~13~~ ❌ *(dropped 2026-10-12)* → ~~27~~ ✅ *(live refresh by polling, replaces 13)* → Phase 2. *(2026-10-11: rama 18 moved up to right after 17 by the developer.)* **Reordered 2026-10-11 (developer delegated the order; 25 kept first by explicit decision):** 25 (demo) is next; 17 moves up because the invite-link join gap leaks exactly the new players the rama 21 landing now brings in, and the push TTL/logout bugs hit current players; 16 is now only the small 1–10 lock for user-created Scale questions; 22 (tests + CI) moves **ahead of** the i18n pair — rama 23 touches every template string, so a client build in CI is the safety net, and a green CI badge plus domain tests are a portfolio signal on their own; then 23–24; 18 and 13 stay last. Earlier rationale (pre-milestone, kept for history): core-loop bug first (20); cheap UX fixes incl. the invite-link join gap (17); Scale 1–10 (16) lands before content gets translated ×3; public landing (21) is the highest-ROI product win for strangers; then the confirmed-priority i18n pair (23–24); engagement (19) and cosmetic work (18) next; tests+CI (22) and the demo experience (25) explicitly demoted to low priority by the developer; SignalR (13) stayed optional and was finally dropped in favour of polling (27).

**Deferred → scheduled (2026-10-12):** caching/perf hardening (reduce repeated queries, cache layer, regulate per-platform usage/limits, cap history depth) — the scaling forecast it was waiting for is rama 29; the work itself is rama 31.

**Phase 2 backlog:** bundled "reparto/casting" question type · "adivina el autor" · points/ranking · priority-boost + in-app currency · frame purchasing + longevity frames (streak frames confirmed and moved to rama 19, §13) · **streak freeze** (Duolingo-style item that forgives one missed cycle; ties into coins) · **group streak** (consecutive cycles where every member voted — collective goal) · in-question chat/debate · Google OAuth · per-group time-zone logic · activity-aware selector rotation · history late-vote-to-unlock · monthly statistics · email verification & password recovery · HttpOnly-cookie refresh tokens · SSR/SEO. *(2026-10-12: invitation QR done in rama 28; caching scheduled in rama 31; the gameplay items were designed in rama 30 (§17) and scheduled as ramas 33–44, except "reparto/casting", which stays here. Points/ranking (rama 37) was dropped the same day; its useful part lives in the rama 41 awards.)* *(Multi-language gameplay and automated tests/CI were promoted out of Phase 2 into ramas 22–24 on 2026-07-16.)* **Full developer wishlist in §16.**

---

## 14. Project Conventions & Git Guidelines

### Language
- **All** identifiers, comments, XML/JSDoc, and commit messages → **English**.
- UI-facing strings → the translation dictionaries in `client/src/app/core/i18n/translations/` (rama 23): write the Spanish text in `es.ts` (the source of truth for the keys) and its English and German versions in `en.ts` / `de.ts` — never hardcode copy in templates or components. Player-facing API messages go in `server/VayaPreguntita.API/Localization/ServerText.cs`, in the same three languages.

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

### Releases (rama 26)

The app follows **semver** (`MAJOR.MINOR.PATCH`): a new player-visible feature bumps MINOR, fixes-only bumps PATCH. Every PR that changes something players notice adds or extends the top entry of `client/src/app/core/releases/releases.ts` (newest first; written for players, not developers — no internal names — in Spanish, English and German, rama 23) and sets the same version in `client/package.json` (+ the two `version` fields at the top of `package-lock.json`). Set the entry's `date` to the merge day. Several PRs may share one release until it ships. Once merged, tag the merge commit on `master` with an **annotated** tag `vX.Y.Z` (message "Vaya Preguntita X.Y.Z — <release title>") and push it. Internal-only changes (docs, tests, CI, refactors) don't get a release entry or a tag. Earlier releases were tagged retroactively on the last merge of each release day: v1.0.0 → #33, v1.1.0 → #36, v1.2.0 → #39, v1.2.1 → #41, v1.3.0 → #46, v1.3.1 → #50, v1.4.0 → #53.

### Architecture rules
- **DTOs at API boundaries** — never return EF entities.
- **No DB/business logic in controllers** — services only; controllers orchestrate.
- **All schema changes via EF migrations** — never edit Supabase manually.
- **AutoMapper profiles** for mapping (`MappingProfile.cs`); no manual mapping in controllers.
- **Time comes from the injected `TimeProvider`** (rama 22) — services and the daily tick never read `DateTime.UtcNow` or the wall clock directly; use `DailyClock.UtcNow/Today/TimeOfDay(clock)` so tests can pin the instant. Entity default timestamps (`= DateTime.UtcNow`) are the only exception.
- **CI must be green** — `.github/workflows/ci.yml` builds and tests the API and the client on every PR; new domain rules come with a test.
- Keep Angular components small; push logic into services; use **Signals** and native control flow (`@if`/`@for`/`@switch`).
- Build UI with the **Spartan skill** (`agents/skills/spartan`) and Tailwind utility classes; custom CSS only in the `@theme` token block.

### Frontend Design System & Style Guide

**These rules are binding for every new screen and feature** (fixed in the visual review, `feat/visual-review`, 1.24.0, from the UX/UI audit of 2026-10). A PR that breaks one needs the developer's explicit OK.

Source of truth for tokens: `client/src/styles-lime.css` (`:root` / `:root.dark` in `@layer base`, the shop themes as `:root[data-vp-theme=…]` blocks, the Tailwind mapping in `@theme`). **Never hardcode raw Tailwind palette classes (`text-green-500`, `bg-amber-400`, `text-white`…), hex/rgb values or one-off colours in components** — use a semantic token below. If a colour or state has no token yet, add one first (light + dark, and say whether shop themes recolour it). The only exceptions are art that is *content*: streak-frame and shop-cosmetic artwork (`streak-tiers.ts`, `shop-catalog.ts`), the coin (fixed gold) and QR codes (dark-on-white so they scan).

**Colour tokens (light + dark pair; shop themes recolour the neutrals, `primary`, `accent` and `group-*`):**
| Token | Usage |
|---|---|
| `background` / `foreground` | Page base |
| `card` / `card-foreground`, `popover` / `popover-foreground` | Elevated surfaces (cards, dialogs, menus) |
| `primary` / `primary-foreground` (+ `primary-hover`) | Sage — brand, secondary CTAs ("Sugerir pregunta", "Crear grupo"), selected toggles, result bars. In dark mode a **mid** sage with light text, so it never competes with lime |
| `primary-text` | Sage as **text or icon** (`text-primary-text`). Same as `primary` in light, lighter in dark for contrast. Never use `text-primary` |
| `accent` / `accent-foreground` (+ `accent-hover`, `accent-text`) | Electric Lime — the one main action of a moment ("Te toca elegir", "Adivinar", the selected vote option), focus ring, notification dots |
| `secondary` / `secondary-foreground`, `muted` / `muted-foreground` | Neutral surfaces, chips (question type), de-emphasized text |
| `sand` / `sand-foreground` | Warm Sand — celebrations and support (system chat pill, monthly-summary banner) |
| `flash` / `flash-foreground` / `flash-border` | Honey — flash questions only. Fixed on every theme (an event, not the personal palette) |
| `warning` | Calm "act soon" states (streak in danger), as text/icon over a `/15` tint |
| `success` / `success-foreground` | Confirmation feedback ("Guardado", "copiado") |
| `destructive` / `destructive-foreground` | **Only** errors and destructive actions. Red never means "third option", "coins spent" or "wrong guess" |
| `frame-<key>` | The seven profile frame colours (`purple`, `blue`, `green`, `yellow`, `orange`, `red`, `pink`), tuned to the palette with a dark variant. `User.FrameColor` stores the **key**, never a hex (legacy hex is mapped by `FrameColors.LegacyHex`) |
| `border`, `input`, `ring` | Borders, input outlines, focus ring (ring = `accent`) |
| `group-*` | The theme's hue family; used only behind the frost glyph tone |
| `sidebar-*`, `logo-*` | Sidebar chrome and logo mark — don't reuse for other UI |

**Button hierarchy (identical in light and dark):** lime `accent` = the single primary action of the moment; sage `primary` = secondary CTAs and confirmations; `outline` = purchases (always with the coin) and alternatives ("Unirme con código", "Pista"); `ghost` / `link` = ways out ("Saltar", "Ahora no", "Ya tengo cuenta"). Destructive actions live in menus or confirmation dialogs, never as a bare red icon in a list (deletes in lists are grey and turn red on hover). One accent button per view.

**Results and categories:** one colour, not a palette per option — result bars are `bg-primary/60` for the leading option(s) and `bg-primary/25` for the rest, labels in `text-foreground`; voter faces sit inside the bar on the right, vertically centred (up to 4; from 5, three and "+n"), never spilling onto the next row. Question types are told apart by **glyph**, not colour: `<app-question-type-chip>` (neutral `secondary` chip + the type's glyph from `QUESTION_TYPE_GLYPH`).

**Icons and glyphs:** no emoji in UI chrome. Concept icons come from the glyph set (`GlyphComponent`, `glyphs.ts`): a Lucide icon on a soft tile in a fixed tone — sage (everyday), lime (quick/bright), sand (warm moments), frost (freezes, gifts), honey (flash). Add a name to `GLYPHS` instead of choosing a tone per call site. Plain Lucide `ng-icon` is for controls (arrows, ✕, ⋯, chevrons). The coin is always `<app-coin>` / `<app-coin-text>` (dictionaries keep `🪙` as the marker). Emoji stay only where they are content or OS-rendered: push-notification text (shop badge emoji were retired in rama 46, §17.15).

**Typography:**
- `font-display` (`DM Serif Display`, italic) — page, section and dialog titles, the daily question (`.vp-question-text`: `text-3xl`, `md:text-4xl`), big numbers (wallet, streak count, headline stats, the monthly-summary cover).
- `font-sans` (**Quicksand**, loaded in `index.html`) — all UI text, body copy, labels.
- Three title levels: page = the `PageHeader` title (or `font-display text-2xl italic` on a hero card); section = `font-display text-lg italic tracking-tight`; eyebrow = `text-xs uppercase tracking-widest` **only for metadata**, never as a section title.
- Sizes: Tailwind's `text-*` steps plus `text-2xs` (10 px: timestamps, avatar initials, counters). No arbitrary `text-[Npx]`.
- Time is always "number, space, unit": `4 h 59 min`, `22 h 25 min 5 s` (`countdown.*` keys). Month labels standing alone start with a capital ("Septiembre de 2026"); inside a sentence they keep the language's casing ("Tu septiembre de 2026") — never CSS `capitalize`.

**Spacing & shape:** default Tailwind spacing. Two radii only: `rounded-default` (`--radius`, 1.125 rem) for every surface, card, input, banner, bubble and tile; `rounded-full` for pills, chips, avatars and round buttons. No `rounded-md/lg/xl/2xl/3xl` in feature code (Spartan's own internals excepted). Shadows: `shadow-sm` at most on cards; no coloured glows.

**Layout patterns:**
- **Sub-page header:** always `<app-page-header>` (round ghost back arrow + display title + projected actions; `inLayout` for screens inside the main layout).
- **Toolbars:** at most one or two icons visible plus a `⋯` menu (`hlm-dropdown-menu`) with **text** for the rest.
- **Per-row actions** (members, items): a `⋯` menu with text labels, never two bare icons.
- **Explanations:** keep screens lean — long "how it works" copy goes behind an "i" (`hlm-popover` on a round ghost `lucideInfo` button in the page header), as in the shop; a key number (the shop balance) sits in the header as a pill.
- **Long pages:** split into sticky `hlm-tabs` (shop) or fold older content (Novedades: latest and new versions open, the rest collapsed). A phone page shouldn't run on for many screens.
- **People:** wherever a member is chosen or listed (vote options, results, chat, guesses), show `app-user-avatar` with their ring and cosmetics plus `app-user-name` (name colour) — cosmetics must be visible where they matter.
- **Status chips on cards** never change the card's height (icon + number; the full sentence in `sr-only` / `title`).
- **Floating prompts** (install, notifications) never cover the main action: they don't show on group screens, `join` or the demo, and respect the safe-area inset.
- **After an action** stay where the result shows (don't auto-jump to the next card); point at what's next instead ("Te queda 1 pregunta").
- **Forms:** the save button is full width at the end. Pick-one-of-few choices (question type) are glyph tiles, not a select. Text that may run long on a phone uses a 2-row `hlmTextarea`. Every password field has a show/hide toggle.
- **News banners:** full width, dismissible with ✕; opening or dismissing counts as seen, and the item stays reachable from its menu.
- **Empty states:** `hlm-empty` with a glyph, a short display title and a one-line explanation (see `group-list.component.html`).
- **Unread dots:** a small `bg-accent` dot with `ring-2 ring-card`, as on the user-menu avatar and the group cards (§16 unread activity); always with `sr-only` text.

**Interactive states — every interactive element:**
- **Hover:** the token's `-hover` variant when one exists; otherwise `hover:bg-muted` / `hover:bg-secondary`.
- **Focus:** `focus-visible:ring-2 focus-visible:ring-ring` (never plain `focus:`). Dialogs, drawers, sheets and full-screen viewers focus their **panel** on open (`autoFocus: 'dialog'` in the Hlm wrappers), so opening with a tap never lights up the ✕.
- **Disabled:** keep the colour at `opacity-60`; prefer a hint saying what's missing over a silent disabled button.
- **Selected / active:** toggles and segmented controls use `bg-primary text-primary-foreground` (never a faint grey tint); selected options and tiles use the `accent` border with an `accent/15` fill.
- **Transitions:** `transition-colors` for state changes; `transition-all` only when size or shadow animates.
- **Loading:** disable the trigger and swap its label/icon for a spinner; never two loading affordances at once.
- **Touch targets:** buttons are 40 px by default (`hlm-button` `default`); `sm` / `xs` / `icon-*` keep their look but get an invisible ~44 px touch area on touch screens. Don't shrink icon buttons below `size-8` in dense rows.

**Components:** build with **Spartan** (`Hlm*`, see `.agents/skills/spartan`) — `hlm-checkbox`, `hlm-select`, `hlm-switch`, `hlm-tabs`, `hlm-dropdown-menu`, `hlm-empty`, `hlmTextarea`… never a native `<input type="checkbox">`, a native `<select>` or a hand-rolled button. Customize with `class` on top of the Hlm primitive.

**Avatars and cosmetics:** nothing is drawn over the photo (avatar accessories were retired in rama 43, §17.15 item 7). A plain frame colour is `var(--frame-<key>)`.

**Copy glossary (es):** "correo" / "correo electrónico" (never "email"), "anillos" (not "skins"), footer "© 2026 Vaya Preguntita" everywhere. UI copy only from the dictionaries.

**Logo and app icons:** `LogoComponent` / `LogoWordmarkComponent` (one line, `whitespace-nowrap`; colours via `--logo-*`). PWA icons are full-bleed opaque squares (`android-chrome-*`, `apple-touch-icon`, purpose `any`) plus `maskable-*` with the mark inside the 80 % safe zone (purpose `maskable`). Manifest colours are hex (`#051b0e`), never `oklch`.

**Accessibility:** icon-only controls need an `aria-label` or `sr-only` text; decorative elements get `aria-hidden`. Meaning carried by an icon or colour (✓/✗, dots, the streak chip) also needs `sr-only` text — `aria-label` on a plain `<span>` isn't read. Don't render interactive-looking elements without an action. Pinch zoom stays enabled (developer's call, 2026-10-20).

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
- Don't implement Phase 2 items (gamification, OAuth, avatars) without explicit confirmation.

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

Captured from the developer's running notes (2026-06-22). Legend: 🟢 MVP · 🟡 MVP-small (polish on existing flow) · 🔵 Phase 2 · ⚪ confirmed idea, no priority or rama assigned yet · ✅ already in code.

### Auth & security
- ✅ **Login with username** — `Login` already accepts username *or* email via `Identifier`; just surface it in the UI.
- ✅ **"Email/username already registered" message** — backend already returns `409 Conflict`; shown in the register form (`feat/notifications`).
- 🔵 **Email verification** *(rama 55, 2026-10-21)* — needs an email provider; adds `User.EmailVerified`.
- 🔵 **Password recovery** ("¿Has olvidado tu contraseña?") *(rama 55, 2026-10-21)* — needs an email provider (free tier, e.g. Resend/Brevo) + single-use reset token + migration. **Confirmed 2026-10-11:** stays Phase 2 and ships **together with Google sign-in**. (Changing the password while logged in already exists in `/profile`.)
- 🔵 **HttpOnly-cookie refresh tokens** *(rama 54, 2026-10-21)* — move refresh token out of JS-accessible storage (security hardening).
- 🔵 **Change JWT `Issuer`/`Audience`** *(rama 54, 2026-10-21)* once a real domain exists (do together with CORS/domain setup).

### Groups & invitations
- ✅/🟢 **Group names are NOT unique** — labels only, no existence check (decided).
- ✅ **"You're already in this group" on join** — `JoinGroupAsync` returns `(found, alreadyMember)` tuple; UI shows "Ya eres miembro de este grupo." toast (`feat/notifications`).
- 🟢 **Change admin** — backend `PUT /groups/{id}/admin` done; UI in `feat/group-admin` (rama 9).
- ✅ **Investigate slow groups fetch** — checked during rama 1: `GetUserGroupsAsync` uses AutoMapper `ProjectTo` directly on the `IQueryable`, which compiles to a single flat SQL query (no `Members` collection loaded) — no N+1. The slowness is almost certainly the Render free-tier cold start (~50 s wake).
- ✅ **Invitation QR code** — generate from the invite code. Done in rama 28.
- ✅ **Tweak invite dialog** — group-name display + invite-button colors (polish). Done in rama 28.
- 🔵 **Extract `ShareGroup` component** out of `GroupCard` (refactor) *(rama 53, 2026-10-21)*.

### UI/UX
- 🟢 **Branding & design system** — unified Tailwind `@theme` tokens, color/spacing/type scales, dark mode + WCAG AA contrast, Spartan component theming & states, logo usage, personality. See ramas 4 (foundation) & 11 (polish).
- 🟡 **Toast notifications** — implement early (Spartan toast); base for the "already registered" / "already in group" / error messages.
- ✅ **Theme switch** — `theme.service` + `theme.component` exist; wire the toggle.
- 🔵 **Evaluate "Impeccable"** for the UI.
- ✅ **User avatar image** — `User.AvatarUrl` + Supabase Storage upload, DiceBear preset grid, circular canvas crop, `UserAvatarComponent` with `frameColor` ring. Profile settings page at `/profile`.
- ✅ **Adaptive frame-color palette (light/dark)** *(done in `feat/visual-review`, 1.24.0)* — `User.FrameColor` stores a palette key (`purple`, `blue`, `green`, `yellow`, `orange`, `red`, `pink`; `FrameColors.PaletteKeys`) instead of a hex; `--frame-<key>` tokens in `styles-lime.css` give each a light and a dark variant tuned to the palette, and `UserAvatarComponent` draws `var(--frame-<key>)`. Migration `AddLastSeenAndFrameKeys` converts the stored hex values (unknown hex → `streak`); the API still accepts the seven legacy hex values from old clients and stores their key (`FrameColors.LegacyHex`).
- ✅ **Calmer streak-danger hint on the group card** *(done in `feat/visual-review`, 1.24.0)* — a fixed-size `warning` chip with the flame and the day count; the full sentence is `sr-only` / `title`. The card never changes height.
- ✅ **Unread-activity dot on group cards** *(done in `feat/visual-review`, 1.24.0; developer's decisions 2026-10-20)* — a lime dot on the group's initial. **Counts as activity** since you last had the group open: others' chat messages, a new question you haven't voted, new votes from others on the open question you already answered, and your turn to pick. **Seen** is per user on the server: `GroupMember.LastSeenAt` (null → `JoinedAt`), set by `POST /api/groups/{id}/seen`, which the group screen calls on entering and leaving. Folded into the group list response as `GroupResponse.HasNewActivity` (no new polling, §11.1).
- 🟢 **Multi-language gameplay (i18n)** — **promoted to ramas 23–24 (§13), confirmed 2026-07-16**: play in EN and DE; default language from the browser (`navigator.language` / `Accept-Language`), not IP; per-user `PreferredLanguage` override; translated seeded templates server-side; user-created questions stay in the author's language.

### Daily, rotation & gamification
- 🟢 **Per-group time zone column** — `Group.TimeZoneId` added now (rama 0); logic stays UTC+2 until Phase 2.
- ⚪ **Group streak** *(postponed by the developer 2026-10-12, was rama 38; no priority or rama now)* — consecutive cycles where ≥ 75 % of the members voted on time, with milestone coins for everyone and a chat message. Fully designed in §17.8 (+ milestone rewards in §17.2); pick it up from there if it comes back.
- ✅ **History gating + late voting** — done in **rama 39** (§17.7, release 1.17.0). Decided 2026-10-12: late votes count in the tally, but not for streak, coins or monthly awards.
- 🔵 **Activity-aware selector rotation** — *(the fairness/predictability part is solved by shuffled rounds, rama 33 §17.4; weighting by activity is still open)* weight rotation by recent participation; a member inactive for weeks shouldn't keep being picked and defaulting to an auto-question. Replaces the MVP modulo rotation (§4.4).
- ✅ **"Race to select"** — done in **rama 40** (§17.5, release 1.18.0). Turn priority, planned alongside it, was dropped (§17.5).
- ⚪ **Auto-deactivate dead groups + admin reactivation** *(confirmed 2026-10-08; rama 52, 2026-10-21)* — if nobody answers, or only one member does, for several consecutive days, the group is **deactivated**: the daily cycle stops (no more preselection or activation of questions) until the group's **admin manually reactivates it**. On reactivation, **every member gets a push notification** that the group is active again (new notification type, e.g. `group_reactivated`, alongside the existing ones). *Why:* (1) abandoned groups keep burning compute and database space for nothing; (2) a dormant group keeps consuming its question stock — if its members ever come back to really play, ~100 questions would already have been used up (cloned, marked as used by §6.6) without anyone having seen them. Two real groups already show this pattern (played only on day one). **Open decisions, to settle when this is scheduled:** the inactivity threshold (how many days) and what exactly counts as "inactive" (zero voters vs. at most one voter — mirrors the ≥2-member start gate, §4.3); counted from the last day with ≥2 voters or the last day with any vote; what happens to the already-pending `DailyEntry` and the currently open question at the moment of deactivation; whether the admin is warned before deactivation and which UI shows the paused state to non-admin members; whether group mute also silences the reactivation push.
- ⚪ **Suggest a pack question** *(developer, 2026-10-12; rama 53, 2026-10-21)* — the "Sugerir" picker that non-selectors open lets them browse the templates of **all the group's enabled packs** (§6.4, §6.5), not only write a new question. Suggesting one clones it into the group pool (`!IsUsed`), so it gets the same priority as a member-written question in the picker list and in the automatic preselection (pool before pack fallback, §6.6). Confirmed with the developer: **no coins and no points for the suggester** (it takes one tap; §17.3 points reward authorship and exclude pack clones). **It does become a guessing game:** when it is played, "guess the author" (§17.6) asks **"¿Quién la sugirió?"**, with the normal rewards for whoever guesses right. This needs a nullable `Question.SuggestedById`, and the guessable check becomes "member-written **or** suggested". **Max 3 pending suggestions per member**, so one person can't fill the pool and decide most auto-picks. Deleting a suggestion before it is played returns the template to the pack (it already works this way: §6.6 derives "used" from the clones that exist). Open: whether the picker shows "sugerida por X" (proposal: no, it would spoil the guess).
- ⚪ **Admin can edit a badly set-up question** *(developer, 2026-10-12; rama 53, 2026-10-21)* — the group admin can fix a question that came out wrong (a typo, the wrong type, missing poll options, badly formed teams, a blacklisted member…). Today only the creator (or the selector, on the pending question) can change one, and the only admin tool is deleting it from the pool. Open when scheduled: which states can be edited (pool and pending for sure; the **open** question only while nobody has voted, because editing it after votes would make those votes meaningless); whether the creator is told; whether editing a pack clone drops its translations, as already happens when the selector rewrites one (rama 24); and whether it counts against the authorship of "guess the author" (§17.6).
- ✅ **Visual review: more personality** *(developer, 2026-10-12; first pass in `feat/brand-glyphs`, 1.23.0; the rest in `feat/visual-review`, 1.24.0)* — **First pass:** no emoji in UI chrome — the glyph set (`GlyphComponent`, `glyphs.ts`), the coin (`CoinComponent`, `🪙` as the dictionary marker), solid sage "Sugerir pregunta", chat streak messages with `ChatMessageDto.glyph`, push notifications keep their emoji (developer's call, 2026-10-20) with a monochrome `badge`, full-bleed PWA icons. **Second pass (UX/UI audit 2026-10, `docs/reviews/ux-audit-2026-10.md`):** everything the audit found, turned into the binding rules of §14 "Frontend Design System & Style Guide" — brand tokens for flash (honey), warning and frame colours; one-colour result bars; neutral question-type chips with glyphs; a darker dark-mode `primary` with `primary-text` for text; 40 px buttons with 44 px touch areas; one `PageHeader`; display section titles, one surface radius and a `text-2xs` step; dialogs focusing their panel; Spartan checkbox/select/tabs/empty; people shown with avatar and cosmetics in vote options, results and chat; the group toolbar's `⋯` menu; member actions in a menu; shop tabs and rarity colours; folded Novedades; the monthly-summary cover; accessories clear of initials; the install/notification prompts kept off the group screens; maskable icons. Shop badges stay emoji (developer's call, 2026-10-20: the badge *is* the emoji bought).
- 🔵 **Re-enable `Vote.GuessedCreatorId`** — "adivina el autor", scheduled as **rama 36** (§17.6).
- 🔵 **Monthly statistics** — monthly summary ("wrapped") with awards, participation and "tu match" (no ranking: points were dropped, §17.3), scheduled as **rama 41** (§17.12).
- 🔵 **"Reparto/casting" question type** — moved back to the backlog by the developer during rama 30 (2026-10-12); not designed.
- 🔵 **Change a vote by paying coins** (§4.6) — designed in rama 30 (§17.10), scheduled as **rama 43**.
- ✅ **Notification system** (push) — Web Push / VAPID; 4 types (`new_question`, `selector_turn`, `user_voted`, `new_message`); global preferences + per-group mute; nudge prompt; auto-open selector after voting (`feat/notifications`).

### Chat
- ⚪ **Reactions on chat messages** *(confirmed 2026-10-12; rama 48, 2026-10-21)* — react to a message with an emoji. **Decided 2026-10-21** (also covers player-written answers in the results): see §17.15 item 6 — fixed emoji set, one reaction per person (changing replaces), who reacted is shown, not on your own, toggleable `reaction` push, inside the existing 15-s chat poll.

- ⚪ **Chat only after you answer** *(developer, 2026-10-12; rama 49, 2026-10-21)* — you only see (and get `new_message` pushes for) a day's chat once you've voted on that question, both in the app and in notifications, so the chat can't spoil the answers. Open when scheduled: what non-voters see instead (a locked chat with a message count?); whether the pushes held back while you hadn't voted are dropped or summarised after you vote; how it interacts with history gating / late votes (rama 39, §17.7).

### Known small bugs & improvements (rama 56)
Collected here until the polish pass (§13 rama 56) starts: the developer's own list plus what a sweep of the app and code finds; the developer approves the final list before the branch. *(Empty so far.)*

### Infra / ops
- 🔵 **Free domain** *(rama 54, 2026-10-21)* via name.com + GitHub; then update Issuer/Audience, CORS `AllowedOrigins`, and frontend API base URL.

---

## 17. Phase 2 Gameplay Design (rama 30)

**Analysis before code** (rama 30, decisions confirmed with the developer on 2026-10-12). This section is the contract for the gameplay ramas 33–44 (§13). Every item is checked against the capacity budget of §11.1: no new polling, new data folded into existing responses, per-minute work O(groups due now), fan-out batched per tick, every list capped and indexed.

**Out of scope:** the **"reparto/casting"** question type moves back to the backlog (§16, developer decision 2026-10-12). Changing a vote by paying coins (§4.6) is in, as a shop item (§17.10).

### 17.1 One currency: coins

**Coins** 🪙 are **one global wallet per user**, earned in any group, spent in the shop, and shown in the wallet chip (user menu, shop) plus a "+5 🪙" toast after earning.

*(The original design had a second, per-group currency — **points** ⭐ with a monthly ranking. It was dropped on 2026-10-12, see §17.3. Recognition between members comes from the streak and the monthly awards (§17.12); there is no ranking or leaderboard of any kind.)*

### 17.2 Coin economy

**Earn (proposal; the developer reviews the numbers):**

| Source | Coins | Capped? |
|---|---|---|
| Vote on the daily question before it closes | 5 | yes |
| **First** member to vote the daily question on time in that group (developer, 2026-10-12) — won with a conditional write, so simultaneous votes can't both get it | 3 | yes |
| Selector made an **active choice** (picked, created, or **confirmed the auto-selected question**, §17.4) | 5 | yes |
| … and the chosen question was **created by the selector during their turn** (inline create) | +5 | yes |
| Correct "guess the author" (§17.6) | 5 | yes |
| Won the race to select (§17.5) | 10 | yes |
| Own streak reaches a tier (the ring tiers of `StreakTiers`): 3 / 7 / 15 / 30 / 100 / 182 / 365 days | 5 / 20 / 35 / 50 / 100 / 150 / 300 | no |
| Own streak milestone (the `StreakTiers` milestones): 21 / 50 / 200 / 300 days | 25 / 50 / 100 / 150 | no |
| Group streak milestone (§17.8, *postponed to backlog*): 7 / 30 / 100 / 365 — **every member** | 20 / 50 / 100 / 250 | no |

- **Streak rewards pay every time** a streak reaches the tier, even after losing and rebuilding it (developer, rama 34, 2026-10-12). The amounts grow fast enough that holding one long streak always beats rebuilding short ones: one 7-day streak pays 5 + 20 = 25 against 10 for two 3-day streaks, and one 15-day streak pays 60 against 50 for two 7-day streaks. (The design draft listed tiers 3/7/30/… and milestones 14/…; rama 34 aligned them with the tiers and milestones the app already celebrates.)
- **Daily cap: 40 capped coins per user per local day across all groups.** It closes off the "fake groups with a second account" farm: each group gives at most one question a day, and the cap bounds the total. Rewards for rare milestones are uncapped.
- Late votes (§17.7) and votes on flash questions (§17.11) earn **nothing**.
- Reference rhythm: one group, voting every day and guessing half the time ≈ 7–8 🪙/day ≈ **230 🪙/month**, plus selector days and milestones.

**Spend (prices proposed):**

| Item | Price | Limits | ≈ days of play (1 group) |
|---|---|---|---|
| Toque / nudge (§17.10) | 10 | 1 per target per day | ~1 day |
| Author hint (§17.10) | 20 | 1 per question | ~3 days |
| Change my vote (§17.10) | 50 | once per question, only while open | ~1 week |
| Streak freeze (§17.9), for yourself or **as a gift** | 100 | max **2** held per group (the receiver's cap applies) | ~2 weeks |
| ~~Turn priority: "I choose next"~~ (§17.5) | — | dropped 2026-10-12, see §17.5 | — |
| Badge emoji / name colour (§17.10) | 150 / 250 | each owned once | 3–5 weeks |
| Shop title / app theme (§17.10) | 200–400 / 250 | each owned once | 1–2 months |
| Avatar accessory (§17.10) | 200–500 by rarity | each owned once | 1–2 months |
| Streak repair (§17.9) | 300 | within the next cycle after the loss; max 1 per month per group | ~6 weeks |
| Ring skin (§17.10) | 400–1 000 by rarity | each owned once | 2–4 months |
| Flash question (§17.11) | 500 | max 1 per member per group per day (several can be open at once) | ~2 months |

**Gifts are items, never coins** (anti-farm: a second account could otherwise earn up to the cap every day and pass it on).

**Data model.** `User.Coins` (int, balance, check ≥ 0) **plus** an append-only ledger `CoinTransaction { Id, UserId, Amount (±), Reason (enum), GroupId?, RefId?, CreatedAt }`. The balance and the row are written in the same DB transaction. A unique index `(UserId, Reason, RefId)` makes every reward idempotent (the tick may retry); vote, first-voter, streak and selector rewards use the `DailyEntry` id as `RefId`. `CoinReason { Vote, FirstVoter, StreakTier, StreakMilestone, SelectorChoice, SelectorCreated, StreakFreeze, StreakFreezeGift, StreakRepair }` (int, never renumbered; later ramas append — rama 36 `GuessCorrect = 9`, `AuthorHint = 10`; rama 40 `RaceWon = 11`; rama 44 `FlashQuestion = 12`; rama 42 `Cosmetic = 13`, with `RefId` = the catalog item id). Purchases write a negative row: freezes and gifts with `RefId = null` (Postgres treats nulls as distinct, so they repeat), repairs with the missed entry's id. `CoinService.GrantAsync` locks the user's row (`SELECT … FOR UPDATE`) so two votes at once can't both pass the cap. The first-voter bonus is claimed with a conditional update on `DailyEntry.FirstVoterUserId` (`WHERE FirstVoterUserId IS NULL`); the `AddCoins` migration backfills it from the earliest vote of every activated entry. An index `(UserId, CreatedAt)` lets the cap be computed by summing today's capped rows. The balance makes reads O(1) and the ledger gives auditing, refunds and anti-abuse forensics. Spending uses a conditional `UPDATE … SET Coins = Coins - @p WHERE Coins >= @p` so it can never go negative under concurrent purchases.

**When rewards are paid.** Vote and guess rewards are paid in the vote request, and the vote response carries `coins: { earned, balance, capReached }` (rama 34) for the toast: "+8 🪙", or "no more coins today" when the cap ate the whole reward. Selector rewards are paid **at activation** (tick at T, batched per due group), because until T the selector can still change their choice: 5 🪙 for any `SelectionKind` other than `Auto`, +5 for `Created`; a failed grant is logged and never blocks the rest of the tick. Streak and group-streak rewards are paid where the streak is evaluated.

**API.** `GET /api/users/me` gains `coins`. `GET /api/users/me/coins/history?before=` returns the ledger, 20 per page, as `{ items: [{ id, amount, reason, groupId, groupName, createdAt }], nextBefore }` (built in rama 34). `GET /api/shop` returns the catalog plus what you own. `POST /api/shop/purchase { item, groupId?, targetUserId? }` returns 409 if there aren't enough coins or a limit is reached (built in rama 35 for the group items; see §8).

### 17.3 Points (ranking) — dropped

**Dropped (developer, 2026-10-12), before implementation.** The designed points (1 per on-time vote, 2 per correct author guess, 3 when your member-written question is played + 1 per on-time answer it gets) mostly duplicated the streak and the coins: in an active group everyone votes daily, so the ranking would really sort members by how many questions they write — competitive pressure the "soft competition" tone wanted to avoid. The two genuinely new ideas — **recognising authors whose questions get played** and **good author-guessers** — live on as monthly awards (§17.12), computed from the same data when the summary is built. Nothing is stored or shown for points: no `ranking` endpoint, no "Clasificación" section, no `GroupMember.TotalPoints`, no `pointsEarned`.

- **Definition kept (used by §17.6 and §17.12):** "member-written" means `Source == UserCreated` and `CreatorId != null`. Pack clones don't count, even when a member edited them. A question earns its author recognition only once it is **played**, never just for being created (so the pool can't be farmed with junk).

<details><summary>Original points design (historical)</summary>

| Event | Points (in that group) |
|---|---|
| Vote on time | 1 |
| Correct "guess the author" | 2 |
| **Your** member-written question is played | 3 |
| … + per answer it receives on time (other members only) | +1 each |

- **No new storage for points:** everything is derivable from `Vote` (+ `IsLate`), `AuthorGuess` (+ `IsCorrect`) and `DailyEntry` + `Question.CreatorId`. The **current month** is computed on demand. It is bounded: ≤ 31 entries × ≤ 30 members, one query using the existing `(GroupId, Date)` index. Past months are read from the month-end snapshot (§17.12). All-time = `GroupMember.TotalPoints`, accumulated when each month is snapshotted, plus the current month on the fly.
- **API:** `GET /api/groups/{id}/ranking?month=YYYY-MM` (default: current). The member-card stats (`…/members/{userId}/stats`) gain `totalPoints`.
- **UI:** a "Clasificación" tab/section in the group, showing this month's ranking with a short "se reinicia cada mes" note. There is no crown and no permanent podium.

</details>

### 17.4 Selector rotation: shuffled rounds (replaces §4.4 modulo)

**Confirmed (developer, 2026-10-12):** the rotation stays **fair**, so everyone chooses the same number of times, but it **isn't predictable**: nobody can work out who comes after whom.

- **Rounds:** in each round every member is selector exactly once. Inside a round, the next selector is drawn **at random among members who haven't chosen yet in this round**, at preselection time. The order is never stored ahead of time, so nobody, not even the server, knows it until the draw.
- Model: `Group.SelectorRound` (int) + `GroupMember.LastSelectorRound` (int). The draw takes members with `LastSelectorRound < Group.SelectorRound`. If none remain, `SelectorRound++` and the draw is over everyone, **excluding the previous selector** when there are ≥ 3 members (no back-to-back turns across a round boundary).
- **Join mid-round:** the newcomer gets `LastSelectorRound = SelectorRound - 1`, so they enter the current round. **Leave:** the row is gone and nothing else changes. **Rejoin** = a new member (acceptable).
- **Bootstrap:** the creator is still the first selector (§4.1), and that turn counts as their turn of round 1.
- **Extra turns don't spend your round turn:** winning the race (§17.5) is an extra. The member's own turn in the round stays intact. Losing the race **does** spend the original selector's turn (confirmed).
- `CalculateSelector`'s day-count math goes away. `DailyEntry.SelectorUserId` (already the source of truth) is written by the draw.
- **Before the draw** (built in rama 33): with no pending entry there is no selector yet, so `daily/current` returns `selection: null` and `daily/select` returns 403. It only happens in the ≤ 1-minute window between T and the tick.
- **Existing groups** (built in rama 33): the migration starts them at round 1 with everyone due, except the member already holding the pending turn, which counts as their round-1 turn.

**Confirming the auto-selected question counts as choosing (confirmed).** Today the selector can only *change* the pending question, so a selector who likes the auto-selected one can't say so. New: `POST /api/groups/{id}/daily/select/confirm` marks the pending entry as chosen without changing its question. `DailyEntry.IsAutoSelected` becomes `DailyEntry.SelectionKind { Auto, Confirmed, Picked, Created }`, migrated from the bool: `false → Picked`. `Created` = inline-created during the turn and earns the +5 bonus (editing it in place keeps it `Created`; editing a picked question leaves it `Picked`). **Caveat for rama 34:** the picker sends a pack Deathmatch with re-arranged teams as an inline `NewQuestion`, so today it is recorded as `Created` (and as a member-written question, §17.3) — tell it apart before paying the bonus. In the selection dialog, the selector sees the auto-selected question with a primary "Me vale esta" button.

### 17.5 Race to select (turn priority dropped)

**Race to select.** If at **T − 10 min** the pending entry is still `SelectionKind == Auto`, the tick (batched for groups whose T−10 is now) sets `DailyEntry.RaceOpenedAt` and pushes a new **`race_to_select`** notification to the members who have already voted today, except the selector (toggleable in `NotificationPreferences`; respects group mute; see "As built"). `daily/current.selection` gains `raceOpen: true`. Any member then sees "¡Elige tú!" and opens the normal selection dialog. **The first one to pick or confirm wins:** they become `SelectorUserId`, get 10 🪙 (and the normal active-choice reward at activation), and the original selector loses their turn (it was already spent at the draw). If the original selector chooses during those 10 minutes, they keep the turn and there is no race reward. Concurrency: the race is won with a conditional update (`WHERE SelectionKind = Auto`), so the first write wins and the rest get 409 "Alguien se te ha adelantado".

**Turn priority ("I choose next", 150 🪙) — dropped (developer, 2026-10-12).** It was built in rama 40 and removed before merging. The design was a FIFO queue of bought turns per group: the oldest buyer became the selector instead of the draw, refunded on leaving. It is dropped because what it really sells ("get my question played") is better served by the **flash question** (§17.11). The flash question is immediate, sits next to the daily question, and never touches the rotation. Turn priority delayed everyone else's turn: very visible in a 3–4 person group, and a way for the most active player to buy the selector seat. One paid way to get your question out is easier to explain than two that do almost the same thing. No `TurnPriority` table, no `CoinReason.TurnPriority*`, no "Reservar turno" button.

**As built (rama 40, release 1.18.0; decisions confirmed with the developer 2026-10-12):**

- **When the race opens:** the tick runs the activation check on the clock shifted 10 minutes ahead (`DailyCycleRunner.RaceLead`), so it works for any T, including one just after midnight. One push per entry; `RaceOpenedAt` makes it idempotent.
- **What racers see:** while the race is open, `daily/current.selection.pendingQuestion` goes to every member (they may keep it with "Me vale esta"), but the drawn selector's identity stays hidden (§17.6). The group screen shows a "⏱️ ¡Elige tú la próxima!" banner with the normal picker; the selector keeps their usual "te toca elegir" banner.
- **Winning:** `select` and `select/confirm` claim the entry with `UPDATE … SET SelectorUserId = @me WHERE SelectionKind = Auto AND ActivatedAt IS NULL` inside the same transaction as the choice. The original selector claims too, so a racer can never overwrite their choice. A choice that fails validation rolls the claim back and the race stays open. Losers (including the original selector) get 409 `RaceLost`, shown inline in the picker; the 30-s refresh then removes the banner. The 10 🪙 (`CoinReason.RaceWon`, `RefId` = entry, **capped**) are paid in the request and returned as `{ coins }` for the toast.
- **Push `race_to_select`:** "⏱️ ¡Elige tú en {grupo}!" / "Nadie ha elegido la próxima pregunta y sale en 10 minutos. Quien la elija primero gana 10 🪙." It goes **only to members who have already voted today's open question** (developer, 2026-10-12: no extra pushes for people not playing that day), never to the drawn selector, respects group mute and the new **"¡Elige tú!"** preference (profile, default on), and expires at T.

### 17.6 "Guess the author"

- **Only for member-written questions** (§17.3 definition), and only when the author is still a member. The author can't guess their own question. *(Planned extension, backlog §16 "Suggest a pack question": a pack question a member suggested is also guessable, as "¿Quién la sugirió?". Keep the guessable check easy to extend.)*
- **Optional, right after voting** (confirmed: revealed immediately, not at close): the vote screen adds "¿Quién la escribió?" with the member avatars and a "Saltar" button. The answer is revealed **at once**: "¡Era Ana!" plus 5 🪙 if correct. We accept that someone may spoil it in the chat; spoiling only hurts whoever hasn't voted yet.
- The guess can be sent with the vote or later the same day, but only once and only while the question is open. `Vote.GuessedCreatorId` is re-enabled (it's commented out today) together with `Vote.GuessedAt`. Endpoint: `POST /api/groups/{id}/daily/guess { creatorId }`. `daily/current.today` gains `guessable`, `myGuess`, and `author` (only once you have guessed or skipped).
- **Anonymity leak to fix in the same rama:** the pool/selection endpoints return `creatorId` today (the picker uses it to mark "mine"). Replace it with `isMine: bool` so nobody can read authorship from the API. Until results, `QuestionToVoteDto` must not carry the creator either.
- The result card says who wrote the question once you have guessed or skipped. The history shows the author normally.
- **Decisions confirmed with the developer (rama 36, 2026-10-12):**
  - **Candidates:** every current member except whoever is guessing. Members blacklisted from the question and members who joined after it was written stay in the list as decoys, so the list never reveals when someone joined.
  - **Minimum group size: 3 members** (at least 2 candidates). With 2 members the question is not guessable.
  - **"Saltar" means giving up:** it reveals the author and the guess is gone (stored as `GuessedAt` set with `GuessedCreatorId = null`). Closing the step without choosing keeps the guess pending: the results card shows a "¿Quién la escribió?" card until the question closes.
  - **Author hint** (20 🪙): bought once per question (the ledger row `(UserId, AuthorHint, RefId = entry id)` enforces it). It removes `floor(wrong / 2)` of the wrong candidates and is only offered when that is ≥ 1 (groups of 4+). The removed set is deterministic per user and question, so reloading shows the same candidates. A correct guess still pays the full 5 🪙.
  - **Author's view:** the author has no guess step; their results card shows "Te han pillado X de Y" (members who guessed right / members who guessed), updated live. No coins for fooling people.
  - **History:** shows the author, plus a ✓/✗ for your own guess.
  - **Member card:** a new stat "Aciertos adivinando X/Y" (correct guesses / guesses made in the group, skips excluded).
  - **Second anonymity leak fixed in the same rama:** `daily/current.selection` sends `selectorUserId`/`selectorUsername` only to the selector themself. The UI never showed them, but the API sent them to every member, and the selector is often the author.
  - **Not included (developer):** no chat message at close, and no push to the author when someone guesses right.
- **As built (rama 36):**
  - **Storage:** a separate `AuthorGuess { DailyEntryId, UserId, GuessedCreatorId?, IsCorrect, GuessedAt?, HintUsed }` table (PK `(DailyEntryId, UserId)`) instead of `Vote.GuessedCreatorId`/`GuessedAt`: one vote can span several `Vote` rows (multi-select polls, secret pairings), so the guess would have been duplicated. `IsCorrect` is frozen when the guess is made. A row with only `HintUsed` is a pending guess that bought the hint.
  - **API:** `POST daily/guess { creatorId } | { skip: true }` → `{ guess, coins }`; `POST daily/guess/hint` → `{ guess, balance }`. `guess` = `{ state: pending | guessed | skipped | mine, candidateIds, hintAvailable, hintUsed, hintPrice, guessedUserId, correct, author, caughtCount, guessCount }`, sent in `daily/current.today.guess` (only once you have voted) and in the vote response. The history list carries `authorUsername` and `myGuessCorrect`; the history detail carries `author` only once the question has closed.
  - **The pool** (`GET questions/pool`, `POST questions`) returns `isMine` instead of `creatorId`.
  - **Member card:** "Veces selector" counts closed questions only; counting the open one would reveal who picked today's question.

**Group screen redesign (rama 36, confirmed with the developer, 2026-10-12).** The question is what stands out:
- **Top bar:** back · group name · 🔥 streak chip · history · settings. The chip shows the streak (and 🧊 freezes held) and turns **orange when you haven't voted and the question closes within 3 h** (the streak-danger default). Tapping it opens the streak panel: current and best streak, the freezes held, buy a freeze, and repair when available (drawer on mobile, dialog on desktop).
- **Above the question,** so they're always seen: the selector banner ("Te toca elegir la de mañana" with the pick/change button) and **one** dismissible streak notice at a time (lost streak → freeze saved → gift).
- **Time left** is a thin progress bar with "Quedan 5 h 12 m" under it; on the same row, the voters: a small avatar stack and "3/6 han votado" — tap it for who voted and who's missing (never their answers). It replaces the old "Han votado X de Y" line under the results.
- **"Sugerir una pregunta"** for everyone but the selector: a quiet button after the question/results, before the chat.
- **"¿Quién la escribió?"** sits right under the question, before the results, until you guess or skip.

### 17.7 History gating + late vote

- **Gating (changes §4.6, open archive):** a past day's results show only if you voted that question, on time or late. Otherwise the history item shows the question, total votes and a "Vota para ver los resultados" button. The history list stays as it is (a teaser).
- **Late vote (confirmed):** answering a closed question **counts in its tally like any other vote, with no mark**, but earns **no streak, no coins and doesn't count for the monthly awards**. Stored as `Vote.IsLate = true` (needed to exclude it from streak/coins/awards). Endpoint: `POST /api/groups/{id}/history/{date}/vote` (same `CreateVoteDto` + validation; targets must be current members). Free.
- Applies to every past question of the group, including those from before you joined: it makes exploring the archive fun. The author also has to vote to see the results of their own question.

### 17.8 Group streak

*Postponed to the backlog (developer, 2026-10-12): rama 38 is not scheduled. The design below is kept for when it comes back.*

- A cycle **counts** if voters **on time** ≥ **75 %** of the members at close, rounded up (confirmed). A member who used a freeze didn't vote, so they don't count. **Frozen cycles** (no active question, §13 rama 19 rule) neither add nor break.
- Evaluated **when a cycle closes** (tick at T, batched per due group, O(members)). Model: `Group.CurrentGroupStreak`, `BestGroupStreak`, `LastGroupStreakEntryId`.
- Milestones **7 / 30 / 100 / 365**: coins to every member (§17.2) + a chat system message ("🔥 ¡30 días seguidos como grupo!").
- **UI:** a group-streak badge in the group header. The daily card shows today's progress ("6/8 han votado · faltan 0 para la racha de grupo") from data `daily/current` already has (vote count + member count), with no new query.

### 17.9 Streak freeze

- **Duolingo-style (confirmed):** bought for 100 🪙 per group, **max 2 held per group**. It is consumed **automatically** when you miss an active cycle: the streak is kept but doesn't add that cycle.
- **Plus 1 free freeze per month per group (confirmed).** It is granted **lazily** (when the streak is read or a freeze is evaluated: if `LastFreeFreezeMonth < this month` and you hold < 2, +1), so there is no month-start fan-out. If you already hold 2, that month's free one is lost.
- Model: `GroupMember.StreakFreezes` (0–2), `LastFreeFreezeMonth`, `FreezeUsedEntryId` (for the notice). Consumed by the close-of-cycle tick for members with an effective streak ≥ 1 who didn't vote. It sets `LastStreakEntryId` to the closed entry without incrementing `CurrentStreak`.
- **Gift (confirmed):** any member can buy a freeze for another member of the same group; it fails if the receiver already holds 2. The receiver sees "🎁 Ana te ha regalado una congelación". You can't gift yourself: the gift button is only on other members' cards, and `targetUserId` equal to the buyer is rejected (400). Tapping the gift button first shows an inline confirmation ("¿Regalarle una congelación a Ana por 100 🪙?" / Cancelar · Sí, regalar) — nothing is spent until confirmed (1.22.2).
- **Streak repair (confirmed):** if you lost a streak without a freeze, you can buy it back (300 🪙) until the close of the next cycle, as long as you haven't missed that one too. Max once per month per group. Restores `CurrentStreak` to its pre-loss value, without adding the missed cycle. The lost-streak notice (§13 rama 19 (d)) gets a "Reparar por 300 🪙" button. Model: `GroupMember.LostStreakValue`, `LostStreakEntryId`, `LastRepairMonth`.
- Next time you open the group you see the notice "🧊 Tu congelación salvó tu racha de 12 días", the same mechanism as the lost-streak notice. The "streak in danger" push keeps firing, and says when a freeze will cover the miss.

**Built in rama 35 (confirmed with the developer, 2026-10-12):**
- **Where it lives (UI):** the group view has a "your streak in this group" section under the countdown, with the current streak and **🧊 n/2**, so you never leave the group. Tapping the freezes opens a dialog to buy one (disabled with the reason when you hold 2 or are short of coins). When a lost streak can be repaired, a "Reparar · 300 🪙" button shows in that section and on the lost-streak notice. **Gifting** is on each other member's card (tap their avatar in the group): "🎁 Regalar congelación · 100 🪙". The `/shop` page only says that freezes are bought from each group (the catalog arrives in rama 42).
- **When freezes are spent:** activating a group's entry at T closes the previous cycle (`DailyCycleRunner` → `StreakService.CloseCycleAsync`). Members whose streak was alive (`LastStreakEntryId` = the entry before the one that closed) and who didn't vote it first get the lazy monthly freeze, then spend one if they hold any. Otherwise the loss is recorded (`LostStreakValue`, `LostStreakEntryId`). Groups below 2 members never activate, so their cycles stay frozen as before. If a vote on the closing entry lands just after the tick froze it, the vote counts and the freeze is given back.
- **Lazy monthly freeze:** granted when `daily/current` reads your streak, at close of cycle, and before a purchase or a gift is checked against the limit (so a member new this month starts with 1).
- **Repair:** available while the missed entry is the last closed one (until the next cycle closes), at most once per local month per group, for lost streaks of 2+ days (the same threshold as the lost-streak notice). It restores the lost value **plus today's vote** if you already voted the open question (that vote started a new streak of 1). It pays no streak-tier or milestone coins and doesn't post a chat message. Dismissing the lost-streak notice doesn't cancel the repair: the button stays in the streak section.
- **Gift notice:** in-app only (no push): the receiver sees "🎁 Ana te ha regalado una congelación" in the group until dismissed; when several people gift, the last giver is named.

### 17.10 The shop: cosmetics and items

**Design principle (developer, 2026-10-12):** nothing you buy replaces the streak ring, because the streak ring is what people want to show off. Shop cosmetics are **additive** (they sit on, around or next to the ring), and shop items never make one vote weigh more than another. ~~Shop frames~~ are dropped for that reason.

**Cosmetics (permanent, bought once, equipped from the profile):**

| Item | What it does | Where it shows |
|---|---|---|
| **Ring skin** | Restyles the streak ring (ice flames, neon, pixel art…) and **still encodes the tier**: same thresholds as §13 rama 19, more intense as the streak grows. Below day 3 there is still no ring. | everywhere the ring shows |
| ~~**Avatar accessory**~~ | *(Retired in rama 43, §17.15 item 7: easy to fake with a photo.)* An object over the avatar (sunglasses, party hat, cardboard crown, moustache…), positioned over the image, never over the ring. | everywhere the avatar shows |
| **Name colour** + **badge emoji** | Colour of your name, and an emoji next to it. Colours come from a curated palette with light/dark variants (WCAG AA, like the §16 adaptive frame-colour item). | chat, member list, results/voters, member card |
| **Shop title** | Joke titles ("El Oráculo", "Cotilla Profesional"…). Chosen in the profile **in place of** the streak title, through the existing `User.SelectedTitleKey`, which now also accepts owned shop titles. | profile, member card |
| **App theme** | A personal accent colour for the whole app (light/dark variants). Only you see it. | your own UI |

**Seniority frames (free, confirmed):** unlocked by time in **that group** (`JoinedAt`): 1 month, 6 months, 1 year, 2 years. They are a frame option `"seniority"` with the same semantics as `"streak"` (inside a group: your seniority there; outside: your highest). Being a frame, it is an *alternative* to the streak ring, which is fine because it is free and optional. Rejoining a group resets it (accepted).

**Game items (confirmed; pay per use, no inventory except freezes — superseded by the item inventory below, rama 45):**
- **Author hint** (20 🪙): in "guess the author" (§17.6) it removes half of the wrong candidates. A correct guess still pays its normal reward.
- **Change my vote** (50 🪙): redo your vote once while the question is open. This relaxes §4.6 ("the vote is final"). It doesn't change the first-voter reward or the streak, and late votes can't be changed. The UI shows "Cambiar voto · 50 🪙" on the results card. **Built (rama 43; developer, 2026-10-21):** daily questions **and flash questions** alike; also sold in the inventory.
- **Gift a freeze** (100 🪙) and **streak repair** (300 🪙): see §17.9.
- **Toque / nudge** (10 🪙): push to a member who hasn't voted an open question. **As decided at branch time (developer, 2026-10-21):** any open question, the daily one or a flash; only when it can reach them — a push device, the new `nudge` preference on, and the group not muted; **at most one per member and group every 24 h, from anyone**; with an **optional short message** (≤ 80) shown in the push: "👉 *Grupo* — Ana te recuerda que no has votado: «Vótame a mí y te invito a una cerve»". Used from the list of who has voted ("3/6 han votado"), next to each member still missing: an "Avisar" button (EN "Remind", DE "Erinnern"), shown only for members a nudge can reach right now — nothing at all next to the others, to keep the list clean (developer, 2026-10-21). **Players never see "toque":** it is called **«Aviso»** (EN "Reminder", DE "Erinnerung") everywhere — the shop item, the profile switch «Avisos para votar», the ledger row «Avisaste a alguien», the error messages and the release note (developer, 2026-10-21); the code keeps the name `nudge`. Also sold in the inventory. Cost: one push per use.

**Data model.** The catalog is a **server constant** (`ShopCatalog`: key, kind, price, rarity; like the pack catalog). The art is pure CSS/SVG on the client, like the streak rings. Permanent ownership is `UserItem { UserId, ItemKey, PurchasedAt }` (PK `(UserId, ItemKey)`). Equipped slots go on `User`: `RingSkinKey`, `AccessoryKey`, `NameColorKey`, `BadgeKey`, `ThemeKey` (each validated against `UserItem`), plus the existing `SelectedTitleKey`. Pay-per-use items write only a ledger row (`Reason = HintUsed / VoteChanged / Nudge…`, `RefId` = question or target) and that row also enforces the per-use limits.

**Payload cost:** every DTO that carries a user's avatar (`UserDto`, `VoterDto`, `GroupMemberDto`, chat author) gains `ringSkinKey`, `accessoryKey`, `nameColorKey` and `badgeKey`, all short strings. That's a few dozen bytes per member in `daily/current` and chat, which is negligible against §11.1.

**Shop page** `/shop`: wallet, the cosmetics catalog with a live preview on your own avatar, freezes / gifts (choose group and member), and the ledger history. Pay-per-use items are bought where they are used (hint in the guess step, change vote on the results card, nudge on the member list / today's voter list).

**Item inventory (developer, 2026-10-12; rama 45).** Everything you can buy with coins is also in the shop, even when it has its own spot in a group. Game items bought in the shop go to a **global inventory** (not tied to a group) and are **used later in a specific group**, from that item's usual spot:

| Item | Bought in the shop | Used from |
|---|---|---|
| Flash question (§17.11) | 100 🪙 (rama 46; was 500) | "⚡ Lanzar flash" under the daily question, and "⚡ Enviar ahora" in the create-question form |
| Streak freeze (§17.9) | 100 🪙 | the group's streak panel, for yourself, or a member's card as a gift. Activating one counts towards the max 2 held per group (the receiver's cap for gifts) |
| Author hint (§17.10) | 20 🪙 | the "¿Quién la escribió?" step |
| Streak repair (§17.9) | 300 🪙 | the lost-streak notice / streak panel. Buying ahead works as insurance; the 1-per-month-per-group limit and the repair window still apply when it is used |

- **The button in each spot:** with stock it reads **"Usar · tienes N"**, highlighted, and uses one without charging. With none, it keeps today's pay-in-place button (buy and use in one step).
- **No stock limit** per item: coins are the brake. Freezes keep their max 2 *active* per group.
- Cosmetics are not part of this: they stay bought once and equipped (above). Change my vote and the nudge joined the inventory in rama 43 (developer, 2026-10-21): `InventoryItem.VoteChange = 4`, `Nudge = 5`, used from the results card and the nudge dialog.

**As built (rama 45, release 1.20.0):**

- **Model:** `UserInventoryItem { UserId, Item, Quantity }`, table `UserInventory`, PK `(UserId, Item)`, check `Quantity >= 0` (migration `AddUserInventory`). `InventoryItem { FlashQuestion = 0, StreakFreeze = 1, AuthorHint = 2, StreakRepair = 3 }` (int, never renumbered).
- **Buying:** one per tap, at the same price as paying in place, after a confirmation dialog. It writes a normal ledger row under the item's existing reason (`FlashQuestion`, `StreakFreeze`, `AuthorHint`, `StreakRepair`) with no group and `RefId = null`, so repeated purchases never clash with the idempotency index. The count is upserted in the same transaction.
- **Using:** each spot's endpoint takes `useOwned` (body field on `shop/purchase`, query `?useOwned=true` on `daily/guess/hint` and `flash`). It spends one with a conditional update (`Quantity > 0`) inside the spot's own transaction, so any later failure (freeze cap, repair window or monthly limit, flash daily limit, a hint already used) gives the item back. Using one writes no ledger row. With none left the request fails with 409 "No te queda ninguno en tu inventario" and charges nothing, never silently falling back to coins. An owned hint is guarded by the guess row (only the request that flips `HintUsed` spends it), since there is no ledger row to make it idempotent.
- **Where counts come from:** `GET users/me.inventory` (loaded with the profile, like the coins) and every purchase/use response (`inventory`), which the client patches into the current user.
- **UI:** `/shop` gets an "Objetos" section (hint 20 → freeze 100 → repair 300 → flash 500): what each one does, "Tienes N" (or how many coins are missing), and "Comprar · N 🪙" with confirmation. In each spot, with stock the button turns primary and reads "Usar · tienes N" ("Regalar una tuya · tienes N" on a member's card); the flash confirmation then asks "¿Lanzarla ahora con una de tu inventario?". The cosmetics placeholder stays until rama 42.

**As built (rama 42, release 1.22.0):**

- **Catalog (confirmed by the developer 2026-10-13).** Ring skins: Neón 400 (common) · Píxel 600 (rare) · Hielo 800 (epic) · Llamas 1 000 (legendary). Accessories: gafas de sol 200 · gorro de fiesta 200 · bigote 300 · auriculares 300 · corona de cartón 400 · aureola 500. Name colours, 250 each: coral, mandarina, oro, menta, cielo, lavanda, rosa, lima (Tailwind 700 on light surfaces, 300 on dark ones, all WCAG AA). Badges, 150 each: 🔥 👀 🍿 🫖 🐸 ☕ 🌶️ 🦄 💅 🤡 🐐 🧠. Shop titles: Cotilla Profesional 200 · Voto Indeciso 200 · Abogado del Diablo 300 · El Oráculo 400 (adapted in EN/DE like the streak titles). App themes, 250 each: Océano, Fresa, Uva, Mandarina; lima stays the free default.
- **Catalog data.** `ShopCatalog` (server) holds `{ Id, Key, Kind, Price, Rarity, Name (titles only) }`; the client mirror `core/constants/shop-catalog.ts` holds the art. A purchase writes a ledger row with `Reason = Cosmetic` and `RefId` = the item's stable id, so the ledger's unique `(UserId, Reason, RefId)` index also makes a double purchase impossible and the history can name the item (`itemKey` in the ledger DTO).
- **Ring skins** are a `FrameLook` per tier: the same seven thresholds, humble and still on the first tier, then a glow, a sweeping light, sparks/twinkles, a top gem, motes and finally rays as the streak climbs. Without a streak ring (frame not "Racha", or under 3 days) there is no skin.
- **Accessories** are SVGs drawn over the photo inside a radius of ~46 % of the avatar, so they never cover the ring. *(Retired in rama 43, §17.15 item 7.)*
- **App themes are a full palette (developer, 2026-10-13):** each theme rotates the hues of backgrounds, cards, borders, primary, accent, logo and group icons, in light and dark (`data-vp-theme` on `<html>`, `styles-lime.css`). Charts, sand and success/destructive keep their colours. The theme follows the signed-in account and is cached on the device so a reload paints it at once; signing out drops it.
- **Seniority frames:** frame option `"seniority"` in the profile; stages at 30 / 182 / 365 / 730 days in the group (1 month, 6 months, 1 year, 2 years), each with a richer finish. Inside a group it uses the member's `JoinedAt`; outside, the oldest current group (`users/me.firstJoinedAt`; the all-groups member card uses the oldest of its rows).
- **Where cosmetics travel (deviation from the payload note above):** instead of adding fields to every avatar DTO, a `look: { ringSkin, accessory, nameColor, badge } | null` rides on the **member list** (`GET groups/{id}/members`), the member card and `users/me`. Inside a group, results, voters, the chat, free-text answers, the author guess and the flash card look members up by id (`GroupStreaksService`, like the streaks), so `daily/current` and the chat don't grow at all. The monthly summary keeps its plain avatars.
- **Equipping happens in the shop** (where the try-on preview is), through `PUT users/me`; shop titles are chosen in the profile's title picker (🛍️, after the streak titles). Name colour + badge show in the chat, the member list, the voters list in results and the member card.
- **Shop header (rama 46, developer 2026-10-21):** the balance is a pill with the coin in the page header, always in view, next to an "i" whose popover holds how to earn coins, the daily cap, what items and "Tu estilo" are, and one line per style tab; the big wallet card and the section intros are gone (headings stay for screen readers).
- **Shop UI:** "Tu estilo" section in `/shop` between the items and the ledger: a sticky try-on card with your own avatar, name and title; tap any tile to try it on (a theme is tried on the whole app while you stay on the page), buy with a confirmation dialog, then "Ponérmelo" / "Puesto · Quitar". Ring-skin tiles show your own tier, or silver when you have no ring yet.

### 17.11 Flash question (instant)

Confirmed by the developer as **an extra question that lives alongside the daily one**. It never replaces it and never touches the golden rule (§4.1), which governs the daily question only.

- Bought for **500 🪙** by any member (the selector too). It is **launched immediately**, stays **open for 24 h** from launch (not until the next T), and is limited to **1 per member per group per local day** (developer, rama 44: being expensive, there is no group-wide limit, so several flashes can be open at once). The group must have started (≥ 2 members). It doesn't count for streaks, coins or the monthly awards. Its card looks different (⚡).
- The author is public: the buyer launches it, so there is no "guess the author". Results follow the daily rule: vote to see them. It appears in the history marked ⚡, under the same gating as §17.7.
- Model: `FlashEntry { Id, GroupId, QuestionId, LaunchedByUserId, ActivatedAt, ClosesAt, LocalDate }` with a unique index `(GroupId, LaunchedByUserId, LocalDate)` and an index `(GroupId, ClosesAt)` for the open ones. The flash has its own `Question` (`IsUsed = true` from birth, so it never enters the pool or the daily draw). Votes reuse `Vote`.
- **API:** `POST /api/groups/{id}/flash` (body = `CreateQuestionDto`, same validation as §9; the question, the flash and the charge commit in one transaction, so any failure refunds) and `POST /api/groups/{id}/flash/{flashId}/vote`. It is folded into `daily/current` as a `flashes` list (empty when there are none), **not** a second polled endpoint (§11.1 budget). Push: a new `flash_question` type to all members but the buyer (toggleable, respects mute). The 30-s poll also runs while a flash question is open.
- **Where it is bought (developer, 2026-10-12):** in the "Sugerir pregunta" dialog (and in the selector's own create form). After writing the question, below "save to the pool" there is a paid button **"⚡ Enviar ahora · 500 🪙"** that launches it as a flash question instead. Final wording is settled at branch time.
- Goes in **its own rama**, because it touches the group-detail layout and the vote/results flow for two coexisting questions. Originally scheduled last; **brought forward to right after rama 40 (developer, 2026-10-12)** once turn priority was dropped (§17.5).

**As built (rama 44, release 1.19.0; decisions confirmed with the developer 2026-10-12):**

- **Launch:** `POST flash` → `{ flash, balance }`; 409 "not enough coins" / "already launched one today in this group", 400 when the group hasn't started or the question isn't valid for the group. Ledger row `CoinReason.FlashQuestion = 12`, `RefId` = flash id. Before charging, the button asks for confirmation ("¿Lanzar ahora por 500 🪙?"); with too few coins it is disabled and says how many are missing.
- **The group screen is a deck** (developer, rama 44): today's question plus every open flash, one card at a time, with a pager on top ("Hoy" · "⚡ {name}" tabs, arrows) shown only when there is more than one. Unanswered cards carry a pulsing red dot. The screen opens on the first unanswered card (today's first), and after voting one it moves on to the next unanswered after a short pause; on today's question it waits while "¿Quién la escribió?" is still pending. When the card you're on is answered and another isn't, an "⚡ Te quedan N por responder" button points at it. The flash card has an amber ⚡ frame, who launched it, its own time-left line and voters row. The buyer lands on their new flash right after launching it.
- **Vote:** `POST flash/{flashId}/vote` returns the fresh flash state while it's open; after it closes the same endpoint is the late vote from the history (stored `IsLate`, returns 204). A flash vote never earns streak or coins and sends no "X ha votado" push.
- **History:** closed flashes are merged into `GET history` by local launch date, as items with `flashId` (and `authorUsername` = the buyer); a page covers the days of its daily entries, so the date cursor never splits or repeats a flash. The detail is `GET flash/{flashId}`, gated like a daily question (§17.7).
- **Push `flash_question`:** "⚡ Pregunta flash en {grupo}" / "{nombre} pregunta: {texto}", 24 h TTL, new **"Preguntas flash"** preference in the profile (default on).

### 17.12 Monthly summary ("wrapped")

- **Snapshot, not on the fly:** on the 1st of each month the tick builds `MonthlySummary { GroupId, Month, Payload (JSONB), CreatedAt }` for every group with ≥ 1 activated entry last month, **batched** (a few groups per tick) so the 00:00 burst never competes with requests.
- **Contents (all three confirmed; the fourth, "the month's ranking", was dropped with points on 2026-10-12, §17.3):**
  - **Monthly awards:** most voted in superlatives, most active selector (active choices), 🕵️ **best author-guesser** (most correct guesses), 📅 **most consistent** (participation %), 🖋️ **star author** (the member-written question played that month that got the most on-time answers; ties → most member-written questions played). Awards are titles, not positions: no ordered list of members is shown. These absorb the recognition the dropped points were meant to give.
  - **Participation:** group and per member, best streaks (plus the group streak, only if §17.8 is ever built).
  - **"Tu match"** (developer, 2026-10-12): the member you voted for most **mutually**, counting Superlative picks and Secret Pairing picks in both directions (A→B + B→A). Both must have picked the other at least once. It is personal: each member sees their own match. Computed per pair while building the snapshot (≤ 30 members → ≤ 435 pairs, trivial).
- **Push on the 1st** (`monthly_summary` type, toggleable, respects mute): "Ya está el resumen de octubre en *Grupo*", sent at the group's T together with that day's activation so no new burst is added.
- **UI:** a story-style full-screen sequence (tap to advance, `prefers-reduced-motion` → static cards), reachable from the group page for the last 12 months. **API:** `GET /api/groups/{id}/monthly-summaries` (list of months) and `…/monthly-summaries/{YYYY-MM}`. The personal part (your match) is cut out for the caller server-side.

**Decisions (developer, 2026-10-13):** award ties are **shared** (every member tied at the top wins the title; nobody wins with 0); participation is **on-time answers over the daily questions opened since the member joined**, and "most consistent" needs at least 5 such questions; **only current members** (at build time) count — for awards, participation and matches, including whose votes count; "best streaks" means the **longest run of consecutive daily questions answered on time within the month**, computed from the votes.

**As built (rama 41, release 1.21.0):**
- Everything counts **daily questions and on-time votes only**: flash questions (§17.11) and late votes (§17.7) never reach the summary. A correct author guess counts only next to an on-time vote.
- A daily question is in a member's base if it was still open (activation + 24 h) when they joined, or they voted it.
- Star author: member-written (`Source = UserCreated`) questions only; the award carries the question text. "Tu match" ties are shared too.
- The tick builds the **previous local month** for up to 5 groups per tick (`MonthlySummaryService.BatchSize`), on any day of the month, so a sleeping free-tier server catches up on wake. The push goes out with the group's next activation (`NotifyIfReadyAsync`), or right away when today's question had already opened when the summary was built. `NotifiedAt` dedupes it. TTL 3 days; it opens `/groups/{id}?summary=YYYY-MM`.
- Group page: a 🎉 button in the top bar (only when the group has summaries; red dot while the newest is unopened) and a "Ya está el resumen del mes" card above the notices until the newest month is opened on this device (`localStorage`). The viewer's last slide lists the other months.

### 17.13 Cost against the §11.1 budget

| Item | Hot-path impact | Tick impact |
|---|---|---|
| Coins | vote request +1 insert (ledger) | activation-time selector reward, O(due groups) |
| Shuffled rotation | none | one random draw per preselection (already O(due groups)) |
| Race to select | `daily/current` +1 bool (`raceOpen`) | T−10 pass, O(groups due at T−10), batched push |
| Guess the author | `daily/current` +3 small fields | none |
| History gating / late vote | history detail +1 lookup | none |
| Group streak / freeze | none (computed at close) | close pass O(members of due groups) |
| Monthly summary | none | monthly batched snapshot |
| Shop cosmetics | ~4 short strings per member in the member list, member card and `users/me` only (rama 42: in-group avatars look them up by id) | none |
| Nudge | one push per use | none |
| Flash | `daily/current` +1 small query (indexed `(GroupId, ClosesAt)`) | none |

No item adds polling. Every new list (ledger, summaries) is capped and indexed.

### 17.14 Implementation ramas

The split and order live in §13 ("Gameplay ramas", 33–44). Each rama depends only on earlier ones. Shuffled rotation goes first because it needs no coins. The flash question was planned last but goes right after rama 40 (developer, 2026-10-12), replacing the dropped turn priority.

### 17.15 Shop and economy revision (developer, 2026-10-20)

Decided after one week of real data (ledger 2026-10-12 → 2026-10-20, 20 users with coins). **Earning is much faster than the §17.2 estimate:** an active player in one group earns ~10–13 🪙/day (≈ 400 🪙 in the first month with streak rewards), in three groups ~35–40 🪙/day (≈ 1 100–1 200 🪙/month, near the daily cap). Votes made 48 % of all coins and streak tiers 29 %. **Spending is almost nil:** 120 of 1 727 🪙 (7 %), one freeze and one hint, and **no cosmetic was bought** — the coins are there, the rewards don't appeal.

**Confirmed changes:**

1. **Flash question: 500 → 100 🪙** (`CoinRewards.FlashQuestionPrice`, which also prices it in the inventory). It drives the group dynamic, so it should be used often; the 1 per member per group per day limit stays. **It must be visible:** today it can only be found inside "Sugerir pregunta". Add a clear entry point on the group screen. **Built (rama 46):** a secondary amber "⚡ Lanzar flash · 100 🪙" button under the daily question, next to "Sugerir pregunta" and shown to everyone (the selector too); "Lanzar flash · tienes N" when the inventory holds one. It opens the create-question form in flash-only mode (no save-to-pool or select buttons).
2. **Badge emojis removed** (`CosmeticKind.Badge`, catalog ids 31–42). Users can already put emojis in their name, so the badge adds nothing. Owners are **refunded** the price (ledger row, positive) and the equipped slot is cleared. Ids are never reused. **Built:** migration `RetireBadgesAndShopTitles` writes one `CoinReason.CosmeticRefund = 15` row per item (`RefId` = the item id, so the ledger still names it via `ShopCatalog.Retired`), adds the coins, deletes the `UserItem`s, clears shop titles from `SelectedTitleKey` and drops `User.BadgeKey`; `look.badge` and `users/me.shopTitles` are gone.
3. **Shop titles removed** (`CosmeticKind.Title`, catalog ids 51–54): a bought title doesn't show off anything. Same refund and clean-up; anyone using one as `SelectedTitleKey` falls back to "Automático". Streak titles stay.
4. **Streak coins must always reward continuing over restarting.** Requirement: for any number of days played, one unbroken streak earns at least as much as any split of those days into shorter streaks (counting the day lost to the break), forever — including past 365 days.
   - *The current tiers fail it.* They pay every time a tier is reached, and the short tiers pay more per day than the long ones: restarting a 7-day streak over and over (25 🪙 every 8 days) beats a 365-day streak (985 🪙 in total), and from day 29 on a split almost always beats the unbroken streak. Past 365 nothing more is paid at all.
   - **Confirmed (developer, 2026-10-20):** replace the per-reach tier payouts with a **daily streak bonus paid with each on-time vote that never decreases as the streak grows**, e.g. +1 🪙/day on days 3–6, +2 on 7–14, +3 on 15–29, +4 on 30–99, +5 from 100. A non-decreasing daily bonus is always fair, since breaking a streak can only drop you to a lower bonus (checked by brute force for 800 days: no split beats the unbroken streak). It totals ≈ 69 🪙 over the first 30 days and 120 🪙/month afterwards. Tier and milestone celebrations stay; if they keep a one-off coin reward, it is paid **only the first time** a user reaches that tier, so it can't be farmed. **Decided at branch time (developer, 2026-10-20):** the bonus is **outside the daily cap** (otherwise multi-group players at the cap would see "+0 racha"), and the tier and milestone coins stay at their §17.2 amounts but are paid **only when the vote beats the user's best streak ever** in any group (`User.HighestStreakEver`, the same check that unlocks titles; `StreakUpdateDto.IsNewRecord`). Built: `CoinRewards.StreakBonusSteps`/`StreakBonus()` (client mirror in `streak-tiers.ts`), ledger `CoinReason.StreakBonus = 14` (`RefId` = the entry), `coins.streakBonus` in the vote response.
   - **It must be visible (developer, 2026-10-20):** the bonus is paid right after voting and shown apart from the vote reward (e.g. toast "+5 🪙 voto · +3 🪙 racha"). The streak panel shows today's bonus and the next step ("Tu racha te da +3 🪙 por voto · +4 desde el día 30"). **Built:** toast "+N 🪙 · +M 🪙 de racha 🔥" and that line under the streak count in the streak panel; before day 3 it says from which day the bonus starts.
5. **Change my vote and toque/nudge go into development** (rama 43, design in §17.10). Changing the vote must be **obvious in the UI** — a visible "Cambiar voto · 50 🪙" action on the results card, plus a one-off hint or release note the first time, so people find out it exists. **Built (rama 43, release 1.27.0):** a full-width outline button under the results, a one-off "¡Nuevo!" hint above it, and the release note.

6. **New items liked by the developer (2026-10-20)** — rules, prices and data model settled at design time; prices are rough proposals in days of play for one group (~11 🪙/day):
   - **Anonymous flash** (~150 🪙): a flash question where nobody can see who voted what (votes are public everywhere else, §4). The tallies are still shown. Meant for the questions nobody answers honestly in public.
   - ~~**Duel** (~120 🪙) as a paid item~~ — **dropped as an item (developer, 2026-10-21):** a flash Deathmatch of one member against another already is a duel, so paying again adds nothing. **Instead, every 1-vs-1 Deathmatch someone chose on purpose is a duel, for free** (rama 47):
     - *What counts:* a Deathmatch whose two teams are one member each, **launched as a flash** or **chosen by the selector as the daily question** (an active choice, §17.4 — confirming the auto-pick counts; a question auto-selected with nobody acting does not, so no duel happens without someone wanting it). The person who sets it up **doesn't have to be one of the two**: putting two others to fight is a duel too.
     - *Before sending:* when the create form (or the selector's pick) holds a 1-vs-1 Deathmatch, a notice explains it: "Esto iniciará un duelo contra Bea y le avisaremos de que la has retado" when you are one side; "Esto iniciará un duelo entre Ana y Bea y les avisaremos" when you aren't.
     - *Notification:* the two duelists get a **special push that replaces** the normal one for that question (the flash push, or the daily new-question push at activation) — "Ana te ha retado a un duelo" / "Ana os ha puesto a pelear". **No new setting:** it follows the preference of the push it replaces (flash questions or new question) and the group mute.
     - *Result:* the most-voted side wins; equal votes = a draw; a duel with no votes doesn't count. Late votes count in the tally like any vote (§17.7).
     - *Price:* none beyond the flash itself (100 🪙); a daily duel is free.
     - **Built (rama 47), decisions taken at branch time:** nothing is stored — a duel is read from the question's teams plus the flash or the entry's `SelectionKind`, so the race winner (§17.5) counts as the chooser too. The chooser never gets the duel push: on a flash they get no push (as before), on a daily duel they get the normal new-question push. The record counts a duel only **once it has closed** (daily: the next question opened; flash: its 24 h are over), so it doesn't move while people vote; late votes cast afterwards still update it. It is kept **per group**, like the other member-card stats; the profile shows it for each group. The notice is also shown in the flash confirmation to members who can only suggest, and not when a 1-vs-1 is only saved to the pool.
   - **Extension** (~30 🪙): +24 h on an open flash. **Available on every flash and every duel.**
   - **Time capsule** (~150 🪙): a flash question whose results stay hidden until a date the buyer picks (1 month, 6 months, 1 year); everyone gets a push when it opens.
   - **Duel record on the profile** (free, rama 47 ✅): **won · drawn · lost** ("3 · 1 · 2") on the profile and the member card, counting the duels above.
   - **Reactions (free, must have):** react to results (and ideally chat messages) for free. Emoji reactions if feasible; at minimum a like. Not a shop item.
     - **Confirmed (developer, 2026-10-21, rama 48):**
       - *What can be reacted to:* **chat messages** and **text a player wrote** in the results — Open Text answers (§5.6) and the "Otro" text of a Custom Poll with `AllowOther` (§5.5) — on daily questions, flashes and the history, while the results are visible to you. **Not** the question itself, and **not** structured votes (Superlative, Secret Pairing, Deathmatch, Scale, poll options): the vote already is the opinion, and reacting inside the voters dialog would be clumsy.
       - *Not to your own* message or answer.
       - *One reaction per person per message or answer, like WhatsApp*: a fixed emoji set; picking another emoji **replaces** yours (🔥 → 🙃 leaves only 🙃), tapping yours again removes it. Who reacted is shown.
       - *Push `reaction`*, toggleable in `NotificationPreferences` (default on), respects the group mute: it goes to the author of the message or answer, grouped into one replaceable notification per message/answer, at most one every ~10 min per recipient; removing a reaction never notifies.
       - *No new polled endpoint:* reactions ride on the existing 15-s chat poll and on the results already loaded (§10, §11.1); only a set/remove call is added.
       - *Anonymous flash (rama 50):* the author of an answer can still be notified (only they learn it's theirs), but nothing may show to anyone else whose answer it is.
       - *Settled at branch time:* the exact emoji set (~6), storage (one row per `(target, UserId)` with the emoji as a column), and whether changing your emoji updates the grouped push.
   - **Winner effect** (~300–500 🪙, cosmetic): when you are the most-voted option in a result, your result bar plays your effect (confetti, spotlights…). It only shows when you win, so it is earned as much as bought.

7. **Avatar accessories removed** (developer, 2026-10-21; `CosmeticKind.Accessory`, catalog ids 11–16). They are drawn inside the photo, so anyone can fake one by uploading a picture with the sticker already on it: a bought accessory proves nothing. Same clean-up as badges and shop titles (items 2–3): owners are **refunded** the price (`CoinReason.CosmeticRefund` row, `RefId` = the item id, named via `ShopCatalog.Retired`), the `UserItem`s are deleted, `User.AccessoryKey` is dropped and `look.accessory` disappears from the API; ids are never reused. The client drops the accessory art, its shop tab and the "accessories push initials aside" rule (§14). **Built (rama 43, release 1.27.0).**

**Under discussion (not decided):** accessories that stick **out of** the avatar (horns, antennae…), drawn outside the photo so they can't be faked — the developer finds the idea odd for now, so it stays parked. Ring finishes that layer **on top of** the tier material instead of replacing it, so a gold ring still looks gold, possibly gated by tier — **depends on how they look**, so they need visual prototypes first. Also still open: the final (top two of a Superlative in a duel), scheduled flash, declining a duel for coins, rematch, custom flash card, paid sticker packs, predictions (bet on the winner before the results), a group pot, a pinned chat message, choosing tomorrow's pack, vote-entrance and chat-bubble styles.

