# Agent execution contract — Nova Haven Platform

1. Read product scope, architecture design and the relevant OpenSpec change **before editing code**. If an existing repository is supplied, inspect actual folders, naming, dependency injection, error handling and migrations first; actual repo convention wins over guesses.
2. Do not claim code, tests, deployment or source audit occurred unless tool evidence exists. Never describe proposed specs as released capabilities.
3. Keep the product a modular monolith: Next.js website/admin, Flutter reader, ASP.NET Core REST API, SQL Server. Do not introduce a microservice, broker, GraphQL, separate CMS product or direct Minecraft database writes without an approved change.
4. One source of truth for behavior: OpenSpec `specs/` for shipped behavior, `changes/` for proposed behavior; one OpenAPI contract for clients. Design contains HOW; spec contains observable WHAT; tasks contain execution steps. No duplicated conflicting specs.
5. Minecraft plugin gameplay authority stays in Java plugins. No fabricated plugin endpoints, custom combat/quest/economy logic, or access to plugin SQL schemas without reading the real source and explicit contract.
6. Public Wiki read must be anonymous and published-only. Admin mutation must check server-side authorization, antiforgery where cookie auth is used, content sanitization and concurrency. Never store long-lived secrets in browser localStorage. Do not commit secrets.
7. Changes should be vertical slices with tests: persistence + API + web + mobile where scope requires. Add only minimal shared abstractions; avoid empty interfaces, giant utils and boilerplate repositories.
8. Keep identifiers and API conventions consistent. Expose UUIDs as strings in JSON; use UTC timestamps; return a consistent RFC 9457 Problem Details representation for errors; use migrations rather than ad hoc schema edits.
9. Before claiming done: run relevant tests/build and explain any inability to run them. Include a reproducible demo data path, README and local startup commands at delivery.
10. Review spec with owner before writing implementation tasks. Use feature branches or worktrees for coding; resolve API/schema changes in contract first; avoid parallel conflicting database migrations.

## UI/UX execution

- Read `design-system/nova-haven/MASTER.md` before changing public UI. Preserve the owner-approved homepage direction and existing page-specific compositions.
- If `ui-ux-pro-max` is available in the active agent environment, use its design-system/search workflow and the detected stack guidance. If unavailable, follow the manual baseline and disclose that its local search catalog was not run.
- For web UI changes, verify WCAG AA text contrast, visible keyboard focus, 44px touch targets, reduced motion, Vietnamese text wrapping, and mobile overflow.
