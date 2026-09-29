# P0 Local Backend Verification Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Verify the recovered Nova Haven backend against its current Wiki contract, generate and review the first EF Core migration for local SQL Server, and record any toolchain blockers before expanding scope.

**Architecture:** Keep the existing modular monolith: ASP.NET Core API owns HTTP/auth/Problem Details, Application owns editorial validation, Domain owns entities/invariants, and Infrastructure owns EF Core/Identity/migrations. SQL Server is local/disposable only for this batch; no production database or Minecraft plugin database is touched.

**Tech Stack:** .NET 10, EF Core/SQL Server 10.0.12, ASP.NET Core Identity, xUnit, Node 22+; Next.js and Flutter are verified only when their SDK/dependencies are available.

**Spec:** `openspec/changes/add-wiki-cms/specs/wiki-content/spec.md`, `docs/superpowers/specs/2026-09-17-nova-haven-platform-design.md`, `AGENTS.md`

## Global Constraints

- Public Wiki reads expose only the current published revision; drafts and historical revisions remain private.
- Admin mutations require server-side Admin authorization, CSRF protection where cookie auth is used, and ETag/If-Match preconditions.
- UUIDs are serialized as strings, timestamps are UTC, and API errors use safe Problem Details.
- EF migrations are reviewed artifacts; do not use `EnsureCreated` or edit a Minecraft/plugin database.
- SQL Server verification uses only a disposable local instance and local credentials kept out of Git/source.
- No media, News, bookmarks, Minecraft integration, microservice, broker, GraphQL, or unrelated refactor is included in this batch.

## Review Focus

- Migration coverage: Identity tables plus Wiki articles, revisions, categories, tags, draft-tag and revision-tag joins, indexes, rowversion columns, and restrictive foreign keys.
- Publication isolation: draft edits, restore, unpublish, and historical tags must not alter public reads.
- Relational concurrency: stale rowversion/ETag, unique slug/classification constraints, and SQL deadlock/foreign-key conflicts must map to the documented status codes.
- Local startup: API must require an explicit local connection string and must not create schema during startup.
- Cross-client contract: web and Flutter may only consume the same versioned public API; missing SDKs must be reported as blockers rather than guessed around.

### Task 1: Toolchain and source verification

**Files:**
- Read: `AGENTS.md`, `README.md`, `LUNA_HANDOFF.md`, `docs/roadmap/NOVA-HAVEN-ROADMAP.md`, OpenSpec Wiki spec/tasks, backend source, `contracts/openapi/wiki-v1.json`
- Test: root Node suite and backend domain tests

**Interfaces:**
- Produces: verified baseline commands, SDK availability, and the exact source model used by migration generation.

- [x] **Step 1: Run the root contract/source suite**

  Run: `npm test`

  Expected: all current Node tests pass; record exact count and exit code.

- [x] **Step 2: Restore and build the API**

  Run: `dotnet restore backend/NovaHaven.Api/NovaHaven.Api.csproj` then `dotnet build backend/NovaHaven.Api/NovaHaven.Api.csproj --no-restore`

  Expected: all referenced projects compile with zero warnings and zero errors.

- [x] **Step 3: Run domain tests**

  Run: `dotnet test tests/backend/NovaHaven.Domain.Tests/NovaHaven.Domain.Tests.csproj --no-restore`

  Expected: all discovered xUnit tests pass.

### Task 2: Generate and review InitialWiki migration

**Files:**
- Create: `backend/NovaHaven.Infrastructure/Data/Migrations/*_InitialWiki.cs`
- Create: `backend/NovaHaven.Infrastructure/Data/Migrations/NovaDbContextModelSnapshot.cs`
- Read: `backend/NovaHaven.Infrastructure/Data/NovaDbContext.cs`, `backend/NovaHaven.Infrastructure/Data/NovaDbContextFactory.cs`
- Test: EF design-time migration build/model inspection

**Interfaces:**
- Consumes: `NOVA_DB_CONNECTION` and the latest `NovaDbContext` model.
- Produces: a reviewed migration that can create the local schema for Identity and Wiki persistence.

- [x] **Step 1: Confirm no migration exists and configure a local-only design-time connection**

  Run: `rg --files backend/NovaHaven.Infrastructure | Select-String 'Migrations'` and set a process-local `NOVA_DB_CONNECTION` pointing to the disposable local SQL Server endpoint.

  Expected: no prior migration/snapshot is present and no credential is written to source.

- [x] **Step 2: Generate the migration**

  Run: `dotnet ef migrations add InitialWiki --project backend/NovaHaven.Infrastructure --startup-project backend/NovaHaven.Api --context NovaDbContext --output-dir Data/Migrations`

  Expected: migration and model snapshot are created from the current model without contacting a production database.

- [x] **Step 3: Review schema coverage**

  Check that the generated migration contains Identity tables; `WikiArticles`, `WikiArticleRevisions`, `WikiCategories`, `WikiTags`, `WikiDraftTags`, `WikiRevisionTags`; unique slug/name indexes; rowversion columns; and `Restrict` foreign-key behavior for draft/history references.

  Expected: every model mapping in `NovaDbContext` is represented and no unrelated table or destructive operation appears.

- [x] **Step 4: Build with the generated migration**

  Run: `dotnet build backend/NovaHaven.Api/NovaHaven.Api.csproj --no-restore`

  Expected: zero warnings and zero errors.

### Task 3: Local SQL Server integration gate

**Files:**
- Modify: `docs/roadmap/NOVA-HAVEN-ROADMAP.md` with evidence only
- Modify: `docs/verification/2026-09-21-p0-local-backend.md`
- Test: local SQL Server migration update and `npm run smoke:wiki`

**Interfaces:**
- Consumes: `InitialWiki`, local `ConnectionStrings__NovaDb`, development-only `SeedAdmin` settings.
- Produces: evidence for migration application and the Wiki lifecycle/concurrency smoke path, or an explicit environment blocker.

- [x] **Step 1: Verify a disposable local SQL Server is available**

  Run: inspect local SQL Server services/LocalDB and Docker availability; use only `127.0.0.1:14333` or a documented local instance.

  Expected: a reachable disposable local SQL Server, or a recorded blocker naming the missing runtime.

- [x] **Step 2: Apply the migration locally**

  Run: `dotnet ef database update --project backend/NovaHaven.Infrastructure --startup-project backend/NovaHaven.Api --context NovaDbContext`

  Expected: local database update succeeds; no `EnsureCreated` path is used.

- [x] **Step 3: Run API and smoke lifecycle**

  Run: start API on `http://127.0.0.1:5080` with Development-only seed settings, then run `npm run smoke:wiki` with local-only environment variables.

  Expected: draft remains private, publish/republish/restore/unpublish behavior, ETags/CSRF/role guards, classification reference guards, and tag historical isolation pass against SQL Server.

### Task 4: Client build gates

**Files:**
- Modify: `docs/roadmap/NOVA-HAVEN-ROADMAP.md` with evidence only
- Test: `apps/web` typecheck/build and `apps/mobile` pub get/test/analyze when SDKs are available

**Interfaces:**
- Consumes: the existing OpenAPI v1 contract and public API client code.
- Produces: verified web/mobile build status with explicit missing-toolchain blockers.

- [x] **Step 1: Install web dependencies and typecheck/build**

  Run from `apps/web`: `npm install`, `npm run typecheck`, `npm run build`.

  Expected: all commands exit 0, or the exact dependency/toolchain failure is recorded.

- [x] **Step 2: Run Flutter checks**

  Run from `apps/mobile`: `flutter pub get`, `flutter test`, `flutter analyze`.

  Expected: all commands exit 0, or missing Flutter SDK is recorded without changing architecture.

## Execution rulings

- The checkpoint had no `.git`, so the plan ran in the supplied workspace without creating fake history or a branch.
- LocalDB was used because the owner explicitly scoped the database to local projects and Docker was unavailable.
- The API startup project received the EF design-time package only after EF CLI reproduced the missing-package error; runtime architecture was unchanged.
