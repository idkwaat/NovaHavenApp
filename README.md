# Nova Haven Platform — local all-systems source (NOT deployment-ready)

Wiki-first Minecraft RPG community portal + private Admin CMS + published-only Flutter reader. Discord remains the community hub. The game plugins retain all gameplay authority; this project has no access to their databases.

**Cài đặt local và nạp dữ liệu demo:** xem [Hướng dẫn cài đặt tiếng Việt](docs/INSTALLATION-VI.md). Active provider is PostgreSQL for this local project; no populated database or credentials are included.

## Source and verification status

This source tree was recovered from the approved `openspec/` specification and then extended in local feature slices. Historical commit IDs and test claims from unavailable source checkouts are not evidence for this repository; use the date-stamped verification reports and roadmap for current status. This remains a local MVP, not a production-ready deployment.

## Current source

- `backend/NovaHaven.Domain` — Wiki entities, immutable article revision snapshots and draft/revision tag joins.
- `backend/NovaHaven.Application` — feature BLL/services, commands/results, validation, transaction and repository ports.
- `backend/NovaHaven.Infrastructure` — EF Core/Identity persistence, PostgreSQL provider and feature repositories/DAL. Historical SQL Server migrations remain archived and excluded from compilation.
- `backend/NovaHaven.Api` — REST controllers/contracts, cookie/CSRF/admin security, Problem Details and ETag handling; `Program.cs` keeps only infrastructure health routing outside MVC controllers.
- `apps/web` — Next.js landing, public Wiki/News/Catalog/Recipes/Knowledge/Community/Rewards/Commerce, browser cart, simulated checkout and Admin editors/order history.
- `apps/mobile` — Flutter reader for published Wiki/News/Catalog/Knowledge/Community content, plus player account and notification inbox screens.
- `contracts/openapi/wiki-v1.json` — maintained v1 contract for the implemented local API slices, including Knowledge, Community, Integration, Rewards and Commerce.
- `tests/web` — native Node executable helper/client/contract tests.

The local completion batch includes media upload/gateway, audit, News, bookmarks, published related links/TOC, typed Catalog items/recipes, Knowledge Graph, Community systems, Integration capability status, Rewards definitions, simulated local Commerce checkout, player accounts and notifications/Web Push. All business API slices use Controller → Application BLL → feature repository → Infrastructure DAL; the OpenAPI contract matches all 106 controller method/path pairs. No payment gateway, real charge or game-benefit delivery is connected. Current build/test evidence and remaining acceptance limits are recorded in [`docs/verification/2026-09-30-api-mobile-completion.md`](docs/verification/2026-09-30-api-mobile-completion.md). This remains a local MVP, not production-ready.

Category lifecycle source includes create/detail/edit/reorder/deactivate/delete with draft/revision reference checks, normalized-name uniqueness and an ETag concurrency token. The older SQL Server migration chain is historical only. PostgreSQL migration source is maintained separately; do not run historical SQL Server migrations or use `EnsureCreated`. The app does not apply migrations on startup. Category PATCH/DELETE require Admin authorization, CSRF and `If-Match`.

## Toolchain

Requires .NET SDK 10, `dotnet-ef` 10.0.12, PostgreSQL (local install or Docker Compose), Node >=22/npm and Flutter SDK. EF Core/Identity references are pinned to 10.0.12 with the Npgsql EF Core provider. Historical dated reports may describe the previous SQL Server setup; they are not current PostgreSQL verification.

## Local setup

Làm theo [hướng dẫn cài đặt tiếng Việt](docs/INSTALLATION-VI.md) để tạo PostgreSQL local, áp dụng PostgreSQL migrations, bật Admin Development, nạp dữ liệu demo và chạy Next.js/Flutter. Database SQL Server cũ không bị sửa hay tự chuyển đổi.

## Test gates

```bash
npm test
dotnet test tests/backend/NovaHaven.Domain.Tests/NovaHaven.Domain.Tests.csproj
cd apps/web && npm install && npm run typecheck && npm run build
cd apps/mobile && flutter pub get && flutter test && flutter analyze
```

The 58/58 SQL Server integration result in [`docs/verification/2026-09-29-local-platform-hardening.md`](docs/verification/2026-09-29-local-platform-hardening.md) is historical and does not apply to the PostgreSQL provider. Current evidence: solution build 0 warnings/errors; Domain 80/80; PostgreSQL Integration 78/78 on a disposable loopback PostgreSQL 18.4 cluster; EF reports no pending model changes; Node 161 passed/5 skipped; Next typecheck and isolated production build passed (20/20 routes); Flutter 26/26 and analyze clean. The integration fixtures applied migrations only to GUID-named disposable databases. No existing local content database was migrated or modified. External push delivery, authenticated browser/device acceptance, deployment and production operations are not claimed.

For an evidence-based defense package, see [`docs/defense/DEMO-GUIDE.md`](docs/defense/DEMO-GUIDE.md), [`docs/defense/ERD.md`](docs/defense/ERD.md) and [`docs/defense/READINESS-CHECKLIST.md`](docs/defense/READINESS-CHECKLIST.md). Mutation-oriented rehearsals should use a disposable local database, not an existing local database whose content must be preserved.

The automated tests do not replace manual browser/device acceptance. Full authenticated parity still needs an end-to-end run: Admin login → create draft invisible to public → publish → web/mobile see the same revision → edit draft retains the live revision → republish → restore to draft → unpublish returns 404. Existing preview evidence predates the provider switch and is not proof of current user-database state.
The Wiki smoke script includes category and tag ETag checks, delete of unused entries, refusal to mutate referenced classifications, published tag filtering and historical tag isolation. The all-systems smoke checks public reads, Admin order-history authorization, and submits only an empty cart that must be rejected without persisting an order. Run these against a disposable local PostgreSQL database after the baseline migration has been generated and applied.

The legacy `scripts/localdb-backup-restore.ps1` applies only to a historical SQL Server installation; it does not back up PostgreSQL and must not be used for the active local database.

## Local Minecraft world map

The public `/map` page prefers the local BlueMap viewer when it is running, configured for a fixed flat/top-down camera with detailed 3D block models and no live player tracking. The existing source-derived atlas remains the no-server fallback. The supplied save reports `DataVersion=3955` and spawn X=191, Z=-71; the local workflow pins BlueMap CLI 5.16 because that release supports Java 21 and this world format. BlueMap binaries, config, rendered tiles, and content-addressed world snapshots stay under the ignored `.local/bluemap/` directory. BlueMap reads a private snapshot containing only `level.dat` and overworld region files; it never receives the source folder and playerdata is excluded. Repeating setup/start with an unchanged ZIP reuses the same snapshot after validating its SHA-256 manifest. Optional metrics are disabled. No Minecraft server or remote database is needed. Redistribution rights for the world have not been verified, so keep this preview local.

Install Java 21 and run the BlueMap viewer from the repository root, passing either an extracted Java world directory or the original ZIP. Setup and start may use the same input path; source data is copied to a checksum-addressed local snapshot:

```powershell
python scripts/bluemap_local.py setup --world "C:\path\to\world"
# Chỉ chạy render sau khi đã xem điều khoản Mojang và đồng ý:
python scripts/bluemap_local.py start --world "C:\path\to\world" --accept-mojang-downloads
```

`start` renders and serves the map on `http://127.0.0.1:8100`; leave that terminal open and run the Next.js site in another terminal. Open `/map` on the web app (normally `http://localhost:3000/map`). Press Ctrl+C in the BlueMap terminal to stop the local viewer. To render again after editing the save, rerun `start`; BlueMap updates tiles from the on-disk world. BlueMap requires downloading Minecraft rendering resources from Mojang on its first render and its `accept-download` setting represents acceptance of Mojang's terms. The runner leaves this off; review the link in `.local/bluemap/config/core.conf`, then add `--accept-mojang-downloads` to `render` or `start` only if you agree. The viewer is bound to loopback only and is not available to other machines or an online deployment.

To keep a render running without rebuilding tiles, use `python scripts/bluemap_local.py serve --world "C:\path\to\world"`. `python scripts/bluemap_local.py status` checks the default local viewer port. The setup downloads only the pinned official `bluemap-5.16-cli.jar` release and verifies its SHA-256 before use. The separate atlas renderer below remains available for a lightweight preview.

To regenerate the checked-in local derivative from a save on your machine, run from the repository root:

```powershell
python scripts/render_minecraft_map.py --world-zip "C:\path\to\world.zip" --output-dir "apps/web/public/maps/nova-haven"
```

Then start the web app using the local setup above and open `/map` (the default Next.js development port is `3000`). The generated `terrain.png` and `manifest.json` are the only world-derived public assets; never copy the raw save into `apps/web/public`.

## Classification concurrency hardening (2026-09-18)

The current source wraps category PATCH/DELETE and article POST/PATCH/publish/restore in serializable transactions. The API maps recognized PostgreSQL unique/FK/deadlock/serialization conflicts to safe HTTP Problem Details responses. Article edit/restore response ETags are emitted only after successful commit. `scripts/wiki-smoke.mjs` races an article creation with category deactivation on a disposable category and checks that both operations cannot succeed. Historical SQL Server results are in `docs/verification/2026-09-18-classification-concurrency.md`; rerun equivalent checks on PostgreSQL before claiming provider parity.

## Security/deployment restrictions

Do not expose PostgreSQL or file storage publicly; never commit secrets. Production requires HTTPS, data-protection key persistence, login rate limiting, migration review, non-disposable media storage security, backups/restoration and monitoring; this workspace is local-only. All REST mutations use Admin policies and CSRF/ETag as applicable. No automatic production deployment.

## Player accounts, inbox notifications and Web Push (2026-09-29)

Player accounts use ASP.NET Core Identity cookies in the **local PostgreSQL database**. Registration signs the player in immediately, never grants Admin, and does not send or require email. The site has `/account` and a paginated `/notifications` inbox; the navigation bell reads the current player's unread count. Admin publication of Wiki, News, catalog/recipes, knowledge, community, rewards and commerce writes inbox notices with the publication; Admin can also send a broadcast. Notification writes, push-subscription changes and logout require CSRF. Web Push requires server-side VAPID configuration and HTTPS push-provider endpoints; Development VAPID keys are generated only under ignored `.local/webpush-vapid.json`.

The PostgreSQL baseline includes the inbox and Web Push subscription tables. Apply it only to a fresh/disposable PostgreSQL database after reviewing the generated migration. Existing SQL Server databases remain unchanged; do not run PostgreSQL migrations against them or assume an automatic data transfer.

Flutter has immediate player registration/login and the same paginated PostgreSQL-backed notification inbox, with Identity/antiforgery cookies persisted by `flutter_secure_storage`. Native mobile push is not included; Web Push is browser-only. This local feature batch's current test results are recorded in the delivery report and supersede earlier email-confirmation instructions.

Current local account/pagination/notification implementation evidence: [`docs/verification/2026-09-29-player-access-pagination-notifications.md`](docs/verification/2026-09-29-player-access-pagination-notifications.md).

## Wiki tags slice (2026-09-18)

- Admin: create/update/deactivate/delete tags with CSRF and `If-Match` where applicable; selected draft tag UUIDs must be distinct, active and at most 10. Historical tag references prohibit rename/slug change/deactivation/deletion.
- Publication saves immutable `WikiRevisionTags` while editing/restoring updates only `WikiDraftTags`. Public `/api/v1/wiki/tags`, article `tags` slugs and `?tag=` use the current published revision exclusively.
- `npm test` includes tag validation, client, contract, reference-guard and smoke-script assertions. `tests/backend/NovaHaven.Domain.Tests/WikiTagTests.cs` and Flutter mock-client tests are included in the verified domain/Flutter suites.
- Before migration, verify new tables `WikiTags`, `WikiDraftTags`, `WikiRevisionTags` and their restricted FKs/unique indexes exist in the reviewed `InitialWiki` migration. Never run `EnsureCreated` or edit the live Minecraft plugin database.
- Evidence: `docs/verification/2026-09-18-wiki-tags.md`; implementation plan: `docs/superpowers/plans/2026-09-18-wiki-tags.md`.
