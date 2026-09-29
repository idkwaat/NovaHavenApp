# Nova Haven Platform — architecture design

**Date:** 2026-09-17
**Decision status:** The owner approved the product direction in chat; the Wiki specification is the implementation baseline.
**Classification:** Architectural / greenfield handoff; source code has not been inspected because no repository was attached.

## Context and objective

A term-4 capstone requires a Flutter mobile frontend, React/Angular-class admin website, REST backend and DB. The owner prefers an actual useful website for the Minecraft Nova Haven server, while Discord remains the community hub. Product = public Minecraft RPG portal/Wiki + content editor CMS + companion reading app, shared API. It must be demonstrable without an online Minecraft server.

## Options considered

- **A. Separate student application:** simpler to isolate; wasted effort and diverges from Nova branding.
- **B. Standalone platform with optional future Minecraft adapter (chosen):** course-ready offline, future extensible and respects plugin ownership; requires contract discipline.
- **C. Direct plugin DB integration:** creates coupling to unknown schema and risk of corrupting game authority; rejected.

## Product boundaries

Public website: landing, IP and Discord links, Wiki/categories/articles/search, guides, news, rules/FAQ. Admin: editorial CMS for Wiki then news. Mobile: Wiki/news reader and later authenticated bookmarks. Discord continues to own social/chat/support. No forum, ticketing, economy, marketplace, gameplay progression, player profile sync, NovaVFX editor or Minecraft account linking in the first release.

## Stack and physical architecture

- One monorepo: `apps/web` Next.js + React + TypeScript for public pages and `/admin`; `apps/mobile` Flutter/Dart; `backend/NovaHaven.Api`, `.Application`, `.Domain`, `.Infrastructure`; `tests`, `contracts/openapi`, `openspec`, `docs`.
- ASP.NET Core Web API + EF Core + SQL Server. Modular monolith, separated by functional feature; no microservice/broker introduced for hypothetical scale.
- Both frontend clients consume versioned REST API (`/api/v1`). OpenAPI and example fixtures serve as contract. Web public pages should render discoverable content server-side; use explicit cache expiry or post-publish invalidation rather than letting previews or stale drafts leak.
- Local: Docker Compose SQL Server + API + web (Flutter runs emulator/device). Production: HTTPS reverse proxy, route `/api` to backend, other paths to Next.js, SQL Server private; secrets in environment/secret manager, not Git. Hosting provider/domain deferred until available.
- Game plugin integration sits behind an adapter only after code audit and approved plugin contract. The adapter may consume authenticated APIs/read-only snapshots, never write to plugin-managed databases.

## Separation of responsibilities

- API: HTTP contract, authorization enforcement, request validation entry, Problem Details, correlation IDs.
- Application: use cases, editorial rules, transactions, concurrency orchestration.
- Domain: content invariants, editorial state, revision model.
- Infrastructure: Identity persistence, EF Core/migrations, media storage, external adapters.
- Avoid Generic Repository boilerplate over EF Core; use services and feature-focused querying. DTOs at HTTP boundary; entity internals not returned.
- Next.js Admin forms call API over same-origin `/api` deployment route. Public content may be fetched server-side. Flutter uses generated/typed client aligned to OpenAPI; never shares SQL access or source-level domain rules.

## Identity and authorization boundary

- ASP.NET Core Identity for users/roles. Anonymous public reading. Admin role required for editorial mutations; player role for eventual bookmarks.
- For web choose HttpOnly+Secure cookie, SameSite deployment policy and CSRF protection on all unsafe methods; avoid token in browser localStorage. Flutter may use access token + rotated/revocable refresh token stored in platform secure storage, designed in its separate Identity spec. Do not pretend token flows are already implemented.
- Media uploads must be authenticated, type/size checked, safely named, and private until referenced by a currently published revision. Render Markdown as sanitized HTML; no arbitrary raw HTML or script execution. No secrets in repo.

## Editorial model and data ownership

- `WikiArticles`: stable UUID, immutable slug after first publish, draft fields, current public revision reference, editorial state, metadata, `rowversion` concurrency token.
- `WikiRevisions`: immutable title/summary/content/category/tags snapshots and publication metadata. Editing creates/updates a draft; publishing creates a new revision and switches current public pointer atomically. Restore copies an old revision into a new draft, not directly public.
- `WikiCategories` (one required primary category), `WikiTags` (optional tags), `WikiMedia` and revision/media references. `AspNetUsers`/roles under Identity.
- Slugs uniquely reserved case-insensitively (including unpublished records) and never silently reassigned. Use UUIDs serialized as strings, UTC datetimes, indexes/constraints and controlled migrations.
- Public listing/search and article detail **only** use current published revisions; admin can access drafts and audit/history by role. Unpublishing removes public visibility and invalidates caches without deleting history.
- Images: private upload with authenticated preview; public route serves only media currently referenced by public revision; never expose a static file directory containing all drafts.
- An optional audit record stores privileged editorial action and actor, time, target, outcome for investigation. Do not store plaintext secrets.

## Interaction / data flows

1. Admin authenticates, creates article draft with title, slug, summary, category, Markdown and optional tags/media; backend validates and persists.
2. Preview requires auth; public API returns 404 for never-published/unpublished article. Search excludes it.
3. Publish verifies latest edit precondition and references, snapshots complete revision in transaction, changes published pointer and returns new public version. A stale editor receives a conflict/precondition response with no partial update.
4. Public Next.js Wiki and Flutter load same REST published revision. Public caching never exposes admin response or stale draft; refresh after publish/unpublish is bounded.
5. Subsequent admin draft edits do not change live revision. Restore creates draft; only a subsequent publish changes public state.

## Error handling and testing

- Successful reads 200; create 201; validation 400; unauthenticated 401; forbidden 403; not found 404; unique slug 409; missing edit precondition 428; stale edit precondition 412. Return RFC 9457 Problem Details with safe messages, validation field errors and correlation ID; avoid exposing sensitive record existence where unauthorized.
- Unit: slug rules, state transitions, revision creation/restore, validation.
- Integration against SQL Server: unique constraint, revision transaction, race/stale ETag behavior, published-only queries, auth and media access. Do not substitute EF InMemory as proof for relational concurrency.
- Web: editor form, draft/preview, responsive navigation, safe Markdown rendering and SEO metadata. Flutter: public category, list, search, article display on Android. Contract test matches OpenAPI fixtures. E2E: seed -> create -> private -> publish -> web/mobile -> edit -> prior revision remains live -> republish -> unpublish.
- Only declare a build/test/deployment successful after running it against actual code and recording results.

## Deployment and rollback

Build staging artifacts, run migrations against backed-up staging DB, smoke-test role guards/public read/media/search, then enable production behind TLS with backed-up SQL Server volume. Store upload media in persistent private storage; separately validate restore procedure. On deployment regression, roll back app version if schema backward compatible; migrations require an explicit forward-fix or rehearsed database restoration plan. Never deploy directly to Minecraft plugin DB.

## Risks and mitigations

- Scope creep -> Wiki CMS as first vertical slice; news and bookmarks separately specced; never block the course demo on gameplay integration.
- Source/convention mismatch -> audit actual repository and reconcile docs before scaffolding.
- App SEO/caching -> Next.js server rendering and conservative cache expiration; verify draft leakage and publish freshness.
- Upload XSS/path traversal -> content sniffing, size limits, sanitized rendering, random storage names, guarded media gateway.
- Two editors clobber each other -> ETag/rowversion checked transactionally and explicit retry UX.
- Overengineering -> modular monolith, no speculative broker/microservices, add abstraction only with clear use.

## Review gates / definition of done

Architecture accepted -> written spec review (now) -> Superpowers writing-plans + OpenSpec `tasks.md` -> incremental TDD/implementation -> build and tests -> course demo -> production-readiness checklist. A document or checklist is not evidence that its software exists.

## External facts to obtain before implementation

Actual project repository and conventions (if already started), official capstone rubric/deadline, Nova brand asset licenses, domain/hosting budget. They cannot be asserted as known here. These influence schedule/deployment choice, not the already agreed product role and MVP boundaries.
