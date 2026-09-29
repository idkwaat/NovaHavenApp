# Nova Haven Platform — local all-systems source (NOT deployment-ready)

Wiki-first Minecraft RPG community portal + private Admin CMS + published-only Flutter reader. Discord remains the community hub. The game plugins retain all gameplay authority; this project has no access to their databases.

**Cài đặt local và nạp dữ liệu demo:** xem [Hướng dẫn cài đặt tiếng Việt](docs/INSTALLATION-VI.md). Hướng dẫn dùng database SQL Server local riêng; không chứa file database đã nạp dữ liệu.

## Source and verification status

This source tree was recovered from the approved `openspec/` specification and then extended in local feature slices. Historical commit IDs and test claims from unavailable source checkouts are not evidence for this repository; use the date-stamped verification reports and roadmap for current status. This remains a local MVP, not a production-ready deployment.

## Current source

- `backend/NovaHaven.Domain` — Wiki entities, immutable article revision snapshots and draft/revision tag joins.
- `backend/NovaHaven.Application` — editorial validation.
- `backend/NovaHaven.Infrastructure` — SQL Server EF Core/Identity model and twelve ordered migrations through `AddUserNotifications`.
- `backend/NovaHaven.Api` — cookie/CSRF/admin endpoints, Wiki/Catalog/Knowledge/Community lifecycles, local media/audit/News, Integration capability status, Rewards and local-demo Commerce checkout.
- `apps/web` — Next.js landing, public Wiki/News/Catalog/Recipes/Knowledge/Community/Rewards/Commerce, browser cart, simulated checkout and Admin editors/order history.
- `apps/mobile` — Flutter reader for published Wiki/News/Catalog/Knowledge/Community content, plus player account and notification inbox screens.
- `contracts/openapi/wiki-v1.json` — maintained v1 contract for the implemented local API slices, including Knowledge, Community, Integration, Rewards and Commerce.
- `tests/web` — native Node executable helper/client/contract tests.

The local completion batch includes media upload/gateway, audit, News, bookmarks, published related links/TOC, typed Catalog items/recipes, Knowledge Graph, Community systems, Integration capability status, Rewards definitions and an explicitly simulated local Commerce cart/checkout. It does not connect a payment gateway, charge money or deliver game benefits. Prior all-systems evidence is historical; see `docs/roadmap/48H-ALL-SYSTEMS-STATUS.md` and the date-stamped verification reports for current limits. This remains a local MVP: authoritative Minecraft data sync, real payment/grant execution, browser/mobile E2E and production hardening are not claimed.

Category lifecycle source includes create/detail/edit/reorder/deactivate/delete with draft/revision reference checks, normalized-name uniqueness and a category rowversion. The current schema is represented by twelve ordered EF migrations through `AddUserNotifications`. Do not generate a second `InitialWiki` migration or use `EnsureCreated`; add a new migration only for an intentional model change, review it, then apply the migration chain. The app does not apply migrations on startup. Category PATCH/DELETE require Admin authorization, CSRF and `If-Match`.

## Toolchain

Requires .NET SDK 10, `dotnet-ef` 10.0.12, SQL Server 2022 (local install or Docker), Node >=22/npm and Flutter SDK. Versions of Microsoft EF/Identity references are pinned to 10.0.12. The 2026-09-21 local verification used .NET SDK 10.0.302, `dotnet-ef` 10.0.12, Node 24.16.0 and Flutter 3.44.1; Docker was unavailable.

## Local setup

Làm theo [hướng dẫn cài đặt tiếng Việt](docs/INSTALLATION-VI.md) để tạo SQL Server local, áp dụng migrations, bật Admin Development, nạp dữ liệu demo và chạy Next.js/Flutter. Hướng dẫn dùng database riêng cho demo và nêu rõ các giới hạn hiện tại.

## Test gates

```bash
npm test
dotnet test tests/backend/NovaHaven.Domain.Tests/NovaHaven.Domain.Tests.csproj
cd apps/web && npm install && npm run typecheck && npm run build
cd apps/mobile && flutter pub get && flutter test && flutter analyze
```

The 2026-09-28 disposable-LocalDB run predates local checkout and exercised the prior ten-migration chain. On 2026-09-29 the checkout source, UI, contract and additive migration were implemented; this execution context cannot start the Windows LocalDB instance, so the new SQL migration and transaction integration tests remain unverified. `NovaHaven_Local` was not migrated or changed as part of this feature work. See [`docs/verification/2026-09-28-current-readiness.md`](docs/verification/2026-09-28-current-readiness.md) and the new [`docs/verification/2026-09-29-local-demo-commerce.md`](docs/verification/2026-09-29-local-demo-commerce.md).

For an evidence-based defense package, see [`docs/defense/DEMO-GUIDE.md`](docs/defense/DEMO-GUIDE.md), [`docs/defense/ERD.md`](docs/defense/ERD.md) and [`docs/defense/READINESS-CHECKLIST.md`](docs/defense/READINESS-CHECKLIST.md). Mutation-oriented rehearsals should use a disposable local database, not an existing local database whose content must be preserved.

The Node tests do not replace SQL/browser/manual gates. Current verification includes C# build, LocalDB integration, web production build, Flutter tests/analyze and APK install/launch on an Android emulator. Full browser/mobile acceptance must still verify: Admin login → create draft invisible to public → publish → web/mobile see the same revision → edit draft retains the live revision → republish → restore to draft → unpublish returns 404. Current evidence and the remaining limitations are recorded in the readiness report above.
The Wiki smoke script includes category and tag ETag checks, delete of unused entries, refusal to mutate referenced classifications, published tag filtering and historical tag isolation. The all-systems smoke checks public reads, Admin order-history authorization, and submits only an empty cart that must be rejected without persisting an order. The previous version of this smoke passed against LocalDB; the updated check still needs to run on a working disposable LocalDB after the new migration is applied.

For a local backup/restore rehearsal, run `powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/localdb-backup-restore.ps1`. It creates a timestamped `.bak`, verifies it, restores to a new database name, runs `DBCC CHECKDB`, checks the EF migration count and retains the artifacts for inspection. It does not overwrite an existing restore target or modify the source database. Generated backup files are ignored by Git.

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

The current source wraps category PATCH/DELETE and article POST/PATCH/publish/restore in SQL Server `Serializable` transactions. The Admin mutation filter maps SQL error 1205 (including EF-wrapped errors) to HTTP 409 with a reload/retry message. Article edit/restore response ETags are emitted only after successful commit. `scripts/wiki-smoke.mjs` races an article creation with category deactivation on a disposable category and checks that both operations cannot succeed. This path is compiled and has passed against SQL Server LocalDB; see `docs/verification/2026-09-18-classification-concurrency.md` and the current all-systems evidence.

## Security/deployment restrictions

No direct public SQL Server or file exposure; never commit secrets. Production requires HTTPS, data-protection key persistence, login rate limiting, migration review, non-disposable media storage security, backups/restoration and monitoring; this workspace is local-only. All REST mutations use Admin policies and CSRF/ETag as applicable. No automatic production deployment.

## Player accounts, inbox notifications and Web Push (2026-09-29)

Player accounts use the existing ASP.NET Core Identity cookie store in the **local SQL Server database**. Registration does not grant Admin; users must confirm their email before login. Under `ASPNETCORE_ENVIRONMENT=Development`, confirmation messages are written to the ignored `.local/mail-outbox` folder and can be opened from `/account`; outside Development configure SMTP through environment variables in `.env.example`. The site has `/account` and `/notifications`; Admin CMS has a notification broadcast panel. Notification writes, push-subscription changes and logout require a CSRF token. Push endpoints are restricted to recognized HTTPS push providers; VAPID keys are generated into ignored `.local/webpush-vapid.json` only in Development. Production must provide its own VAPID keys and public HTTPS origin.

The additive SQL Server migration is `AddUserNotifications`. It was generated and model-checked, but this handoff did not apply it to `NovaHaven_Local`. For a **new disposable local database** named `NovaHaven_AccountDev`, set both `ConnectionStrings__NovaDb` and `NOVA_DB_CONNECTION` to the same SQL Server LocalDB/SQL Server connection string, then run `dotnet ef database update --project backend/NovaHaven.Infrastructure --startup-project backend/NovaHaven.Api --context NovaDbContext`. The current execution environment could not start LocalDB; do not redirect this command to a preview DB with data you need to preserve.

Flutter now has player login/registration/email-confirmation and the same SQL-backed notification inbox, with Identity/antiforgery cookies persisted by `flutter_secure_storage`. Native mobile push delivery is not included: it needs a platform push provider and credentials (for example Firebase), while Web Push is browser-only. Flutter tests were added but not executable in this environment because the Flutter SDK is unavailable.

## Wiki tags slice (2026-09-18)

- Admin: create/update/deactivate/delete tags with CSRF and `If-Match` where applicable; selected draft tag UUIDs must be distinct, active and at most 10. Historical tag references prohibit rename/slug change/deactivation/deletion.
- Publication saves immutable `WikiRevisionTags` while editing/restoring updates only `WikiDraftTags`. Public `/api/v1/wiki/tags`, article `tags` slugs and `?tag=` use the current published revision exclusively.
- `npm test` includes tag validation, client, contract, reference-guard and smoke-script assertions. `tests/backend/NovaHaven.Domain.Tests/WikiTagTests.cs` and Flutter mock-client tests are included in the verified domain/Flutter suites.
- Before migration, verify new tables `WikiTags`, `WikiDraftTags`, `WikiRevisionTags` and their restricted FKs/unique indexes exist in the reviewed `InitialWiki` migration. Never run `EnsureCreated` or edit the live Minecraft plugin database.
- Evidence: `docs/verification/2026-09-18-wiki-tags.md`; implementation plan: `docs/superpowers/plans/2026-09-18-wiki-tags.md`.
