# Local shop and simulated checkout — implementation evidence

Date: 2026-09-29  
Scope: source implementation and local build/test checks only. No online service, external payment provider, Minecraft/plugin database, or existing Nova Haven database was used or changed.

## Implemented in source

- Opt-in `isPurchasable` and authoritative integer VND price on commerce drafts and immutable published revisions. Existing offers stay non-purchasable by default.
- Additive `AddLocalDemoCommerce` EF migration with order, line and simulated-payment snapshots, idempotency and reference indexes, and restrictive history FKs.
- Anonymous `POST /api/v1/commerce/orders`: accepts only offer slugs/quantities, re-reads published revisions, ignores client price fields, computes checked totals, and writes order + lines + one `LocalDemo` payment in a SQL Server `Serializable` transaction. UUID idempotency key returns the same receipt on retry and conflicts if reused for a different cart.
- Admin-only paged, read-only `GET /api/v1/admin/commerce/orders`. No public order lookup and no buyer contact/card/bank fields.
- Next.js `/commerce`, `/commerce/[slug]`, `/cart`, `/checkout`; browser storage keeps only slugs and quantities. The receipt explicitly says `realCharge: false` and `fulfilment: none`. Admin can price/enable offers and review simulated orders.
- Updated the v1 OpenAPI contract, local smoke behavior and local setup guidance.

Every screen and API response marks this as a local simulation. There is no payment gateway, real-money collection, refund/webhook, email receipt, reward grant or in-game delivery. The generic editorial seed deliberately still creates no store products; configure a clearly local demo offer in Admin on a disposable local database to populate the shop.

## Verified

| Check | Result |
| --- | --- |
| `npm test` | PASS — 126 passed, 4 skipped, 130 total; includes cart helpers, OpenAPI/UI contract, published-slug regression and checkout error focus |
| `node --experimental-strip-types --test tests/web/commerce-cart.test.mjs` | PASS — 3/3 |
| `apps/web npm run typecheck` | PASS — rerun after the checkout accessibility fix |
| `NOVA_NEXT_DIST_DIR=.next-commerce-review npx next build` | PASS — optimized production build generated `/commerce`, `/commerce/[slug]`, `/cart`, and `/checkout` after the final UI changes |
| `dotnet build backend/NovaHaven.Api/NovaHaven.Api.csproj -c LocalCommerce --no-restore` | PASS — 0 warnings, 0 errors after published-slug fix |
| Domain tests | PASS — 49/49 |
| `dotnet build tests/backend/NovaHaven.Integration.Tests/NovaHaven.Integration.Tests.csproj -c LocalCommerce --no-restore` | PASS — integration tests, including two published-slug cases, compile with 0 warnings/errors |
| `dotnet ef migrations has-pending-model-changes ... --configuration LocalCommerce --no-build` | PASS — no changes pending after the new migration |
| OpenAPI JSON and schema references | PASS — all schema references resolve |

## Blocked / not verified

- Seven focused API/SQL integration tests compiled and were attempted, but all stopped in `LocalApiFactory.InitializeAsync` before test bodies: SQL Server LocalDB error 50, “Cannot create an automatic instance.” The LocalDB registry/runtime is unavailable to this execution identity. This does not establish that the migration, published-slug behavior, or transactional behavior passed.
- The migration was generated and reviewed but **not applied**. No API checkout request, database persistence/transaction, browser purchase journey, or SQL Server runtime behavior was verified in this environment.
- `NovaHaven_Local` and the running preview API were left untouched. The preview still needs the updated API process and the new migration on a disposable local database before its checkout flow can be exercised.
- Review found and source-fixed a published-versus-draft slug mismatch in public detail/checkout; a competing active published slug is now rejected on publish. A keyboard-focus target was also added to checkout errors. Static regression checks pass; SQL-backed scenarios compile but remain runtime-blocked above.
- No Admin-authenticated browser walkthrough, real payment, game fulfilment, mobile checkout or deployment is claimed.

Run the focused runtime check after starting LocalDB under its owning Windows account and targeting a fresh disposable database: `dotnet test tests/backend/NovaHaven.Integration.Tests/NovaHaven.Integration.Tests.csproj -c LocalCommerce --no-restore --filter FullyQualifiedName~LocalDemoCommerceApiIntegrationTests`. The EF pending-model check used `NOVA_DB_CONNECTION` pointed at a design-only local connection string and `--no-build`; it did not open/apply a database.
