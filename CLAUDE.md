# Vaya Preguntita

Monorepo: ASP.NET Core (.NET 10) API in `server/VayaPreguntita.API/` + Angular v19+ client in `client/`.

## Source of truth

**`docs/specs/vaya-preguntita.md` is the single source of truth** for product behavior, the daily lifecycle, question types, data model, API contract, validation rules, limits, and the branch/roadmap plan (§13). It also has a full backlog/wishlist in §16.

Before implementing anything in this repo:
1. Read the relevant section(s) of the spec — don't load the whole file unless asked; jump to the section that matters for the task.
2. If the requested behavior isn't covered there, ask — don't invent it.
3. If the developer confirms a functionally-relevant decision, update the spec so it's never asked again.

## Working agreement

- **One Claude Code session per branch.** Don't carry unrelated work across sessions; `/clear` before switching topics.
- Each rama in the spec's branch plan (§13) is backend + its Angular UI, cut from `master`.
- Model guidance: Sonnet by default; reach for Opus on the genuinely hard ramas (the daily-lifecycle rewrite, the base-pack clone/auto-resolution logic, SignalR auth) per the developer's plan.
- Don't run `dotnet ef database update` (apply a migration) without explicit confirmation.
- Don't implement Phase 2 items (see spec §13/§16) without explicit confirmation.
- **Update the spec inside the feature branch, before opening the PR.** Once the work is confirmed complete, mark the rama done in §12/§13 and fold in any confirmed spec changes as part of the same branch — don't defer it to a separate docs-only branch/PR afterwards.

## Conventions (see spec §14 for full detail)

- All identifiers, comments, and commit messages → **English**. UI-facing copy → **Spanish**.
- DTOs at API boundaries, never return EF entities. No DB/business logic in controllers — services only.
- Schema changes always go through an EF migration.
- Frontend UI: use the **Spartan skill** at `agents/skills/spartan` and Tailwind utility classes — don't hand-roll primitives Spartan already provides.
- Git commits: no `Co-Authored-By: Claude` trailer.
