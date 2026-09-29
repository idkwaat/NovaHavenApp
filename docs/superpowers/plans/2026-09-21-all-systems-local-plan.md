# Nova Haven All Systems Local Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the remaining local-owned Nova Haven platform as connected, testable vertical slices while keeping Minecraft/plugin-owned data behind explicit contracts.

**Architecture:** Preserve the modular monolith. A shared published-revision knowledge graph connects NPCs, quests, locations, seasons and Catalog revisions. Community records use the same publication and authorization boundary. The API remains the only server contract; Next.js and Flutter consume published reads.

**Tech Stack:** .NET 10 / EF Core SQL Server 10.0.12 / LocalDB, Next.js 16 / React 19 / TypeScript, Flutter 3.44 / Dart 3.12, Node executable tests and xUnit.

**Spec:** `docs/superpowers/specs/2026-09-21-all-systems-local-design.md`

## Global Constraints

- SQL Server is local/disposable only; never touch production or a Minecraft/plugin database.
- Public endpoints expose only current published revisions; drafts and historical revisions remain private.
- Admin mutations require server-side authorization, CSRF, Problem Details and ETag/If-Match where concurrent edits matter.
- UUIDs serialize as strings, timestamps are UTC and EF migrations are the only schema changes.
- Do not invent Minecraft statistics, plugin endpoints, ownership, reward execution or payment results.
- Keep relations and published revision snapshots immutable after publication.
- No microservices, broker, GraphQL, generic repository or production deployment.

## Review Focus

- A draft or unpublished relation must never appear in anonymous list/detail responses.
- Publishing must reject links to missing, draft or unpublished targets without partial state.
- Re-publishing after an edit must create a new immutable graph snapshot and preserve the old revision.
- Duplicate links, cycles that are not meaningful for the selected relation type and invalid coordinate/date ranges must return validation errors.
- A stale admin ETag must not overwrite a newer edit or append a partial audit record.

### Task 1: Game Knowledge Graph domain and persistence

**Files:**
- Create: `backend/NovaHaven.Domain/Knowledge/*.cs`
- Create: `backend/NovaHaven.Application/Knowledge/*.cs`
- Modify: `backend/NovaHaven.Infrastructure/Data/NovaDbContext.cs`
- Create: `tests/backend/NovaHaven.Domain.Tests/KnowledgeTests.cs`
- Create: `tests/backend/NovaHaven.Integration.Tests/KnowledgeApiIntegrationTests.cs`

**Interfaces:**
- Consumes: existing Catalog item/revision entities and audit writer.
- Produces: lifecycle aggregates, typed metadata, draft links, immutable revision links and validators for the API task.

- [x] Write failing domain tests for kind-specific validation, date/coordinate rules, unique links and publication snapshots.
- [x] Run focused domain tests and confirm they fail for missing types/behavior.
- [x] Implement the minimal entities, validators and EF mappings.
- [x] Generate/review/apply `AddKnowledgeGraph` migration to LocalDB.
- [x] Run domain tests, migration drift check and integration fixture setup.

### Task 2: Knowledge REST API and OpenAPI

**Files:**
- Create: `backend/NovaHaven.Api/Endpoints/KnowledgeEndpoints.cs`
- Modify: `backend/NovaHaven.Api/Program.cs`
- Modify: `contracts/openapi/wiki-v1.json`
- Extend: `tests/backend/NovaHaven.Integration.Tests/KnowledgeApiIntegrationTests.cs`
- Extend: `tests/web/contract-guards.test.mjs`

**Interfaces:**
- Consumes: Task 1 aggregates/validators and existing Admin auth/CSRF/ETag helpers.
- Produces: anonymous published-only list/detail and Admin CRUD/publish/unpublish endpoints.

- [x] Add failing integration tests for anonymous draft privacy, relation validation, publish snapshots, ETag conflicts and unpublish 404.
- [x] Run tests to observe expected failures.
- [x] Implement endpoint DTOs, Problem Details, transactions, audit events and published-only projections.
- [x] Update OpenAPI schemas and contract guards.
- [x] Run focused integration plus complete backend/Node suites.

### Task 3: Web and Flutter Knowledge clients

**Files:**
- Create: `apps/web/src/lib/knowledge-api.ts`
- Create: `apps/web/app/knowledge/**`
- Create: `apps/web/app/admin/KnowledgeManager.tsx`
- Modify: `apps/web/app/layout.tsx`, `apps/web/app/admin/CategoryManager.tsx`, `apps/web/app/styles.css`
- Modify: `apps/mobile/lib/wiki_api.dart`, `apps/mobile/lib/main.dart`
- Extend: `apps/mobile/test/wiki_api_test.dart`

**Interfaces:**
- Consumes: Task 2 public/admin JSON contract.
- Produces: public Knowledge directory/detail pages, Admin editor and Flutter published reader.

- [x] Add failing Node and Flutter client tests for typed decoding, relation rendering and draft exclusion.
- [x] Run focused tests to confirm missing client behavior.
- [x] Implement clients using existing fetch/error/Markdown conventions.
- [x] Run Node tests, web typecheck/build and Flutter test/analyze/build.

### Task 4: Community graph slice

**Files:**
- Create: `openspec/changes/add-community-systems/specs/community/spec.md`, `tasks.md`
- Create: `backend/NovaHaven.Domain/Community/*.cs`, `backend/NovaHaven.Application/Community/*.cs`, `backend/NovaHaven.Api/Endpoints/CommunityEndpoints.cs`
- Modify: `backend/NovaHaven.Infrastructure/Data/NovaDbContext.cs`, OpenAPI and web/mobile clients
- Create: `AddCommunitySystems` migration and domain/integration tests

**Interfaces:**
- Consumes: Knowledge graph publication and relation model.
- Produces: Events, Guilds, Housing showcase, Player editorial profiles and web-event standings with external data explicitly absent.

- [x] Write OpenSpec and failing lifecycle/relation tests.
- [x] Implement minimal relational records, validation and endpoints.
- [x] Implement public/admin web screens and suitable Flutter readers.
- [x] Apply migration and run all gates.

### Task 5: Integration capability foundation

**Files:**
- Create: `backend/NovaHaven.Domain/Integration/*.cs`, `backend/NovaHaven.Application/Integration/*.cs`
- Create: `backend/NovaHaven.Api/Endpoints/IntegrationEndpoints.cs`
- Modify: `NovaDbContext`, OpenAPI, Admin UI and roadmap docs
- Create: migration and unit/integration tests

**Interfaces:**
- Produces: capability/status contract only; no gameplay synchronization, reward execution or payment processing.

- [x] Write failing tests for status transitions, stale sync and safe error redaction.
- [x] Implement contract/status persistence and Admin status editor endpoint.
- [x] Add explicit transition policy for missing adapters.
- [x] Run all tests and verify no external writes occurred.

### Task 6: Final local acceptance

**Files:**
- Modify: `README.md`, `docs/roadmap/48H-ALL-SYSTEMS-STATUS.md`, `docs/verification/*`
- Create/extend: local E2E scripts and backup/restore rehearsal notes

- [x] Run all Node, .NET, EF, web and Flutter gates freshly.
- [x] Run local API smoke and document browser/emulator/manual gates that remain unverified.
- [x] Update module statuses independently as Implemented, Verified and Accepted.
- [x] Produce the ALL SYSTEMS report with exact evidence and remaining external blockers.

### Extension: Rewards and Commerce definition-only catalogs

- [x] Add OpenSpec, domain aggregates, validators, migrations and immutable revisions.
- [x] Add Admin/public REST APIs, OpenAPI, Next.js Admin/public pages and Flutter readers.
- [x] Verify LocalDB publish/public privacy and explicitly keep grant/checkout out of the API.

## Execution order

1. Task 1 before Task 2; never create a migration before the model is tested.
2. Task 2 before Task 3 so clients use the actual contract.
3. Task 4 only after the graph is stable; it must reuse publication/link rules.
4. Task 5 is definition-only until real external contracts exist.
5. Task 6 is last and cannot mark browser/emulator/production gates as accepted without evidence.
