# Nova Haven roadmap — evidence-based, 2026-09-18

## Status legend

- `SPEC`: approved direction, written behavior only.
- `SOURCE`: files exist but full build/integration is unverified.
- `VERIFIED`: exact test/build/deployment gate has run and passed.
- `BLOCKED`: environment or dependency required to verify.
- `LATER`: separate phase, deliberately not in capstone MVP.

**Recovery note:** On 2026-09-18 the prior workspace contained only `README.md`/`AGENTS.md`; earlier claims about 36 Node tests, media/tag implementation, and commit IDs could not be reproduced. This repository is a reconstruction from the approved specification, not a continuation of that missing source. Do not carry forward earlier pass counts.

**Current execution note (2026-09-28):** Fresh verification includes the LocalDB integration suite (22/22), a ten-round same-ETag concurrent edit test on a generated disposable database, 39/39 domain tests, 83/83 Node tests, Flutter test/analyze (17/17 and clean), a fresh debug APK build, and a read-only local API/Next SSR parity smoke matching all three published Wiki entries. All 10 reviewed migrations were applied to an isolated local smoke database; Wiki/all-systems HTTP smoke, web typecheck and production build are recorded. This closure batch did not rerun the web production build. Android previously killed the prior APK before leaving splash with `LOW_MEMORY` on the 2 GB emulator; the rebuilt APK has not been reinstalled, so stable visual/device acceptance remains unavailable. The existing `NovaHaven_Local` API preview received GET-only parity checks; no migration or seed was run against that database. A read-only inventory found 80 inactive legacy test databases from 2026-09-21/22; they were left untouched because bulk removal was blocked by safety review. Authenticated browser E2E, a stable mobile same-revision/detail run, full manual accessibility review and rubric crosswalk remain open. Added local demo/defense documentation under [`docs/defense/`](../defense/). See [`docs/verification/2026-09-28-current-readiness.md`](../verification/2026-09-28-current-readiness.md) for exact results and limits. The older `VERIFIED` entries below describe their dated runs, not a guarantee that those dependencies are currently available.

**Latest web continuation (2026-09-28):** Extended the browser-local Wiki reading list with “Đọc gần đây”, bounded to 20 validated slugs and timestamps; recent entries are revalidated against the anonymous published-content API, and clear-history is independent of saved bookmarks. Full Node suite passed (111/111), web typecheck and Next production build passed. Browser verification confirmed visit → recent list → clear returns the localized empty state. At 390×844, eight key routes had no horizontal overflow and every visible link/form control measured at least 44×44px; keyboard navigation reached the primary CTA with a visible focus ring. This pass also corrected undersized footer/secondary links and Admin login inputs. The previous bookmark save/list/search/remove browser flow was also verified. No database or production state was changed.

**Player account and notification continuation (2026-09-29):** `SOURCE` SQL Server Identity registration/email confirmation/login/current-user/logout, Development-only local mail outbox + optional SMTP, per-account inbox/read API, Admin broadcast, VAPID Web Push gateway and SQL migration `AddUserNotifications`; Next account/inbox/push UI + service worker/Admin broadcast form; Flutter account/inbox using platform secure storage and shared cookie/CSRF API. `VERIFIED` API build (0 warnings/errors), EF model has no pending changes, Next production build, web typecheck, Node tests (153 pass, 4 pre-existing skipped), domain tests (78/78) and push endpoint rules (7/7). `BLOCKED` Applying/running the SQL Server integration suite: LocalDB cannot start in this execution context; migration was generated/model snapshot updated but not applied to any database. `BLOCKED` Flutter test/analyze: SDK is not installed/on PATH. `LATER` Native FCM/mobile push is deliberately not claimed; this slice implements account/inbox on mobile and Web Push in browsers. See [`docs/verification/2026-09-29-user-notifications-webpush.md`](../verification/2026-09-29-user-notifications-webpush.md).

**Wiki content continuation (2026-09-28):** Added twelve original Vietnamese articles (four each for Khởi hành, Lớp nhân vật and Vùng đất) plus four focused tags to the local-only idempotent demo seed. Articles are sectioned editorial lore/player guidance; they do not invent or claim authoritative plugin stats, drops, commands or recipes. A fresh disposable LocalDB/API run applied the reviewed migrations, published all 15 Wiki entries, read each published detail and passed a second seed run without changing the set. The disposable database was dropped afterward. `NovaHaven_Local` was not mutated and still contains its prior three entries because this session has no Admin login credentials for that preview database. Evidence: [`docs/verification/2026-09-28-wiki-content-seed.md`](../verification/2026-09-28-wiki-content-seed.md).

**Other editorial sections continuation (2026-09-28):** Added four News posts about existing product behavior, four additional original-lore locations and four original-lore NPC profiles. Location coordinates remain null, lore entries explicitly disclaim verification against the online server, and each NPC has a valid published location relation. A disposable LocalDB/API integration check verified all public details and a second seed run preserved the dataset. Current seed no longer creates unsupported sample item/recipe, guild, reward or commerce rows; earlier such rows, if already in a local database, are left untouched. `NovaHaven_Local` remains unchanged, so its preview still serves three Wiki articles until an authorized local Admin runs the seed. Evidence: [`docs/verification/2026-09-28-editorial-content-seed.md`](../verification/2026-09-28-editorial-content-seed.md).

## M0 — Specification and scope

- `SPEC` Product: Minecraft RPG Wiki + CMS + Flutter published reader, Discord community, no gameplay DB writes.
- `SPEC` Architecture/design and Wiki OpenSpec requirements restored into this repository.
- `SOURCE` Foundation reconstruction plan added under `docs/superpowers/plans/`.
- Gate: written specification reviewed and versioned; OpenAPI schemas reconciled to actual endpoint implementation.

## M1 — Foundation: backend, Identity, SQL Server

- `SOURCE` Four .NET projects (Api, Application, Domain, Infrastructure); role-scoped Admin APIs, cookie auth, CSRF, EF model and rowversion.
- `SOURCE` Development-only opt-in Admin bootstrap; private credentials via environment.
- `VERIFIED` Classification-sensitive article/category mutations use serializable SQL transactions; the LocalDB integration suite and Wiki smoke include the classification race and ETag/lifecycle checks. Repeated long-running multi-editor stress is still open.
- `VERIFIED` API restore/build on .NET SDK 10.0.302: 0 warnings, 0 errors; domain xUnit: 20/20. Evidence: `docs/verification/2026-09-21-complete-platform.md`.
- `VERIFIED` Generated and reviewed `InitialWiki` migration/model snapshot with EF Core 10.0.12; schema includes Identity, Wiki articles/revisions/categories/tags, draft/revision joins, unique indexes, rowversion and restrictive history FKs.
- `VERIFIED` Applied the migration to disposable SQL Server LocalDB `NovaHaven_Local` and ran the localhost smoke lifecycle, including ETag/CSRF, classification race, tag revision isolation and draft/public separation.
- `VERIFIED` SQL Server LocalDB integration suite covers anonymous 401, non-admin 403, ETag 428/412, duplicate conflict and published-only lifecycle. Docker remains unavailable, so LocalDB is the local relational gate.
- Gate: cold-start instructions on a new machine, migration creates database, Admin login and permission checks pass with real SQL Server.

## M2 — Wiki content CMS

- `SOURCE` Article create/edit, draft validation, immutable revision snapshot, publish/unpublish, preview-only Admin GET, revision history/restore, ETag preconditions.
- `SOURCE` Category create/list/detail/update/deactivate/delete, reference guard policy, normalized-name and slug uniqueness, rowversion/If-Match preconditions; public article search/category listing still uses published revision only.
- `SOURCE` Category create/edit/delete controls in Admin, with the article dropdown restricted to active categories.
- `VERIFIED` Tag management (normalized unique tag catalog, reference/ETag guards), draft-tag and immutable revision-tag joins, public catalog excludes tags without a current published reference, published-only filter, Admin multi-select, Next.js and Flutter readers. LocalDB integration covers tag privacy and mutation guards.
- `VERIFIED` Local private media upload/gateway, PNG/JPEG/WebP signature and dimension checks, 5 MB limit, published-reference visibility and integration tests.
- `VERIFIED` Admin revision history/restore UI and article audit history/API; restore remains draft-only and does not change the current public revision.
- `VERIFIED` `scripts/wiki-smoke.mjs` runs category/tag lifecycle plus draft → publish → edit → republish → restore → unpublish against the local API and LocalDB.
- `VERIFIED` LocalDB integration covers the classification race, role guards, private media leakage and published-only related links; a full repeated long-running simultaneous-editor stress run and Docker SQL Server path remain unverified.
- `VERIFIED` Wiki Engine 2.0 local slice adds published-only related links, deterministic Markdown TOC rendering and public SEO discovery (`robots.txt`, resilient published-only sitemap and canonical/OpenGraph metadata) in Web/Flutter; redirect management and browser E2E remain open.
- Gate: categories + articles + tags/media pass all OpenSpec scenarios with backend integration and web UI tests. Category reference checks must be exercised against actual SQL Server under concurrent edits before marking VERIFIED.

## M3 — Public Website + Admin

- `SOURCE` Next.js landing page, public Wiki/search/categories/tags/detail, Admin login/category/tag lifecycle/article editor and publish/unpublish.
- `SOURCE` Raw HTML disabled in React Markdown; sanitation and constrained links; conservative no-store data fetching.
- `VERIFIED` `apps/web` dependency install, TypeScript typecheck and Next production build pass. Browser E2E and mobile/responsive manual checks remain unrun.
- `VERIFIED` News/changelog separate OpenSpec, persistence migration, published-only public/Admin lifecycle API, Admin editor and public Next pages.
- `SOURCE` Rules/FAQ authored pages, dedicated Open Graph image, accessibility audit and production asset-library operations remain outside this local completion batch.
- Gate: production build, manual/browser E2E with SQL API, mobile viewport and canonical/OG metadata checks.

## M4 — Flutter Companion

- `VERIFIED` Anonymous category/tag/search/list/detail client, loading/empty/error/retry states, tagged filtering, published related links/TOC, News reader and Flutter API model tests.
- `VERIFIED` Safe rich Markdown rendering is inert for links/raw HTML in the reader; published media remains served by the API gateway.
- `VERIFIED` Flutter 3.44.1 `test` (12/12), `analyze`, debug APK build, and install/launch on the Pixel 7 Android 17 emulator pass; the app reached local Wiki category/tag/article endpoints. Same-revision detail rendering across web/mobile remains unverified.
- `VERIFIED` Secure local Flutter bookmark storage, bookmark list filter and reader action.
- `VERIFIED` 2026-09-28 mobile presentation slice: shared approved dark-earth tokens, four labeled tabs, Explore hub, Vietnamese taxonomy labels, and hiding categories with zero published articles. Follow-up mobile refinement relocates taxonomy filters into an Apply/Clear sheet and reduces hierarchy/card height for small phones. Fresh Flutter tests pass 17/17 (including 320dp/360dp layout, category/tag selection, larger text, landscape, 48dp target and detail typography); analyze is clean; newest debug APK rebuilt and installed. Latest render is blocked by emulator `LOW_MEMORY`; see current readiness report. Widget coverage includes opening a published Wiki item and saving a bookmark.
- Gate: Flutter test + analyze + Android install, both web and mobile show identical published revision.

## M5 — Capstone acceptance / quality

- `VERIFIED` Native Node tests: 41/41; backend domain xUnit: 23/23; LocalDB migration/smoke, web typecheck/build and Flutter test/analyze/APK are recorded in `docs/verification/2026-09-21-complete-platform.md`. The typed Catalog item slice is now included.
- `VERIFIED` Repeated relational simultaneous-editor stress: ten same-ETag races, one winner/rejected loser each round, stored draft corresponds to the winner, published snapshot unchanged; see current readiness report.
- `BLOCKED` Authenticated browser E2E, visual mobile same-revision/detail assertion and full manual accessibility audit remain unverified.
- `VERIFIED` Explicit idempotent Vietnamese demo seed for local API/LocalDB creates publishable fixtures across Wiki, News, Catalog/Recipes, Knowledge, Community, Rewards and Commerce. Evidence: `docs/verification/2026-09-22-demo-seed.md`.
- `VERIFIED` Source-grounded ERD and local demo/defense guide added under `docs/defense/`.
- `SOURCE` Screenshot/video, evaluation-rubric crosswalk, full accessibility audit and logging/monitoring remain.
- Gate: fresh all-platform builds pass and script: Admin login → draft hidden → publish → web/mobile agree → draft edit remains private → republish → restore → unpublish → 404.

## M6 — Production deployment (only after course MVP)

- `NOT DONE` Domain/TLS, production secrets, persistent SQL/media storage, CI deployment, backup and tested restore, uptime/log alerts, staged migration and rollback plan.
- Gate: staging full smoke; backups restored successfully; written owner approval to deploy.

## M7 — Minecraft adapter

- `LATER` Read actual Java plugin contracts; introduce read-only, authenticated integration only if needed; no direct modification of plugin SQL.

## Next implementation order

1. Add authenticated browser E2E and stable emulator data-read parity against the same disposable LocalDB API/demo seed.
2. Complete manual keyboard/screen-reader accessibility checks and map defense artifacts to the actual evaluation rubric.
3. Extend contention/rollback coverage if the rubric requires broader multi-editor/category/tag scenarios.
4. Only after local acceptance, design staging/production storage, backups, HTTPS and deployment.

This roadmap is a tracking source, **not** a claim that a listed source-only feature has been compiled, deployed or tested end-to-end.
