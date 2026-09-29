# Nova Haven local completion batch — 2026-09-21 (superseded)

> This is the earlier Wiki/Catalog batch record. The later all-systems evidence, including Knowledge, Community, Integration, Rewards and Commerce, is in `docs/verification/2026-09-21-all-systems-local.md`.

This batch continued from the P0 checkpoint and used only SQL Server LocalDB (`MSSQLLocalDB`) with database `NovaHaven_Local`. No Docker, remote database, production deployment, or Minecraft/plugin database was used.

## Implemented slices

- SQL Server migrations: `InitialWiki`, `AddWikiMedia`, `AddWikiAudit`, `AddNews`, `AddCatalogItems`, `AddCatalogRecipes`.
- Editorial Catalog slice: typed item kind plus recipe graph, draft/edit, immutable published revisions, public published-only list/detail and Admin CSRF/ETag lifecycle. Recipe publication snapshots only already-published item revisions. No Minecraft/plugin data is seeded or synchronized.
- Private local media storage with PNG/JPEG/WebP signature and dimension validation, 5 MB limit, random server storage keys, Admin preview and published-reference-only public gateway.
- Draft/revision media joins; publish snapshots media references atomically; public article detail includes published media metadata.
- Article audit events for create/edit/publish/unpublish/restore and media upload, plus Admin audit query.
- Next Admin media upload/selection and Revision history/restore/audit UI.
- Separate News OpenSpec, SQL entity, published-only public/Admin lifecycle API, Admin editor and public Next pages.
- Flutter secure bookmark storage/bookmark list filter, News reader and safe rich Markdown rendering in the published reader.
- Wiki Engine 2.0 local slice: published-only related links, deterministic level-1-to-3 Markdown TOC anchors on web and a compact TOC/related reader section in Flutter.
- OpenAPI contract paths for media, audit, News and Catalog items/recipes.

## Verification evidence

| Command | Result |
| --- | --- |
| `npm test` | PASS — 42/42 |
| `dotnet build backend/NovaHaven.Api/NovaHaven.Api.csproj --no-restore` | PASS — 0 warnings, 0 errors |
| `dotnet test tests/backend/NovaHaven.Domain.Tests/NovaHaven.Domain.Tests.csproj --no-restore` | PASS — 25/25 |
| `dotnet test tests/backend/NovaHaven.Integration.Tests/NovaHaven.Integration.Tests.csproj --no-restore` | PASS — 10/10 on SQL Server LocalDB; integration collections configured sequentially for LocalDB |
| `dotnet ef database update ...` | PASS — applied `AddCatalogRecipes` to `NovaHaven_Local`; subsequent model/update checks are clean |
| `dotnet ef migrations has-pending-model-changes ...` | PASS — no model changes pending |
| `apps/web: npm run typecheck` | PASS |
| `apps/web: npm run build` | PASS — routes include `/catalog`, `/catalog/[slug]`, `/catalog/recipes`, `/catalog/recipes/[slug]`, `/news` and `/news/[slug]` |
| `apps/mobile: flutter pub get` | PASS |
| `apps/mobile: flutter test --no-pub` | PASS — 8/8 |
| `apps/mobile: flutter analyze` | PASS — no issues |
| `apps/mobile: flutter build apk --debug` | PASS — `build/app/outputs/flutter-apk/app-debug.apk` |

The SQL integration suite covers authentication/authorization, CSRF-backed mutations, ETag 428/412 behavior, duplicate conflicts, published-only Wiki and related-link privacy, media signature/privacy/public visibility, revision restore isolation/audit, News publish visibility, Catalog item revision lifecycle and recipe publication snapshots over published item revisions.

## Still not claimed

Browser E2E, production deployment, backup/restore, HTTPS/data-protection key persistence, rate limiting, accessibility audit, emulator installation and CI remain unverified. E02–E11 and E13 still require authoritative data/provider contracts; the local media directory is intentionally disposable and is not a production storage design.
