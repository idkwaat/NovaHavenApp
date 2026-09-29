# Classification transaction hardening implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Prevent the article/category race from allowing an article draft or public revision to reference a deactivated category, while returning a safe conflict for SQL Server deadlocks.

**Architecture:** Keep the existing Minimal API modular monolith. Wrap classification read/check/write in one SQL Server `Serializable` transaction for category update/delete and article create/edit/publish/restore. Preserve existing EF rowversion/If-Match and FK constraints. A narrow Admin mutation endpoint filter maps only SQL error 1205 (including wrapped errors) to HTTP 409.

**Tech Stack:** .NET 10 ASP.NET Core, EF Core SQL Server, Node 22 native tests, optional live SQL Server smoke.

**Spec:** `openspec/changes/add-wiki-cms/specs/wiki-content/spec.md`; design `docs/superpowers/specs/2026-09-17-nova-haven-platform-design.md`.

## Global constraints

- Do not change public Wiki endpoints or Minecraft plugin data ownership.
- Do not call an unexecuted SQL smoke test a pass; no SDK or Docker is currently installed.
- `If-Match` remains mandatory for every existing state-changing article/category endpoint except article/category creation.
- Keep the original category references and history intact; do not introduce cascade deletes.

---

### Task 1: Test transaction boundaries and deadlock response

**Files:** Create `tests/web/classification-transaction.test.mjs`; inspect `backend/NovaHaven.Api/Endpoints/AdminWikiEndpoints.cs`.

**Interfaces:** `MapAdminWikiEndpoints(WebApplication)`, `SqlServerConflict.IsDeadlock(Exception)`.

- [x] Add Node tests that inspect each named route handler for `BeginTransactionAsync(IsolationLevel.Serializable, ct)` before its first database query and `CommitAsync` after successful `SaveChangesAsync`.
- [x] Run `npm test`; observed 8 failures before adding transaction guards and race smoke.
- [x] Add a Node guard requiring HTTP 409 mapping for a wrapped SQL Server 1205 in the Admin mutation filter.

### Task 2: Implement transactional boundaries

**Files:** Modify `backend/NovaHaven.Api/Endpoints/AdminWikiEndpoints.cs`; create `backend/NovaHaven.Api/Infrastructure/SqlServerConflict.cs`.

**Interfaces:** `SqlServerConflict.IsDeadlock(Exception): bool`; `SaveWithPrecondition(NovaDbContext,HttpContext,WikiArticle,CancellationToken): Task<(IResult Result, bool Saved)>`.

- [x] Use serializable transactions before category PATCH/DELETE and article POST/PATCH/publish/restore's initial reads.
- [x] Commit after successful `SaveChangesAsync`; `SaveWithPrecondition` returns `Saved` so the caller commits only on success. ETag headers are set only after the commit.
- [x] Filter deadlocks narrowly and map to 409. Preserve 412 for rowversion failures, 428 for missing ETag, and 409 for uniqueness/FK violations.
- [x] Run `npm test`; 25 structural/helper/contract checks pass (not C# build or SQL verification).

### Task 3: Add disposable live concurrency regression

**Files:** Modify `scripts/wiki-smoke.mjs`, `tests/web/classification-transaction.test.mjs`, `docs/verification/2026-09-18-classification-concurrency.md`.

**Interfaces:** `request(path, options)` currently provided by the smoke script; local-only opt-in credentials.

- [x] Create a distinct test category and race its deactivation against article creation using `Promise.all`.
- [x] Assert both mutations cannot succeed and that successful article creation implies the category remains active.
- [x] Run `node --check scripts/wiki-smoke.mjs` and `npm test`; do **not** report a real database result without running `npm run smoke:wiki` against SQL Server.
- [x] Record actual verification status and update roadmap; do not mark M1/M2 VERIFIED until build, migration and SQL integration gates pass.
