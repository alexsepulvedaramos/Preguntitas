# VayaPreguntita API — local development

ASP.NET Core (.NET 10) REST API backing [Vaya Preguntita](../README.md). PostgreSQL via EF Core (code-first), FluentValidation, AutoMapper, and a `BackgroundService` that drives the daily question lifecycle.

## 1. Prerequisites

- **[.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)**
- **EF Core tools** (global tool, needed for migrations):

```bash
dotnet tool install --global dotnet-ef
```

- **Docker** (only for running the integration tests — they spin up PostgreSQL with Testcontainers).

## 2. Setup

**Step 1 — restore dependencies:**

```bash
cd server/VayaPreguntita.API
dotnet restore
```

**Step 2 — configure secrets.** The database connection string is not tracked in version control; store it with user-secrets (the `.csproj` already declares a `UserSecretsId`):

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "YOUR_POSTGRESQL_CONNECTION_STRING"
```

Any local PostgreSQL instance works; production uses Supabase.

**Step 3 — apply migrations:**

```bash
dotnet ef database update
```

**Step 4 — run:**

```bash
dotnet run
```

The API listens on `http://localhost:5212` (matching the Angular client's `environment.development.ts`). On startup, an idempotent seeder populates the 15 question packs.

## 3. Tests

Integration tests live in `VayaPreguntita.API.Tests` (xUnit + `WebApplicationFactory` + Testcontainers PostgreSQL). With Docker running:

```bash
cd server
dotnet test
```

## 4. Deployment

Pushes to `master` touching `server/**` trigger the [GitHub Actions workflow](../.github/workflows/deploy-backend.yml): build → test → Render deploy hook. Render builds the [Dockerfile](Dockerfile) in this folder. Secrets (connection string, JWT key, VAPID keys) are Render environment variables.

## 5. Project conventions

See the [spec](../docs/specs/vaya-preguntita.md) (§14) — highlights: DTOs at API boundaries (never EF entities), business logic in services only, every schema change through an EF migration, FluentValidation for all inputs.
