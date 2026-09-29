# Nova Haven Complete Platform Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Finish the remaining code-deliverable Nova Haven MVP slices locally, with tests and evidence for backend persistence, media, Admin revision/audit UX, News, bookmarks, client builds and E2E-ready delivery.

**Architecture:** Preserve the modular monolith. ASP.NET Core owns HTTP, identity, policies and Problem Details; Application/Domain own editorial rules; Infrastructure owns EF Core/Identity, local SQL Server migration and private local media storage. Next.js and Flutter consume the versioned REST contract; public reads never access drafts.

**Tech Stack:** .NET 10 / EF Core SQL Server 10.0.12, SQL Server LocalDB, Next.js 16 / React 19 / TypeScript, Flutter 3.44 / Dart 3.12, Node 24 tests, xUnit.

**Spec:** `docs/superpowers/specs/2026-09-21-complete-platform-design.md`, `openspec/changes/add-wiki-cms/specs/wiki-content/spec.md`, `AGENTS.md`

## Global Constraints

- SQL Server is local/disposable only; never touch production or a Minecraft/plugin database.
- Public endpoints expose only current published revisions; drafts and historical revisions remain private.
- Admin mutations require server-side authorization, CSRF for cookies, safe Problem Details and ETag/If-Match where concurrent edits matter.
- UUIDs serialize as strings, timestamps are UTC, and EF migrations are the only schema changes.
- Media uses a private local root, random storage keys and published-reference checks.
- Markdown is inert/sanitized; no raw HTML/script/event-handler/unsafe URL execution.
- No microservices, broker, GraphQL, generic repository, direct Minecraft SQL, or speculative gameplay logic.

## Review Focus

- Cross-layer publication isolation: a draft/media/tag/revision change must not leak to anonymous web or Flutter reads.
- Relational failure semantics: unique conflicts, rowversion conflicts, FK/reference guards and transaction rollback must return documented status codes without partial state.
- Local filesystem security: uploads must be signature-checked, bounded, unguessably named and inaccessible until a current published revision references them.
- Identity boundary: anonymous reads work, anonymous/non-admin mutations fail, and no long-lived browser secret is stored in localStorage.
- Contract parity: OpenAPI, API DTOs, Next.js client, Flutter client and smoke tests must agree on field names and published-only behavior.

### Task 1: Core backend acceptance and contract hardening

**Files:**
- Modify: `backend/NovaHaven.Api/Endpoints/*.cs`, `backend/NovaHaven.Api/Program.cs`
- Modify: `backend/NovaHaven.Infrastructure/Data/NovaDbContext.cs`
- Modify: `contracts/openapi/wiki-v1.json`
- Create/Modify: `tests/backend/NovaHaven.Integration.Tests/*`
- Modify: `tests/web/*.test.mjs`, `scripts/wiki-smoke.mjs`
- Test: .NET build/xUnit, LocalDB integration, Node tests, smoke

**Interfaces:**
- Produces: tested Problem Details/auth/concurrency behavior and a complete current Wiki API contract.

- [ ] Add failing integration tests for anonymous 401, non-admin 403, stale ETag 412/428, duplicate conflicts, rollback and published-only privacy.
- [ ] Run the tests against current code and record each expected failure.
- [ ] Implement the smallest endpoint/policy/transaction fixes.
- [ ] Run focused integration tests, then `npm test`, backend tests, migration drift and LocalDB smoke.
- [ ] Complete OpenAPI request/response schemas, Problem Details and examples to match actual endpoints.

### Task 2: Private media persistence and gateway

**Files:**
- Create: `backend/NovaHaven.Domain/Wiki/WikiMedia.cs`, media reference entities
- Modify: `backend/NovaHaven.Infrastructure/Data/NovaDbContext.cs`
- Create: media validation/storage services in Application/Infrastructure
- Modify: `backend/NovaHaven.Api/Endpoints/AdminWikiEndpoints.cs`, `PublicWikiEndpoints.cs`
- Create: EF migration `AddWikiMedia`
- Create: backend media tests and Node contract tests
- Modify: Next.js/Flutter models only where contract requires

**Interfaces:**
- Consumes: article publish transaction and local configured media root.
- Produces: upload, authenticated preview and published-only public media endpoints.

- [ ] Add failing tests for signature/type/5MB limits, random storage keys, private draft 404, public revision reference and rollback.
- [ ] Implement validation, private filesystem storage, reference joins and gateway authorization.
- [ ] Generate/review/apply `AddWikiMedia` on LocalDB.
- [ ] Run focused tests and full suites.

### Task 3: Revision history, restore UX and audit log

**Files:**
- Create: `WikiAuditEntry` entity/mapping and migration
- Modify: Admin endpoints to write audit entries on successful/failed privileged mutations without secrets
- Modify: `apps/web/app/admin/page.tsx` and focused Admin components/styles
- Create: web tests for revision display, restore, ETag reload and audit rendering
- Modify: OpenAPI and verification docs

**Interfaces:**
- Consumes: existing revision list/restore API and ETag contract.
- Produces: Admin-visible history/restore/audit experience; public behavior remains unchanged.

- [ ] Add failing UI/contract tests for revision list, current-public marker, restore confirmation and stale reload.
- [ ] Implement API audit persistence and UI controls.
- [ ] Run web typecheck/build and LocalDB smoke.

### Task 4: News/changelog vertical slice

**Files:**
- Create: `openspec/changes/add-news/specs/news-content/spec.md`, `tasks.md`
- Create: News Domain/Application/Infrastructure/API code and migration
- Modify: Next.js public/Admin pages and API client
- Modify: Flutter API/client/read screen
- Modify: OpenAPI and Node/Flutter/backend tests

**Interfaces:**
- Produces: separate published-only News list/detail and Admin editorial lifecycle.

- [ ] Write the OpenSpec scenarios and failing domain/API/client tests.
- [ ] Implement draft/publish/unpublish/revision persistence and endpoints.
- [ ] Implement web/mobile readers and Admin editor.
- [ ] Generate/apply migration and run all client/backend tests.

### Task 5: Authenticated bookmarks vertical slice

**Files:**
- Create: `openspec/changes/add-bookmarks/specs/bookmarks/spec.md`, `tasks.md`
- Create: bookmark Domain/Infrastructure/API code and migration
- Modify: Flutter auth/session/bookmark UI using platform secure storage
- Modify: OpenAPI and tests

**Interfaces:**
- Consumes: Identity users and published article IDs.
- Produces: authenticated bookmark list/add/remove; no draft leakage and no browser localStorage token.

- [ ] Write OpenSpec scenarios and failing API/Flutter tests.
- [ ] Implement authorization, unique user/article constraint and published-only checks.
- [ ] Implement Flutter secure session/bookmark flow or record SDK blocker precisely.
- [ ] Generate/apply migration and run tests.

### Task 6: E2E/build/delivery proof

**Files:**
- Create: `tests/e2e/*` and browser test config if dependencies are available
- Modify: `package.json`, `README.md`, `docs/roadmap/NOVA-HAVEN-ROADMAP.md`, verification docs
- Create/Review: CI workflow only if repository hosting metadata exists; otherwise local CI command documentation

**Interfaces:**
- Consumes: all completed slices and LocalDB startup path.
- Produces: reproducible local demo and explicit infrastructure limits.

- [ ] Add failing E2E checks for draft privacy, publish, edit isolation, restore, unpublish and media visibility.
- [ ] Implement the browser runner against the local API and Next.js app.
- [ ] Run Node, .NET, web, Flutter and E2E gates; run Android build only if scaffold/SDK is available.
- [ ] Update roadmap/evidence/README without claiming production deployment.

## Execution order

1. Task 1 must be green before Task 2.
2. Task 2 must be green before Task 3 because Admin history must display media/public references correctly.
3. Task 3 is independent of Task 4 but shares Admin layout; finish Task 3 before expanding Admin navigation.
4. Task 4 and Task 5 use separate OpenSpec changes and migrations; never generate them in parallel.
5. Task 6 runs only after all feasible slices are green.
