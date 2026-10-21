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

## Releases & versioning (spec §14 "Releases")

Every PR that changes something players notice ships a release note — **inside the same branch, before opening the PR**:
1. Add a new entry at the top of `client/src/app/core/releases/releases.ts` (or extend the top entry if that version hasn't been merged/tagged yet). Written for players: what they'll notice, no internal names — in Spanish, English and German (every text is an `{ es, en, de }` object; `translations.spec.ts` fails if one is missing). The "Novedades" page and the app version read from this file.
2. Bump semver: new player-visible feature → MINOR, fixes only → PATCH. Set the same version in `client/package.json` and the two `version` fields at the top of `client/package-lock.json`.
3. Set the entry's `date` to the day it's merged.

**On merge**, tag the merge commit on `master` with an annotated tag and push it:
`git tag -a vX.Y.Z <merge-sha> -m "Vaya Preguntita X.Y.Z — <release title>"` then `git push origin vX.Y.Z`.

Internal-only changes (docs, tests, CI, refactors with no visible effect) get no release entry and no tag.

## Conventions (see spec §14 for full detail)

- All identifiers, comments, and commit messages → **English**. UI-facing copy → the i18n dictionaries (`client/src/app/core/i18n/translations/es.ts` + its `en.ts`/`de.ts` versions; player-facing API messages in `Localization/ServerText.cs`) — never hardcoded in templates.
- DTOs at API boundaries, never return EF entities. No DB/business logic in controllers — services only.
- Schema changes always go through an EF migration.
- Frontend UI: use the **Spartan skill** at `.agents/skills/spartan` and Tailwind utility classes — don't hand-roll primitives Spartan already provides.

## UI review gate (every frontend change)

Spec §14 "Frontend Design System & Style Guide" is **binding**. Whenever a branch touches the client UI (templates, component styles, new screens or components):
1. **Read §14 before writing the template**, not only at the end.
2. **Before committing, re-read §14 and check the diff against it**, rule by rule — at least: semantic colour tokens only (selected = `accent` border + `accent/15`, never ad-hoc `primary` tints); Spartan `hlmBtn`/`Hlm*` instead of hand-rolled buttons or native controls; touch targets (icon buttons ≥ `size-8`); `focus-visible:ring-2 focus-visible:ring-ring`; `transition-colors`; disabled at `opacity-60`; the two radii; people shown with `app-user-avatar` + `app-user-name`; no emoji in UI chrome (glyphs from `GLYPHS`); `aria-label`/`sr-only` on icon-only controls; copy only from the i18n dictionaries.
3. Say in the PR description that the §14 check was done and list anything that deliberately deviates (a deviation needs the developer's explicit OK).
- Git commits: no `Co-Authored-By: Claude` trailer.
