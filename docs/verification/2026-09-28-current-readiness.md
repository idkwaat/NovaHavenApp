# Nova Haven — current local readiness check

Date: 2026-09-28  
Workspace: `D:\nova-haven-handoff`  
Scope: local source/build verification only; no remote database, Minecraft database, or deployment.

## Freshly verified

| Gate | Result |
| --- | --- |
| `npm test` | PASS — 115/115 web/client/contract tests (fresh full-suite run after editorial seed expansion) |
| `apps/web: npm run typecheck` | PASS |
| `apps/web: npm run build` | PASS — Next.js production build generated the configured routes |
| `dotnet build backend/NovaHaven.Api/NovaHaven.Api.csproj --no-restore` | PASS — 0 warnings, 0 errors |
| `dotnet test tests/backend/NovaHaven.Domain.Tests/NovaHaven.Domain.Tests.csproj --no-restore` | PASS — 39/39 (fresh continuation rerun) |
| `dotnet test tests/backend/NovaHaven.Integration.Tests/NovaHaven.Integration.Tests.csproj --no-restore` | PASS — 22/22 against SQL Server LocalDB in the LocalDB-owning execution context (fresh continuation rerun); includes ten concurrent same-ETag edit races and the database-cleanup regression test |
| EF migration chain | PASS — all 10 reviewed migrations applied to a newly created, isolated local smoke database |
| `npm run seed:demo` | PASS — isolated LocalDB/API published 15 Wiki articles, 4 News posts, 5 Knowledge locations and 4 NPCs. Public details returned content, NPCs resolved to their published location snapshots, and the second run was idempotent. The disposable database was dropped; `NovaHaven_Local` was not written. |
| Editorial seed integrity | PASS — all 5 focused seed tests and all 115 Node tests pass; new Knowledge coordinates are null and the seed does not create Catalog/recipe, Community, Rewards or Commerce placeholders. |
| `npm run smoke:wiki` | PASS — auth, category/classification race, tags, draft privacy, publish, ETag, republish, restore, unpublish |
| `npm run smoke:all` | PASS — health, public Knowledge/Community/Integration/Rewards/Commerce reads, diagnostics, Admin reads, and no checkout execution |
| Local web routes on `127.0.0.1:3001` | PASS — `/`, `/wiki`, `/news`, `/catalog`, `/knowledge/location`, `/community/guild`, `/rewards`, `/commerce`, `/admin` returned HTTP 200 against `NovaHaven_Local`; the expected Vietnamese content/login labels were present and no API connection error appeared. |
| `npm run smoke:defense` | PASS — read-only loopback GETs confirmed all 3 published Wiki articles match API listing, API detail and Next.js server-rendered pages; `NovaHaven_Local` was not mutated. |
| Flutter | PASS — fresh `flutter test --no-pub` 17/17; `flutter analyze --no-pub` reports no issues; defense-closure rerun reconfirmed both and `flutter build apk --debug --no-pub` succeeded at `apps/mobile/build/app/outputs/flutter-apk/app-debug.apk`. |
| Android emulator | PARTIAL — a prior APK install on the Pixel 7 emulator (Android 17/API 37) was repeatedly terminated before Flutter left the splash screen with `reason=3 (LOW_MEMORY)`; emulator reported 2 GB RAM and low-memory status. The defense-closure APK rebuild was not reinstalled; there is still no stable visual evidence for this exact APK. No fatal Flutter/Android exception appeared in earlier sampled logs. The older `mobile-final.png`/`mobile-explore.png` captures are from the prior mobile layout and are not visual proof of this newest design. |

## Local database and process scope

The migration and HTTP smoke flow used a randomly named `NovaHaven_CodexSmoke_*` database, not `NovaHaven_Local`. After those tests, the smoke API was stopped and that exact temporary database was verified absent and dropped. `NovaHaven_Local` was preserved; its existing migration history was read-only checked and showed all 10 reviewed migrations. For the local preview, the API is now running on `http://127.0.0.1:5080` in Development against `NovaHaven_Local`, with migration, demo seed and Admin bootstrap disabled. Public GETs only were used against that database. The emulator then connected to this local API and loaded the Wiki list endpoints. This mobile UI batch made no database writes, migrations or seed operations.

The mobile UI slices add a shared dark-earth theme aligned with the approved web palette, four labeled primary tabs, an Explore hub, more natural Vietnamese labels for known category/tag slugs, filtering out zero-article categories, and localized Wiki article detail/bookmark presentation. This refinement moves category/tag filters into an Apply/Clear bottom sheet, reduces mobile page-heading scale and card density, and shortens Explore destinations; widget checks cover 320dp/360dp viewports, first-article position, category+tag selection, 48dp filter target, detail typography, larger text and 812×375 landscape. `test/mobile_experience_test.dart` covers the theme/contrast, navigation, empty fixture filtering, localization, article/bookmark behavior and the compact layout. The newest APK builds and installs, but the emulator's low-memory termination blocked a trustworthy screenshot/render check of this revision. This batch made no database writes, migrations or seed operations.

The latest web continuation adds browser-local Wiki bookmarks and “Đọc gần đây” (up to 20 validated slugs), both revalidated against published-only API responses. Browser verification covered visit → recent list → clear. A 390×844 pass across `/`, `/wiki`, `/bookmarks`, `/recent`, `/news`, `/catalog`, `/community/guild` and `/admin` found no horizontal overflow or visible link/form control below 44×44px; keyboard focus reached the primary CTA visibly. Footer links, secondary navigation and Admin login fields were adjusted where that pass found undersized targets. That web-only checkpoint had 111/111 Node tests; the preceding Wiki-seed checkpoint had 112/112, and the latest full suite after expanding News/Knowledge seed coverage is 115/115. Web typecheck/build, domain (39/39), integration (22/22) and read-only API/SSR parity smoke (3 published articles) were rerun at the web checkpoint. No database writes were made against `NovaHaven_Local`.

A read-only inventory also found 80 pre-existing `NovaHaven_Integration_<32-hex-guid>` databases created on 2026-09-21/22, with no active sessions. Their naming/date pattern matches the integration fixture, and today's 21-test run added none. A bulk removal request was blocked by the safety reviewer because these databases predate this task and their contents were not proven disposable/recoverable. They were left untouched; explicit owner authorization is needed before removing this legacy test data.

An earlier unprivileged test attempt could not access the per-user LocalDB registry and reported 0/20 initialized tests. Re-running under the LocalDB-owning context passed all 21 tests; this was an execution-identity limitation, not evidence of an application integration failure. The regular sandbox still cannot inspect that LocalDB instance without the authorized local execution context.

## Still not accepted end-to-end

- Authenticated browser E2E across Admin draft → publish → public read → edit/republish → restore/unpublish. The latest LocalDB integration suite verifies the backend lifecycle and generated-cookie authorization, but does not substitute for exercising the Admin forms in a browser.
- Mobile visual confirmation of the same published revision shown on the web, including a detail-page navigation. Widget tests cover article opening/bookmarking, but do not substitute for this cross-client end-to-end assertion.
- Accessibility/WCAG audit, full responsive visual pass, and a physical-device network test.
- Long-running repeated multi-editor stress, Docker SQL Server path, production deployment, external plugin/Discord/payment integrations, backup/restore rehearsal on a current database.

These are remaining acceptance gates, not claims that the application is complete or production-ready. The checkpoint has no `.git` directory, so this report records command evidence rather than a Git diff or commit.

## Defense-closure follow-up — 2026-09-28

| Gate | Latest evidence |
| --- | --- |
| Repeated ETag concurrency | `ConcurrentArticleEditsWithSameEtagHaveOneWinnerPerRound` ran ten same-token races against its own generated LocalDB. Every round had one committed winner, one rejected loser, persisted draft equal to the winner, and published revision unchanged. Full integration suite: 22/22. |
| Concurrency response contract | A losing concurrent request may receive 412 for a stale precondition or 409 `Concurrent update conflict` when SQL Server reports deadlock 1205. The test requires one winner and one rejected write, not a specific loser code for every interleaving. Sequential stale-ETag behavior remains covered separately. |
| Web/API public parity | `npm run smoke:defense` passed read-only against loopback API `127.0.0.1:5080` and web `127.0.0.1:3001`: all three current published slugs matched list, detail and server-rendered pages for title, summary and revision. |
| Domain, Node and mobile | Fresh runs: domain 39/39, Node 83/83, Flutter 17/17, Flutter analyzer clean. `apps/web npm run typecheck` also passed. |
| Defense artifacts | Added [`docs/defense/ERD.md`](../defense/ERD.md), [`docs/defense/DEMO-GUIDE.md`](../defense/DEMO-GUIDE.md), and [`docs/defense/READINESS-CHECKLIST.md`](../defense/READINESS-CHECKLIST.md). ERD uses EF mapping plus read-only SQL FK metadata; demo mutation steps require a disposable local database. |

`NovaHaven_Local` remained outside the C# test fixture and the parity smoke issued GET requests only. No migration, seed, article edit or other write was run against that existing preview database during this follow-up. Browser-authenticated lifecycle E2E, stable visual Android acceptance and full manual accessibility review remain open.

## Wiki content seed follow-up — 2026-09-28

Added twelve original, sectioned Vietnamese Wiki articles and four focused tags to the local demo seed. A fresh uniquely named disposable LocalDB had all ten reviewed migrations applied; the seed published the three existing baseline entries and twelve new entries, public detail reads returned Markdown for all 15, and a second seed run left the slug set/count unchanged. The disposable database was dropped. The existing preview database still serves three articles and `NovaHaven_Local` was intentionally left untouched because no Admin credentials were available in this task environment. The seed continues to require the normal Admin API login and refuses non-loopback API origins. See [`2026-09-28-wiki-content-seed.md`](2026-09-28-wiki-content-seed.md).

## Other editorial sections continuation — 2026-09-28

Expanded the same local seed with four News posts describing implemented Nova Haven features and four additional Knowledge locations plus four NPC profiles. The latter are clearly identified as original Nova Haven lore rather than verified game entities; all map coordinates are null, and each NPC resolves to an existing published location snapshot. On a fresh disposable LocalDB, public API checks passed for all 15 Wiki articles, 4 News entries, 5 locations and 4 NPCs; the second seed run preserved counts and slugs. The temporary database `NovaHaven_ContentSeedVerify_20260928_36670cb152a444acad6c5b229075b2aa` was dropped. Existing `NovaHaven_Local` was not modified and its preview still returns the same three published Wiki articles; no Admin credentials were available to publish the new content there. Legacy records from earlier seed versions, if present in a local database, are not automatically deleted. Seed-created Catalog/recipe, Community, Rewards and Commerce placeholders have been removed from future seed runs because no authoritative content source is available. Existing records are not deleted or rewritten; a matching existing draft may be published by the seed. Fresh verification: Node 115/115 and read-only `npm run smoke:defense` matched all 3 current preview articles. See [`2026-09-28-editorial-content-seed.md`](2026-09-28-editorial-content-seed.md).
