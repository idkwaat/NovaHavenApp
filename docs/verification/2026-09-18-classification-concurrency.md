# Classification transactions — evidence and remaining gates

**Date:** 2026-09-18. **Status:** source implemented; real SQL Server correctness UNVERIFIED.

## Recovery and root cause

The recovered internal checkpoint had EF FK/rowversion checks, but category PATCH/DELETE and article create/edit/restore were not wrapped in a serializable read-check-write transaction; publication used provider-default isolation. The checkpoint README's earlier claim that these operations were already serializable was incorrect. Only the ZIP-backed source was treated as authoritative.

## Implementation scope

- `backend/NovaHaven.Api/Endpoints/AdminWikiEndpoints.cs`: category PATCH/DELETE and article POST/PATCH/publish/restore begin `IsolationLevel.Serializable` transactions before reading classification and commit after successful write. Early returns or failed `SaveChangesAsync` dispose uncommitted transactions.
- `backend/NovaHaven.Api/Infrastructure/SqlServerConflict.cs`: walks exception causes and recognizes only SQL Server deadlock 1205. Admin mutation filter maps it to HTTP 409 with an explicit reload/retry message. No blind server-side retry of non-idempotent writes.
- Article PATCH/restore respond with a fresh ETag header only after commit. Existing missing/stale ETag handling remains 428/412 and EF FK/unique constraint violations retain their specific 409 behavior.
- `scripts/wiki-smoke.mjs`: races an article create and category deactivate against a new disposable category and checks they cannot both succeed. `contracts/openapi/wiki-v1.json` documents possible 409 for all six transactional mutations.

## Tests executed here

- Wrote failing `tests/web/classification-transaction.test.mjs` before the implementation; the initial run was 15 pass / 8 fail. Additional ETag and OpenAPI regression tests also failed before their fixes.
- `npm test`: 25 pass / 0 fail after changes (Node structural, helper and contract checks; does not execute C#).
- `node --check scripts/wiki-smoke.mjs`: exit 0 (JavaScript syntax only).
- `tsc --strict --noEmit --target es2022 --skipLibCheck apps/web/src/lib/wiki-category.ts apps/web/src/lib/wiki-model.ts`: exit 0 (these two standalone helpers only).

## Not executed / release blockers

- No .NET SDK, Flutter SDK or Docker installed in this authoring container. No EF migration, C# build, C# xUnit execution or SQL Server integration test ran.
- On a disposable SQL Server 2022, generate and review `InitialWiki`, run `dotnet test`, then run `npm run smoke:wiki` with local test Admin credentials. The race checks need repeated runs and a separate two-editor ETag test before accepting transaction behavior. Confirm persisted state and revision FK links after each race.
- SQL exception handling and deadlock behavior are source-reviewed only; re-check compiler diagnostics, actual EF transaction semantics, SQL Server constraint behavior and test failure rollback during the real build/integration stage.

**Acceptance decision:** M1 and M2 remain SOURCE/BLOCKED; do not mark VERIFIED or deploy.
