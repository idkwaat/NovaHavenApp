# Category lifecycle verification — 2026-09-18

**Scope:** Admin-only category create/read/edit/reorder/deactivate/delete, name uniqueness, `rowversion` and `If-Match`, draft/revision reference protections, category UI and an extended smoke script. Work continues on `feature/wiki-foundation-recovery`.

| Gate | Evidence | Status |
| --- | --- | --- |
| TDD RED for missing helper/category API | `npm test` initially failed with `ERR_MODULE_NOT_FOUND`; after initial code, route test failed because category PATCH/DELETE missing. | Observed |
| TDD RED for Admin category UI | `npm test` failed with `ENOENT .../CategoryManager.tsx` before its implementation. | Observed |
| TDD RED for FK exception handling | `npm test` failed on unfiltered `DbUpdateException` catch before a focused SQL 547 filter was added. | Observed |
| Node tests | `npm test` after code changes | 15 passed / 0 failed |
| TypeScript category helper | `tsc --strict --noEmit --target es2022 --skipLibCheck apps/web/src/lib/wiki-category.ts` | Exit 0 |
| Admin TSX syntax | TypeScript parser `createSourceFile` checked `page.tsx` and `CategoryManager.tsx`. | 0 parse diagnostics |
| Smoke script syntax | `node --check scripts/wiki-smoke.mjs` | Exit 0; NOT a live HTTP test |
| C# compilation / xUnit | .NET SDK not available here. Policy xUnit tests have been written but not executed. | BLOCKED |
| SQL Server and EF migration | No .NET SDK/Docker or SQL Server configured. `InitialWiki` migration/snapshot must be generated and reviewed on a machine with the SDK. | BLOCKED |
| Next.js production build / Flutter APK | Dependencies and SDK are not installed in this authoring environment. | BLOCKED |

**Risks to verify before release:** SQL Server concurrency between publishing a revision and deactivating its category; unique key collations, update/delete FK races; Admin CSRF authentication and browser behavior. No full-build or deployment claim is made from these source-level results.
