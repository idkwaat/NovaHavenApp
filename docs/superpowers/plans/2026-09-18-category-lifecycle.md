# Wiki category lifecycle implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add Admin update, activate/deactivate, and delete operations for Wiki categories without damaging published or historical article classification.

**Architecture:** Reuse the existing Admin endpoint group with its role and CSRF filters, and SQL Server EF Core context. A small pure Application policy evaluates reference protections; the API checks those references and persists mutations, while Next Admin exposes the actions. Add rowversion/If-Match to reject stale category writes; do not modify public Wiki API semantics.

**Tech Stack:** .NET 10 / EF Core SQL Server / Next.js TypeScript / Node 22 native tests.

**Spec:** `openspec/changes/add-wiki-cms/specs/wiki-content/spec.md`; `docs/superpowers/specs/2026-09-17-nova-haven-platform-design.md`.

## Global Constraints

- Admin-only and CSRF for every category write; `If-Match` for patch/delete.
- No deletion of a category referenced by any draft or revision; no category rename/slug change when referenced by a revision.
- No deactivation while referenced by a draft or revision (conservative stronger rule until relational concurrency is verified).
- SQL Server unique key for normalized category names and slugs; return 409 on uniqueness conflicts.
- Existing public article revision reads must remain unchanged; any newly introduced migration requires .NET SDK generation and review, not handwritten fake EF snapshots.
- SDK is unavailable here: C# behavior tests must be provided, marked unexecuted, and no build or DB claims made.

---

### Task 1: Define contract and failing Node regression tests

**Files:** `contracts/openapi/wiki-v1.json`, `tests/web/category-lifecycle.test.mjs`, `apps/web/src/lib/wiki-category.ts`.

**Interfaces:** `validateCategory(input: CategoryInput): Record<string,string>`; OpenAPI `PATCH/DELETE /api/v1/admin/wiki/categories/{id}` require Admin cookie + `If-Match`.

- [x] Write native tests for missing category endpoints, security/preconditions, and category validation (empty name, invalid slug, order bounds); run `npm test` and observe expected failure.
- [x] Add contract operations and focused TypeScript category validation helper; run `npm test` to green.

### Task 2: Backend lifecycle and reference protections

**Files:** `backend/NovaHaven.Application/Wiki/WikiCategoryPolicy.cs`, `backend/NovaHaven.Application/Wiki/WikiDraftValidator.cs`, `backend/NovaHaven.Domain/Wiki/WikiCategory.cs`, `backend/NovaHaven.Infrastructure/Data/NovaDbContext.cs`, `backend/NovaHaven.Api/Endpoints/AdminWikiEndpoints.cs`, `tests/backend/NovaHaven.Domain.Tests/CategoryPolicyTests.cs`.

**Interfaces:** `WikiCategoryUpdateInput(Name,Slug,DisplayOrder,IsActive)`; `WikiCategoryPolicy.CheckUpdate(category,input,articleRefs,revisionRefs)` and `CheckDelete(articleRefs,revisionRefs)` return null or 409 reason; category ETag is base64 rowversion.

- [x] Write C# policy tests first for referenced revision rename/deactivation, referenced draft delete/deactivation, allowed safe reorder and unreferenced delete; document test execution blocked by missing SDK.
- [x] Implement pure policy, normalized name index, category rowversion, Admin GET detail, PATCH and DELETE with If-Match, FK race conflict handling and validation; reuse group Admin/CSRF protection.
- [x] Add backend source-contract test to verify route declarations and policy wiring structurally; never equate this with compiling C#.

### Task 3: Admin workflow, verification, and documentation

**Files:** `apps/web/app/admin/page.tsx`, `apps/web/app/styles.css`, `docs/roadmap/NOVA-HAVEN-ROADMAP.md`, `README.md`, `openspec/changes/add-wiki-cms/tasks.md`, `docs/verification/2026-09-18-category-lifecycle.md`.

- [x] Add category edit form for name/slug/order/active status, plus delete with confirmation, disabling actions without ETag; prevent inactive category selection on new drafts.
- [x] Run `npm test`, `node --check scripts/wiki-smoke.mjs`, JSON/XML parse, `git diff --check`; record actual test results and absent SDK gate.
- [ ] Update roadmap/status and commit on existing feature branch; preserve all source, do not send archive until owner requests it.
