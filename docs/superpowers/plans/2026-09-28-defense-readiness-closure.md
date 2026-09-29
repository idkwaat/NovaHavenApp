# Nova Haven Defense Readiness Closure — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Close the highest-value local capstone acceptance gaps and leave a repeatable, evidence-based defense package without claiming deployment or unverified mobile rendering.

**Architecture:** Keep the existing modular monolith and shared published Wiki API. Add only read-only cross-client verification, repeated relational concurrency coverage using the existing disposable LocalDB fixture, and defense documentation derived from the approved design and current EF model. Make the published revision visible in the Flutter article reader so parity is inspectable.

**Tech Stack:** .NET 10, EF Core/SQL Server LocalDB, Next.js 16, Flutter 3.44, Node.js native test runner, Mermaid.

**Spec:** `docs/superpowers/specs/2026-09-17-nova-haven-platform-design.md`; `openspec/changes/add-wiki-cms/specs/wiki-content/spec.md`.

## Global Constraints

- `NovaHaven_Local` is read-only for this work; no migration, seed, Admin bootstrap, or mutating smoke against it.
- Relational mutation/concurrency tests use `LocalApiFactory`'s uniquely named disposable LocalDB and its cleanup path.
- Public Wiki responses remain anonymous and published-only; web and Flutter read the same API revision.
- No production deployment, online service integration, Minecraft database access, or secret persistence.
- This checkpoint has no `.git`; do not claim branch, diff, or commit history.

## Review Focus

- A stale server-rendered page can look like an API outage — verify a fresh request and distinguish HTTP health from data endpoints.
- Web and Flutter can show different article snapshots — compare published title, slug, revision and content in the read-only checker and Flutter detail test.
- Two editors can submit one ETag concurrently — repeat the race against the isolated SQL Server fixture and assert exactly one write wins.
- A readiness script could accidentally mutate user data — test local-origin validation and ensure it issues GET requests only.
- Defense material can overstate completion — label checked, partial and not-tested evidence explicitly.

---

### Task 1: Make the Flutter published revision inspectable

**Files:**
- Modify: `apps/mobile/lib/main.dart`
- Test: `apps/mobile/test/mobile_experience_test.dart`

**Interfaces:**
- Consumes: `WikiArticleDetail.revision`, `title`, `summary`, `markdown` from `apps/mobile/lib/wiki_api.dart`.
- Produces: Article detail metadata that visibly identifies `Phiên bản N` while preserving the existing dark-earth composition.

- [x] Add a widget assertion that the loaded detail shows its API revision and published body; first confirm the assertion fails.
- [x] Render `Phiên bản ${item.revision}` as secondary metadata near the title without shrinking body text or touch targets.
- [x] Run the targeted Flutter widget test, full Flutter tests and analyzer.

### Task 2: Add a read-only web/API defense smoke

**Files:**
- Create: `scripts/defense-readiness-smoke.mjs`
- Create: `tests/web/defense-readiness-smoke.test.mjs`
- Modify: `package.json`

**Interfaces:**
- Produces: `runDefenseReadinessSmoke({apiOrigin, webOrigin, fetchImpl})` which accepts loopback origins only, uses GET only, and returns a compact verification summary.
- Verifies: API health, public article list/detail consistency, and that the corresponding SSR Wiki detail page contains the same published title and revision.

- [x] Test rejection of non-loopback origins before any request is made.
- [x] Test successful matching and mismatched API/detail/page results using deterministic local fake responses.
- [x] Implement the minimal read-only checker and add `npm run smoke:defense`.
- [x] Run Node tests and the checker against the existing local API/web preview; do not mutate the database.

### Task 3: Repeat ETag concurrency against isolated LocalDB

**Files:**
- Modify: `tests/backend/NovaHaven.Integration.Tests/WikiApiIntegrationTests.cs`

**Interfaces:**
- Consumes: the existing `LocalApiFactory` isolation, Admin session and CSRF/If-Match helpers.
- Produces: `ConcurrentArticleEditsWithSameEtagHaveOneWinnerPerRound` using a fresh per-test database.

- [x] Add a test that creates and publishes one article, then runs ten pairs of concurrent PATCH requests with the same ETag.
- [x] Assert one HTTP 200 and one conflict (HTTP 412 for a stale ETag or documented HTTP 409 for SQL Server deadlock 1205) each round, then re-read draft state and verify it equals the single winning payload.
- [x] Run the focused test and the full integration test suite; verify factory cleanup removes only its generated test database.

### Task 4: Prepare defense artifacts from source evidence

**Files:**
- Create: `docs/defense/ERD.md`
- Create: `docs/defense/DEMO-GUIDE.md`
- Create: `docs/defense/READINESS-CHECKLIST.md`

**Interfaces:**
- ERD: Mermaid diagrams grounded in actual `NovaDbContext` table mappings and FK configuration; separate identity, editorial and domain-content modules.
- Demo guide: safe local startup, read-only public preview, Admin lifecycle, cross-client parity and reproducible test commands with secrets represented only by placeholders.
- Checklist: evidence states `PASS`, `PARTIAL`, `NOT RUN` and a clear distinction between local capstone readiness and production readiness.

- [x] Cross-check each table/entity relationship against `NovaDbContext` and the read-only local SQL Server FK inventory before documenting it.
- [x] Write the walkthrough and checklist without inventing a school rubric or implying deployment/mobile visual acceptance.
- [x] Update `docs/roadmap/NOVA-HAVEN-ROADMAP.md` and the current readiness report only with commands actually run in this batch.
