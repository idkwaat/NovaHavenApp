# Nova Haven v2 Local Completion Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reconcile the v2 all-systems handoff with the recovered source, improve the real local product where its data and contracts exist, and leave every external dependency explicitly evidenced instead of simulated.

**Architecture:** Keep the existing ASP.NET Core modular monolith, SQL Server LocalDB, Next.js web/admin and Flutter reader. The Wiki publication model remains the source of truth for public content; new game-facing epics are not fabricated without an authoritative source or provider contract.

**Tech Stack:** .NET 10 / EF Core SQL Server 10.0.12, LocalDB, Next.js 16 / React 19 / TypeScript, Flutter 3.44 / Dart, Node tests, xUnit.

**Spec:** `docs/superpowers/specs/2026-09-21-complete-platform-design.md`, `openspec/changes/add-wiki-cms/specs/wiki-content/spec.md`, and `.incoming-v2/NOVA_HAVEN_48H_ALL_SYSTEMS_ROADMAP.md`.

## Global Constraints

- SQL Server is local/disposable only; no production or Minecraft/plugin database access.
- Anonymous clients see only current published snapshots; Admin mutations retain authorization, CSRF and ETag checks.
- UUIDs are strings, timestamps are UTC, errors use Problem Details, and schema changes use reviewed EF migrations.
- No invented Minecraft data, Discord integration, payment success, plugin SQL writes or production deployment claims.
- Every production-code change has a failing test first, a focused green run, then a full relevant suite.

## Review Focus

- Published-only related links must never select drafts or superseded revisions; test with a private candidate.
- Markdown heading extraction must be deterministic and safe for duplicate/untrusted headings; test generated anchors.
- API contract, TypeScript client, Flutter client and rendered pages must expose the same related-link shape.
- LocalDB must be started explicitly before relational tests; a stopped instance is an environment blocker, not a source pass.
- E01–E14 status must distinguish local evidence from external/provider blockers.

### Task 1: v2 audit and evidence matrix

**Files:**
- Create: `docs/roadmap/48H-ALL-SYSTEMS-STATUS.md`
- Read: `.incoming-v2/NOVA_HAVEN_48H_ALL_SYSTEMS_README.md`, `.incoming-v2/NOVA_HAVEN_48H_ALL_SYSTEMS_ROADMAP.md`, `.incoming-v2/NOVA_HAVEN_LUNA_ALL_SYSTEMS_48H_PROMPT.md`

- [x] Extract the v2 documents into an isolated comparison directory.
- [x] Compare every E01–E14 claim with actual source files and current test gates.
- [x] Record local/live/E2E status and exact fresh evidence without calling roadmap text a shipped feature.

### Task 2: Wiki Engine 2.0 published related links and safe TOC

**Files:**
- Modify: `backend/NovaHaven.Api/Endpoints/PublicWikiEndpoints.cs`
- Modify: `apps/web/src/lib/wiki-api.ts`, `apps/web/src/lib/wiki-model.ts`
- Modify: `apps/web/app/wiki/[slug]/page.tsx`
- Modify: `apps/mobile/lib/wiki_api.dart`, `apps/mobile/lib/main.dart`
- Test: `tests/web/wiki-api.test.mjs`, `tests/web/wiki-model.test.mjs`, `tests/backend/NovaHaven.Integration.Tests/WikiApiIntegrationTests.cs`, `apps/mobile/test/wiki_api_test.dart`
- Modify: `contracts/openapi/wiki-v1.json`

**Interfaces:**
- Consumes: current published article query, current published category/tag joins and existing Markdown renderer.
- Produces: a `related` array containing only published article summaries and a deterministic client-side table of contents; no new external dependency or DB schema.

- [x] Add a Node/API test that expects the public detail contract to expose `related` and a model test for duplicate-safe heading IDs.
- [x] Run the focused tests and confirm they fail because the shape/helper does not exist.
- [x] Add the smallest published-only related query and shared web/mobile model parsing.
- [x] Add a safe TOC helper that accepts only Markdown ATX headings, caps depth at 3 and creates stable `heading-N` IDs without rendering raw HTML.
- [x] Render the web TOC and related links; render related links and a compact TOC in Flutter without making Markdown links executable.
- [x] Update OpenAPI and run Node, integration, web and Flutter focused tests.

### Task 3: local verification and delivery evidence

**Files:**
- Modify: `docs/roadmap/NOVA-HAVEN-ROADMAP.md`, `README.md`, `LUNA_HANDOFF.md`
- Modify: `docs/verification/2026-09-21-complete-platform.md`

- [x] Start `MSSQLLocalDB` explicitly and run migrations drift/update checks.
- [x] Run Node, .NET build/domain/integration, API smoke, Next typecheck/build, Flutter test/analyze/APK gates.
- [x] Run a self-review against the v2 matrix and document browser E2E, emulator, stress, external integration and production limits.

## Execution order

1. Task 1 is already complete and supplies the evidence boundary.
2. Task 2 changes only the real Wiki surface and must keep all current published-only tests green.
3. Task 3 runs after Task 2 and updates evidence only from fresh command output.

Because this checkpoint has no usable `.git` history, completion is recorded in the workspace ledger and verification files rather than by fabricated commits or branches.
