# Vaya Preguntita — Unified Technical & Agent Guide

> **Purpose:** Single source of truth for developers and AI coding agents (Claude, Copilot, etc.).
> **Last updated:** 24 June 2026 · **Status:** MVP build in progress.
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
- Group admins can edit the group's name, description, and daily time, manage members, and (Phase 2) toggle which thematic packs feed the rotation.

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

- **`Pack`** — a named collection of templates. MVP seeds one pack, **"Base"**, always active for every group. (Phase 2: more thematic packs + per-group activation.)
- **`QuestionTemplate`** — a global question *template* (no votes, no group): `Text`, `Type`, type config, optional template options, `PackId`. Templates are **never voted on directly**.
- When a template is chosen for a group's day (auto or manual), the system **clones** it into a concrete `Question` row inside that group (`GroupId` set, `Source = Pack`, `CreatorId = null`). That `Question` holds the votes. This keeps votes isolated per group and `Question.GroupId` non-nullable.

> Schema change: `Question.CreatorId` becomes **nullable** (`null` ⇒ came from a pack, no human author).

**Creator display convention:** the backend keeps `CreatorId = null` for pack-sourced questions and exposes it as-is. The **frontend** maps that `null` to a label such as *"Vaya Preguntita"* / *"El equipo de Vaya Preguntita"* instead of showing it empty. (Rendered in the UI ramas 5/7/8; the field stays genuinely nullable.)

### 6.3 Auto-resolution of dynamic base questions

Superlative, Secret Pairing, and Custom Poll templates are self-contained. **Scale and Deathmatch are not** (they need a target person / teams). For base-pack instances these are resolved **automatically at clone/instantiation time** from the group's current active members:

- **Scale:** `TargetUserId` ← a random active member.
- **Deathmatch:** `Teams` ← a random split of active members into 2 teams (sizes balanced).

Resolution uses the membership snapshot at the moment of instantiation (clone time), which is why minor staleness (a member joining before T) is accepted in the MVP. Implemented in `Helpers/TemplateCloner.cs`; the seed pack is loaded at startup by `Data/BasePackSeeder.cs` (idempotent).

### 6.4 Selection sources

When the selector opens the picker they can choose from: (a) the group's **user-created pool** (`!IsUsed`), and (b) the **base pack** templates. Or they create a brand-new question inline.

### 6.5 Phase 2

Multiple thematic packs, an admin UI to enable/disable packs per group, and pack-aware preselection weighting.

---

## 7. Data Models & Key DTOs

### 7.1 Entities (current + planned)

- **User** — `Id, Username, Email, PasswordHash, RefreshToken, RefreshTokenExpiry, DateJoined`. *(Phase 2: `AvatarUrl`.)*
- **Group** — `Id, Name, Description, InvitationCode, DailyQuestionTime, TimeZoneId (nullable, default 'Europe/Madrid'; MVP logic uses UTC+2), DateCreated, CreatorId`, plus `Members`, `Questions`, `DailyEntries`. **Names are not unique** (labels only). `AdminId` was removed in rama 10 — admin identity lives on `GroupMember.IsAdmin`.
- **GroupMember** — composite key `(GroupId, UserId)`, `JoinedAt` (drives rotation), `IsAdmin` (bool; exactly one member per group is admin at all times).
- **Question** — `Id, Text, Type, Source, IsUsed, DateCreated, DateActivated, Metadata (JSONB), GroupId, CreatorId (→ nullable), Options, Votes`.
- **Option** — poll option (`Id, Text, QuestionId`).
- **Vote** — `Id, DateResponded, QuestionId, UserId`, and the polymorphic answer fields: `SelectedOptionId`, `SelectedTargetUserId`, `NumericValue`, `FreeText` (used by Open Text answers and Custom Poll "Otro" answers). Unique index `(QuestionId, UserId)`.
- **DailyEntry** — `Id, Date, IsAutoSelected, PreselectedAt, ActivatedAt (nullable), GroupId, QuestionId, SelectorUserId`. Unique index `(GroupId, Date)`. `SelectorUserId` is set at preselection time and is the authoritative source for who may call `POST /daily/select` — the backend uses the stored value instead of re-running `CalculateSelector` at selection time, so member rejoins (which change `JoinedAt` and shift the rotation) don't break the selector authorization check.
- **Pack** *(new)* — `Id, Name, Description, IsActiveByDefault`.
- **QuestionTemplate** *(new)* — `Id, Text, Type, Metadata, PackId`, optional template options. Global, voteless.

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
- **CustomPoll:** one row per option; options with 0 votes are omitted (don't clutter results with unvoted options); sorted by `VoteCount` descending.
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
| `GET` | `/api/groups/{groupId}/members` | 🆕 | `{ id, username, avatarUrl, joinedAt, isAdmin }` — **needed for person-based voting** |
| `DELETE` | `/api/groups/{groupId}/members/me` | ✅ | leave group |
| `DELETE` | `/api/groups/{groupId}/members/{userId}` | ✅ | admin: kick member |
| `POST` | `/api/groups/{groupId}/invite-code/regenerate` | ✅ | admin: rotate invitation code |
| `DELETE` | `/api/groups/{groupId}` | 🅿️ | delete group |

### Daily (core flow)

| Method | Endpoint | Status | Notes |
|---|---|---|---|
| `GET` | `/api/groups/{groupId}/daily/current` | ✅ | redesigned → `{ today, selection }` (§4.7); driven off `ActivatedAt` state |
| `POST` | `/api/groups/{groupId}/daily/select` | ✅ | sets the **pending (next-day)** question; never activates early; inline-create runs full §9 validation |
| `POST` | `/api/groups/{groupId}/daily/vote` | ✅ | submit vote; **returns fresh `QuestionResultDto`** |
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

### Pending (MVP)

MVP feature-complete. All 11 ramas merged. Phase 2 backlog in §16.

---

## 13. Roadmap & Branch Plan

Each branch is **backend + its Angular UI**, cut from `master`, merged before the next. The **design-system** branch (rama 4) is frontend-only and may run in parallel with the backend ramas 1–3. Packs management, gamification, chat, SignalR, OAuth, avatars, per-group time-zone logic, fair rotation, and multi-language gameplay are **Phase 2**.

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
| 12 | `feat/realtime-signalr` | *(optional)* SignalR hub + Angular client; replaces the refresh button. |

**Phase 2 backlog:** thematic packs management · "adivina el autor" · points/streaks/ranking · priority-boost + in-app currency · in-question chat/debate · push notifications · Google OAuth · avatars · per-group time-zone logic · activity-aware selector rotation · history late-vote-to-unlock · monthly statistics · email verification & password recovery · HttpOnly-cookie refresh tokens · invitation QR · multi-language gameplay (i18n) · SSR/SEO · caching · automated tests · CI/CD. **Full developer wishlist in §16.**

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
- Don't implement Phase 2 items (packs management, gamification, chat, SignalR, OAuth, avatars) without explicit confirmation.

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
- 🟡 **"Email/username already registered" message** — backend already returns `409 Conflict`; show it in the register form.
- 🔵 **Email verification** — needs an email provider; adds `User.EmailVerified`.
- 🔵 **Password recovery** — needs an email provider.
- 🔵 **HttpOnly-cookie refresh tokens** — move refresh token out of JS-accessible storage (security hardening).
- 🔵 **Change JWT `Issuer`/`Audience`** once a real domain exists (do together with CORS/domain setup).

### Groups & invitations
- ✅/🟢 **Group names are NOT unique** — labels only, no existence check (decided).
- 🟡 **"You're already in this group" on join** — backend already detects membership; give `join` a clear response so the UI can message it.
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
- 🔵 **User avatar image** — `User.AvatarUrl` + upload/display (MVP shows initials).
- 🔵 **Multi-language gameplay (i18n)** — play in other languages: Angular i18n/Transloco on the client + translated base-pack templates server-side. User-created questions stay in the author's language.

### Daily, rotation & gamification
- 🟢 **Per-group time zone column** — `Group.TimeZoneId` added now (rama 0); logic stays UTC+2 until Phase 2.
- 🔵 **History gating + late voting** — require having answered to see past results; let users answer *late* to unlock. Pairs with coins. **Open decision:** do late votes count in the tally? (MVP leaves history open — §4.6.)
- 🔵 **Activity-aware selector rotation** — weight rotation by recent participation; a member inactive for weeks shouldn't keep being picked and defaulting to an auto-question. Replaces the MVP modulo rotation (§4.4).
- 🔵 **"Race to select"** — if the selector hasn't chosen in the last 10 min before T, notify everyone; the first to pick a question wins coins and becomes the selector. Needs notifications + coins.
- 🔵 **Re-enable `Vote.GuessedCreatorId`** when "adivina el autor" ships (fields already stubbed in `Vote`).
- 🔵 **Monthly statistics** per group/member.
- 🔵 **Notification system** (push) — in-app "te toca elegir" indicator is already MVP (§4.7).

### Infra / ops
- 🔵 **Free domain** via name.com + GitHub; then update Issuer/Audience, CORS `AllowedOrigins`, and frontend API base URL.
