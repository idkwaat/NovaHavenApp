# Player Access, Pagination and Notifications Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Make local player accounts usable without email, expose safe pagination throughout public collections and inbox, and deliver persisted admin-publication notices with a working local Web Push path after SQL Server is updated.

**Architecture:** Preserve ASP.NET Core Identity, the modular monolith, same-origin Next API calls, Flutter's secure cookie store, EF Core/SQL Server and the existing API envelope. Publication writes inbox rows in the same EF save/transaction as the published revision; push is a best-effort side effect after commit. Existing ETag/row-version checks reject stale duplicate publication attempts.

**Tech Stack:** .NET 10, EF Core 10, SQL Server LocalDB, Next.js 16/React 19, Flutter, Node tests, OpenAPI 3.1.

**Spec:** `openspec/changes/finish-local-player-access-and-notifications/`

## Global Constraints

- SQL Server is the only database; all work remains local and no production deploy is performed.
- Public Wiki reads stay anonymous and published-only; all account/admin mutations keep CSRF and role protections.
- Registration never grants Admin and does not require or send email.
- API errors use Problem Details; identifiers remain UUID strings and times UTC.
- Preserve the current design system and the four existing unstaged UI changes.

## Review Focus

- Existing unconfirmed Identity accounts must be able to log in after the email gate is removed.
- Very large page values must be rejected before EF Skip overflow or SQL errors.
- Stale/rejected publication attempts must not create per-user notices; drafts must never notify.
- A failed/expired push subscription must not undo the committed inbox row.
- Guest nav must never query or expose another user's unread count; bell stays operable at mobile width and with keyboard/screen readers.

---

### Task 1: Local player authentication without email

**Files:** `AuthEndpoints.cs`, `Program.cs`, account email service files, `UserAccountApiIntegrationTests.cs`, OpenAPI account paths.

**Interfaces:** Produces immediate registration response (201), authenticated cookie login (204) and current-user profile; removes confirm/resend/mailbox API routes and sender/outbox interfaces.

- [x] Write integration tests proving register→login→`/me` succeeds without mail, user has no Admin role, unconfirmed legacy login succeeds, invalid credentials/CSRF fail.
- [x] Run the focused tests and record their expected failure before implementation.
- [x] Remove confirmation endpoints/services/DI/configuration, set registration account usable, disable confirmed-email gate and return useful RFC 9457 validation/conflict responses.
- [x] Update contract and run focused integration plus API build.

### Task 2: Web and Flutter account parity

**Files:** `AccountClient.tsx`, mobile account API/screens/README/tests, `tests/web/auth-notifications-contract.test.mjs`.

**Interfaces:** Both clients call only register/login/me/logout; registration reports an actionable success and login reports API Problem Details, with no mail/confirmation controls.

- [x] Add failing web/mobile assertions for the no-email flow and removed endpoints.
- [x] Implement the clients and remove obsolete mail instructions/tests.
- [x] Run web contract tests/typecheck and Flutter test/analyze if the SDK is available.

### Task 3: API and client pagination

**Files:** public collection endpoint maps, notification endpoint, shared web pagination component, News/Catalog/Recipe/Knowledge/Community/Rewards/Commerce pages and API clients, Flutter inbox API/screen/tests, OpenAPI.

**Interfaces:** Public list API maintains `{items,page,pageSize,total}`; notifications use the same fields plus `unreadCount`; query arguments are `page` and `pageSize` with the established max of 50.

- [x] Add API integration tests for page boundaries, notification isolation/count, read ownership and rejected overflow.
- [x] Add source-level UI contract tests proving list pages preserve filters and render previous/next controls.
- [x] Implement bounded offset validation and visible pagination for collection pages; add inbox pagination in web and Flutter.
- [x] Run targeted tests, full Node suite, .NET build/integration and web typecheck/build.

### Task 4: Publication notifications and nav bell

**Files:** publication endpoints/controllers, bell client, navigation styles, inbox client, LocalDB integration tests, OpenAPI. Reuse the existing `UserNotifications` and `PushSubscriptions` SQL schema.

**Interfaces:** Publisher stages title/body/same-site path notices for registered users before the publication save and attempts push only after commit; bell refreshes unread count from the authenticated inbox API.

- [x] Add integration coverage for News publication and push-provider failure preserving the durable inbox; publication handlers stage notices in the publication save/transaction.
- [x] Reuse the existing notification schema; no new database migration was needed. Stage notices for Wiki, News, Catalog/Recipe, Knowledge, Community, Rewards and Commerce publication.
- [x] Add bell badge, accessible count announcement and responsive keyboard/touch behavior.
- [x] Verify the existing migration on disposable LocalDB before applying it to `NovaHaven_Local`.

### Task 5: Local SQL Server and Web Push acceptance

**Files:** local verification evidence, README, roadmap if applicable; local database only.

- [x] Read the target without printing secrets; verify `NovaHaven_Local` and inspect pending migrations read-only.
- [x] Create a checksum-verified backup under ignored `.local/localdb-backups/`, apply the existing notification migration and verify EF history/tables plus existing data counts.
- [x] Start API against the local DB and smoke-test health/public News; integration tests exercise register/login, inbox pagination/read ownership, publication notices, VAPID generation, subscription CRUD and simulated provider failure.
- [x] Run Node/web/.NET/Flutter tests and builds; document real-provider/device limitations explicitly.

**Review:** Final verification ran after the changes. Real delivery to an external browser push provider and deployment remain unverified. Do not commit or push without verified state.
