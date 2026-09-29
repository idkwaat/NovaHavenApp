# Rebuild Wiki foundation implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reconstruct a coherent first Wiki slice from the approved design because the earlier claimed source is not present in this container; keep missing work explicit.

**Architecture:** One ASP.NET Core modular monolith with EF Core/SQL Server and Identity owns drafts and immutable public revisions. Next.js renders published reads; Flutter reads the same REST contract. No Minecraft integration or production deployment in this increment.

**Tech Stack:** .NET 10, EF Core SQL Server 10, Next.js/TypeScript, Flutter, Node 22 for executable frontend pure-function tests.

**Spec:** `openspec/changes/add-wiki-cms/specs/wiki-content/spec.md`; `docs/superpowers/specs/2026-09-17-nova-haven-platform-design.md`

## Global Constraints

- IDs UUID string in JSON; published-only public queries.
- Title 1–120, slug 3–120 lowercase alnum joined by single hyphens, summary max 300, body 1–50000.
- Server-side Admin authorization and antiforgery for cookie-based mutations.
- Require If-Match on edit/publish/unpublish/restore and preserve immutable revision snapshots.
- No direct Minecraft DB and no fake test or build result; EF migration requires SDK and SQL Server.
- Image upload/tag completeness are separate increments; do not claim completed by this foundation.

---

### Task 1: Repository and executable contract baseline

**Files:** `package.json`, `tests/web/wiki-model.test.mjs`, `apps/web/src/lib/wiki-model.ts`, `contracts/openapi/wiki-v1.json`.

**Interfaces:** Produces `validateArticle(input)`, `normalizeSearch(query)`, `isSafeLink(href)` for Next public/Admin UI.

- [x] Write tests for title/slug/markdown bounds, query bound, unsafe protocols and public DTO contract.
- [x] Run `npm test` and confirm expected initial import failure.
- [x] Implement pure validation/link helpers and initial OpenAPI path/security contract (schema completeness remains open).
- [x] Run `npm test` and confirm green.

### Task 2: ASP.NET layered editorial backend

**Files:** `backend/NovaHaven.Domain/*`, `backend/NovaHaven.Application/*`, `backend/NovaHaven.Infrastructure/*`, `backend/NovaHaven.Api/*`, `tests/backend/*`.

**Interfaces:** Public `GET /api/v1/wiki/articles`, `GET /api/v1/wiki/articles/{slug}`, `GET /api/v1/wiki/categories`; admin create/edit/publish/unpublish/restore; CSRF token + login.

- [x] Define xUnit tests for draft validation and immutable revision creation (execution blocked by missing SDK).
- [x] Define entities/validator, EF model unique constraints/rowversion, HTTP DTOs and endpoints (compile unverified).
- [x] Write SDK/SQL integration verification commands and scripted smoke; blocked from running without SDK and SQL Server.

### Task 3: Readable web and mobile consumers

**Files:** `apps/web/app/*`, `apps/web/src/*`, `apps/mobile/lib/*`, `apps/mobile/pubspec.yaml`.

**Interfaces:** Consumers use published-only API; web admin calls authenticated API with CSRF and ETag.

- [x] Add web API client, public list/details pages with sanitized Markdown and admin login/editor source.
- [x] Add Flutter published-only list/search/detail with loading/error/empty states (compile unverified).
- [x] Run available native Node tests; document blocked Next and Flutter build/analyze.

### Task 4: Roadmap and verification

**Files:** `docs/roadmap/NOVA-HAVEN-ROADMAP.md`, `README.md`, `openspec/changes/add-wiki-cms/tasks.md`.

- [x] Record completed vs blocked requirements without invented progress percentages.
- [x] Run native Node tests and check available toolchain. The other commands remain blocked and are NOT marked passed.
- [x] Save source in working folder; do not send source archive until user asks.
