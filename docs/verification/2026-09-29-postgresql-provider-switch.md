# PostgreSQL provider switch — local verification

**Date:** 2026-09-29
**Scope:** Replace the active Nova Haven database provider with local PostgreSQL/Npgsql after SQL Server LocalDB failed to initialize. This report does not claim real PostgreSQL integration acceptance.

## Implemented

- API, design-time factory and EF test configuration now use Npgsql; no SQLite or second runtime provider was added.
- Domain concurrency versions use `uint`, mapped by Npgsql to PostgreSQL `xmin`; HTTP ETag/If-Match stays base64 with the existing precondition statuses.
- Generated `InitialPostgreSql` plus its model snapshot under `backend/NovaHaven.Infrastructure/Data/PostgreSqlMigrations/`. Historical SQL Server migration files remain on disk but are excluded from compilation. The previous SQL Server database was not migrated, copied or changed.
- Added a disposable PostgreSQL integration fixture driven by `NOVA_HAVEN_TEST_ADMIN_CONNECTION`. It targets `postgres`, creates a GUID-named database per fixture, and cleanup refuses names outside `NovaHaven_Integration_<32 lowercase hex>`.
- Centralized recognized PostgreSQL unique/FK/deadlock/serialization handling. Corrected Commerce check-constraint identifiers for PostgreSQL.
- Replaced the Auth/Audit/Operations Minimal API maps with controller → Application service → Infrastructure read/repository adapters; removed the now-unmapped duplicate endpoint files.

## Verified in this checkout

- `dotnet build backend/NovaHaven.Api/NovaHaven.Api.csproj --no-restore -c PostgreSql` — passed, 0 warnings and 0 errors. The named configuration uses a separate output directory because the ordinary Debug API output is locked by an already-running local process.
- `dotnet test tests/backend/NovaHaven.Domain.Tests/NovaHaven.Domain.Tests.csproj --no-restore -c PostgreSql` — 78/78 passed.
- `dotnet test tests/backend/NovaHaven.Integration.Tests/NovaHaven.Integration.Tests.csproj --no-restore -c PostgreSql --filter "FullyQualifiedName~PlatformControllerArchitectureTests|FullyQualifiedName~RowVersionEtagTests|FullyQualifiedName~EfConfigurationDiscoveryTests"` — 10/10 passed. These tests do not connect to a database; they cover routes/layers, provider/model discovery, active migration selection and ETag/xmin mapping.
- `npm test` — 161 passed, 5 skipped, 0 failed.
- `dotnet-ef migrations has-pending-model-changes` — reported no changes after the PostgreSQL baseline was generated.
- Generated the migration SQL to ignored `.local/postgresql-initial.sql` for review; it contains the PostgreSQL baseline schema and no SQL Server bracket-quoted check expressions. SQL generation does not apply the migration.

## Not verified / blocked

The above baseline run had no credentials for the pre-existing PostgreSQL service, so it did not touch that instance. A follow-up verification used a separate temporary PostgreSQL 18.4 data cluster under the ignored `.local/postgresql-test-cluster`, bound only to `127.0.0.1:15435`, with local test-only trust authentication. It was distinct from the installed user's PostgreSQL service and the existing SQL Server database. `LocalApiFactory` now creates its exact `NovaHaven_Integration_<32 lowercase hex>` database before applying migrations, overrides EF options to that database, checks the context target before both migration and cleanup, and refuses any other target. After the full suite, a direct query confirmed **zero** leftover Integration databases. No existing user content database was migrated, read for acceptance, or modified.

For a repeat run, provide `NOVA_HAVEN_TEST_ADMIN_CONNECTION` with a local PostgreSQL role that can create databases and target `postgres`, then run `dotnet test tests/backend/NovaHaven.Integration.Tests/NovaHaven.Integration.Tests.csproj --no-restore -c PostgreSql`. The fixture uses a unique database per test host and drops it on cleanup; never point this variable at a content database. The test host clears Windows Event Log providers so permission errors cannot mask database failures.

Current exact verification after the News Controller → BLL → repository slice and fixture hardening:

- `dotnet build NovaHaven.sln --no-restore -c Debug -p:BaseOutputPath=.local/solution-build/` — passed, 0 warnings and 0 errors. The isolated output avoids the running preview's locked Debug artifacts.
- `dotnet test tests/backend/NovaHaven.Domain.Tests/NovaHaven.Domain.Tests.csproj --no-restore -c PostgreSql` — **80/80 passed**.
- `dotnet test tests/backend/NovaHaven.Integration.Tests/NovaHaven.Integration.Tests.csproj --no-restore -c PostgreSql` — **70/70 passed** on the isolated PostgreSQL cluster.
- `npm test` — **161 passed, 5 skipped, 0 failed**.
- `dotnet ef migrations has-pending-model-changes ... --configuration PostgreSql --no-build` — no model changes since the baseline.

Still not verified here: the user's ordinary local PostgreSQL connection/Admin bootstrap, external Web Push delivery, current Next production build, current Flutter test/analyze/APK, authenticated browser/mobile parity, deployment and production operations. The earlier LocalDB results remain historical SQL Server evidence only.

The earlier LocalDB results remain historical SQL Server evidence only. The API convention cleanup is partial: Auth, Audit and Operations were moved to Controller/BLL/DAL; other feature APIs still include Minimal API maps. Web production build, Flutter test/analyze and full browser/device acceptance were not rerun in this provider-switch batch.

## Latest architecture and quality continuation (2026-09-29)

The preceding counts describe the earlier provider-switch checkpoint. Afterward, Catalog items, Recipes and Rewards were converted to Controller → BLL → feature repository → Infrastructure DAL. Existing public/Admin routes and OpenAPI operations were kept. Rewards public reads now source the immutable published revision rather than editable draft columns; a PostgreSQL integration test confirms that editing a published reward draft does not alter public detail/search. Unpublish returns the refreshed ETag so the Admin lifecycle can continue.

Latest verification:

- `dotnet test NovaHaven.sln --no-restore -c Debug -p:BaseOutputPath=.local/solution-build/` — Domain **80/80** and PostgreSQL Integration **72/72** passed. The temporary PostgreSQL 18.4 cluster was bound to loopback port 15435; fixture cleanup left **0** `NovaHaven_Integration_*` databases. The cluster was stopped after testing. No user content database was targeted.
- Reward-specific integration after the final ETag response change — **1/1 passed**.
- After separating Recipe HTTP component DTOs from Application commands, focused Catalog/Rewards route-boundary and API-contract-layer architecture tests — **3/3 passed**.
- EF Core `migrations has-pending-model-changes` — no model changes.
- `npm test` — **161 passed, 5 skipped, 0 failed**.
- `npm run typecheck` — passed; isolated Next production build with `NOVA_NEXT_DIST_DIR=.next-review` — exit code **0**, 20/20 pages generated. Generated `.next-review` entries were removed from `tsconfig.json`; the existing dev output was not reused or stopped.
- Read-only runtime GET smoke against the already-running local preview: API `/health`, website `/`, `/catalog`, `/community`, and public Catalog items/recipes/Rewards endpoints all returned HTTP **200**. This checks the existing preview only; it does not prove that preview process was rebuilt from the latest source.
- Flutter SDK is not available on this host, so Flutter test/analyze/APK build were not run.

Integration migrations/tests did not target the user's configured content database. The only access through the existing preview's database path was the read-only GET smoke above; no seed, migration or write was performed there.

Still outstanding: Knowledge, Community, Integration, Commerce/orders and Notifications Minimal API conversions; the user's ordinary local PostgreSQL startup/bootstrap; authenticated browser/mobile acceptance, external push delivery and deployment. No commit or deployment was made.
