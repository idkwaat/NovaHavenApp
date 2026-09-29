# Wiki CMS delivery tasks

## Foundation recovery (2026-09-18)

- [x] Restore approved Wiki spec and architecture design (text).
- [x] Create new source skeleton: Domain/Application/Infrastructure/API, Next.js, Flutter.
- [x] Write and execute native Node tests for shared web logic and contract checks.
- [x] Build backend, web and mobile with their actual SDKs (Android APK remains).
- [x] Produce EF Core migrations and run against real local SQL Server LocalDB.

## CMS backend and Admin

- [x] Draft-only editing, server-side validation, Admin role guard and CSRF route code.
- [x] Immutable revision creation, ETag checks and public-only query code.
- [x] Admin UI: login, article create/edit, category create, publish and unpublish source.
- [x] Real SQL Server LocalDB integration verification of the core lifecycle.
- [x] Category PATCH/DELETE endpoint and UI source, draft/revision reference policy, normalized name index and ETag contracts (not yet built with .NET).
- [x] Confirm category lifecycle and role/ETag behavior against SQL Server LocalDB integration tests; repeated stress remains.
- [x] Add source-level `Serializable` transaction coverage for category PATCH/DELETE and article POST/PATCH/publish/restore, SQL 1205 → 409 mapping, contract checks and disposable concurrency smoke scenario (NOT database-verified).
- [x] Compile transaction changes and run LocalDB lifecycle/concurrency smoke; repeated multi-editor stress remains.
- [x] Tag management and published-only tag filtering verified through .NET, Flutter, migration and LocalDB integration.
- [x] Private media upload, content inspection, publication reference and safe gateway.
- [x] Admin revision list/restore UI and audit history.
- [x] Expose first media from the current published revision in Wiki summaries and render attributed, responsive article-card previews.

## Public and mobile

- [x] Public website home/list/detail/search source.
- [x] Flutter public reader source and test cases.
- [x] `next build` and Flutter `analyze`, `test` (Android build remains).
- [x] Safe Markdown rendering for web and approved published media gateway; mobile remains inert Markdown text.
- [x] Run backend cross-lifecycle integration demonstration; browser/mobile E2E remains.

## Delivery

- [x] Update OpenAPI with media, audit and News paths plus core request schemas.
- [ ] Real EF migration/snapshot, seed, diagrams, screen captures and real deployment instructions.
- [ ] CI, security review, staging backup + restore test before production approval.
