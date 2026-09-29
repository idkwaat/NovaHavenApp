# User Accounts, Notifications and Web Push Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver a local, testable player account and notification experience shared by the web and Flutter clients, with optional standards-based browser push.

**Architecture:** Extend the existing ASP.NET Identity cookie flow, modular EF Core context and same-origin API. Persist user notifications and subscriptions in the local SQL Server database; use SMTP when configured and a Development-only file outbox otherwise. Keep Web Push private-key material server-side and mobile authentication cookies only in secure storage.

**Tech Stack:** ASP.NET Core .NET 10, Identity, EF Core SQL Server, Next.js 16/React 19, Flutter, OpenAPI 3.1, Node/xUnit/Flutter tests.

**Spec:** `openspec/changes/add-user-notifications-webpush/`

## Global Constraints

- All state remains in the existing local Nova Haven modular monolith and SQL Server.
- Anonymous Wiki reads stay anonymous and published-only; Admin routes still require the Admin role.
- Every cookie-authenticated mutation requires antiforgery validation.
- No long-lived secrets in browser localStorage; mobile session material uses secure storage.
- No real SMTP delivery or Web Push claim without local credentials/configuration; never commit secrets.
- Every behavior change follows a failing-test-first cycle; generated migration is reviewed and never applied to an unrelated database.

## Review Focus

- Unconfirmed email must not authenticate; confirmation tokens must be single-use/expiring under Identity semantics.
- A standard registration must not gain Admin, and user cookies must not bypass Admin policy.
- Notification IDs from another user must not reveal or mutate that user's records.
- Invalid or non-HTTPS push endpoints must be rejected before any outbound request.
- Missing VAPID configuration must give a clear unsupported state, not a false success.

---

### Task 1: Backend account lifecycle and local email

**Files:** `backend/NovaHaven.Api/Endpoints/AuthEndpoints.cs`, new local mail sender service, `Program.cs`, integration tests, OpenAPI contract.

**Interfaces:** Account routes `POST /api/v1/auth/register`, `POST /api/v1/auth/confirm-email`, `POST /api/v1/auth/resend-confirmation`, and authenticated `GET /api/v1/auth/me`; retain current login/logout and Admin role behavior.

- [ ] Add integration tests for registration/confirmation/login, CSRF, duplicate email, and standard-user Admin denial; verify each fails against current source.
- [ ] Implement Identity email tokens, configurable SMTP/local Development outbox, and profile endpoint.
- [ ] Run the targeted integration tests and API build.

### Task 2: Notification persistence and delivery API

**Files:** Domain notification entities, Infrastructure EF configurations/context/migration, Application service/repository boundaries, API endpoints/provider, integration tests, OpenAPI contract.

**Interfaces:** `GET /api/v1/notifications`, `POST /api/v1/notifications/{id}/read`, `POST /api/v1/notifications/read-all`; Admin `POST /api/v1/admin/notifications`; user push config/subscription routes under `/api/v1/notifications/push`.

- [ ] Add failing tests for owner scoping, read-state changes, Admin-only broadcast and HTTPS endpoint validation.
- [ ] Implement persistence and APIs, then generate and inspect the additive EF migration.
- [ ] Implement optional RFC 8291/VAPID push, HTTPS host allowlist, expired-subscription cleanup, and a no-keys unsupported response.
- [ ] Run integration tests on an isolated local SQL Server database if available; never target `NovaHaven_Local` implicitly.

### Task 3: Next.js account, inbox and Web Push

**Files:** `apps/web/app/account/**`, `apps/web/app/notifications/**`, `apps/web/public/sw.js`, navigation/styles, Node tests.

- [ ] Add failing API-client and accessible account/inbox behavior tests.
- [ ] Implement responsive sign-up/sign-in/confirmation, notification list/read actions and explicit user-click browser push opt-in using existing Nova UI tokens.
- [ ] Verify Node tests, typecheck and production build; inspect mobile wrapping/focus/targets.

### Task 4: Flutter shared account and notification inbox

**Files:** `apps/mobile/lib/auth_session_store.dart`, `wiki_api.dart`, `main.dart`, account/notification UI and tests.

- [ ] Add failing tests for CSRF/cookie persistence in secure storage, account APIs and notification parsing/read flow.
- [ ] Implement cookie/CSRF request support and accessible account/inbox screens matching the web API behavior.
- [ ] Run Flutter tests/analyze/build if Flutter SDK is available; report absent FCM credentials as no native-push support.

### Task 5: Local runbook and evidence

**Files:** `.env.example`, `README.md`, roadmap and date-stamped verification report.

- [ ] Document LocalDB migration, local mail outbox, SMTP/VAPID configuration and web/mobile startup steps.
- [ ] Run all available focused suites and record exact pass/fail/block status.
- [ ] Mark only verified tasks/spec behavior complete.
