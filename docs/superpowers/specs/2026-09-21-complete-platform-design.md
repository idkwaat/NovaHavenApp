# Nova Haven Platform — completion design

**Date:** 2026-09-21
**Status:** Approved in chat by the owner for implementation.
**Scope:** Complete the remaining local, code-deliverable MVP slices from the recovered checkpoint while preserving the existing modular monolith and OpenSpec boundaries.

## Outcome

Nova Haven shall be runnable locally as a Wiki-first Minecraft RPG portal with an Admin CMS, a published-only web/mobile reader, local SQL Server persistence, private media delivery, revision/audit visibility, News/changelog content, and optional authenticated bookmarks. The Minecraft server remains outside the system of record and no plugin database is accessed.

## Constraints

- SQL Server is local/disposable only for development and verification. No production database mutation is part of this implementation.
- ASP.NET Core remains the only API; Next.js remains the web/admin client; Flutter remains the mobile client.
- Public reads are anonymous and published-only. Admin mutations enforce role authorization, CSRF for cookie mutations, safe Problem Details and ETag/If-Match where concurrent edits matter.
- UUIDs are JSON strings, timestamps are UTC, and EF migrations are the only schema changes.
- Media is stored under a server-configured private local root with random storage keys; public delivery checks current published revision references.
- Markdown is rendered as inert/sanitized content. Raw HTML, script, event-handler and unsafe URL execution is forbidden.
- News and bookmarks use separate OpenSpec changes before their implementation details are treated as shipped behavior.
- No microservices, broker, GraphQL, generic repository layer, direct Minecraft SQL writes or speculative game logic.

## Delivery slices

### Slice 1 — Core acceptance and contract hardening

Keep the existing article/category/tag model and complete the missing relational proof: non-admin authorization, stale rowversion behavior, transaction rollback, repeated concurrency races, Problem Details, and OpenAPI schemas/examples for every current endpoint. Add a local demo seed path that is explicit and development-only.

### Slice 2 — Private media and safe rendering

Add `WikiMedia` plus draft/revision reference joins only if required by the existing model contract. The API accepts PNG/JPEG/WebP only after size and content-signature validation, writes to a private local storage root with a random key, exposes authenticated preview, and exposes anonymous bytes only when a current published revision references the media. Publication validates references atomically. Web and mobile clients accept only approved media URLs and inert Markdown.

### Slice 3 — Revision and audit experience

Expose the existing revision history and restore endpoints in the Admin UI with clear current-draft/public labels, ETag reload handling and no direct public mutation. Add an append-only audit record for privileged editorial actions, storing actor, action, target, timestamp, outcome and safe metadata only.

### Slice 4 — News/changelog

Create a separate OpenSpec change for News. News follows the same draft → immutable published revision → unpublish model, public published-only listing/detail, Admin CRUD and web/mobile read contract. It must not reuse article tables through ambiguous polymorphism; use focused entities/mappings and a reviewed migration.

### Slice 5 — Bookmarks

Create a separate OpenSpec change for authenticated bookmarks. The API owns identity and authorization; Flutter stores only short-lived/rotated credentials through platform secure storage. Bookmarks reference published article IDs, return 404/401 safely as appropriate, and never expose drafts. Web Admin does not gain player bookmark controls.

### Slice 6 — Build, E2E and delivery proof

Add browser E2E for the editorial lifecycle against LocalDB, Android scaffold/build when the SDK permits it, repeat all platform checks, update OpenAPI/README/roadmap/verification evidence, and document what remains infrastructure-dependent. Production deployment is not claimed without real staging infrastructure, secrets, backup/restore and owner approval.

## Data flow and boundaries

Admin UI → same-origin Next.js `/api` proxy → ASP.NET Core endpoint/policy/CSRF → application/domain validation → EF Core transaction → local SQL Server and private media root. Public web and Flutter call only versioned published-read endpoints. News/bookmarks add endpoints only after their own OpenSpec changes and use the same error/auth conventions.

## Testing strategy

- Domain/application tests for validation, state transitions, snapshot isolation and reference policies.
- SQL Server LocalDB integration tests for migration, unique constraints, rowversion/ETag, transaction rollback, authorization, published-only queries and concurrent edits.
- Node contract tests for OpenAPI and smoke-script invariants.
- Next.js typecheck/build plus browser E2E for Admin/public flows and safe Markdown/media behavior.
- Flutter pub get/test/analyze and Android build when the local SDK/emulator is available.
- Every production-code change follows a failing test, minimal fix, passing focused test and full-suite verification.

## Acceptance boundary

Completion means all code that can be verified in this local workspace is implemented and tested. It does not mean production deployment, domain/TLS, real cloud storage, staging backups or Minecraft integration are complete.
