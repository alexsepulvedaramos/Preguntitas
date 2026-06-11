---
trigger: always_on
---

# Copilot Instructions

## Source of truth

- All project rules and specs live in docs/specs/. Read these before coding: docs/specs/vaya-preguntita-core.md and docs/specs/vaya-preguntita-roadmap.md.
- If a behavior is confirmed by the user and relevant to app functionality, update the corresponding spec so it is not re-asked.

## Repo structure and architecture

- Backend API: server/VayaPreguntita.API (ASP.NET Web API, EF Core, AutoMapper).
- Frontend: client/ (Angular v19+).
- Solution file: server/VayaPreguntita.slnx.
- Question rules are stored in Question.Metadata (JSONB in Postgres/Supabase); mapping lives in server/VayaPreguntita.API/Profiles/MappingProfile.cs.

## Question types and voting payloads (spec-defined)

- Superlative: dynamic group member selection; vote uses SelectedTargetUserId; Nobody is sentinel value 0 when AllowNobody=true.
- Deathmatch: teams are defined at creation via Teams; vote uses SelectedTargetUserIds matching exactly one configured team.
- Scale: numeric vote via NumericValue with RangeMin/RangeMax and TargetUserId.
- Secret Pairing: selection list with MinSelections=2 and MaxSelections=2.
- Custom Poll: options list; MinSelections/MaxSelections control single vs multi-select.

## Validation patterns

- FluentValidation validators live in server/VayaPreguntita.API/Validators.
- Question creation and vote validation logic lives in server/VayaPreguntita.API/Controllers/QuestionsController.cs.

## Dev workflows (from README)

- Restore: dotnet restore (run in server/VayaPreguntita.API).
- User secrets: dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<SUPABASE_CONNECTION_STRING>".
- Migrations: dotnet ef database update.
- Run API: dotnet run.

## Project conventions (specs)

- English-only names and comments.
- Use DTOs for API boundaries; keep database logic out of controllers.
- Always use migrations for schema changes.
- Use atomic commits with Conventional Commits and push when a task is approved.
