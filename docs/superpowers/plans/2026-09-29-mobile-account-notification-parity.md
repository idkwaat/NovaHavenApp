# Mobile Account and Notification Parity Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bring Flutter player-account confirmation and notification-inbox flows to the same usable account lifecycle as the web while preserving the existing API and SQL Server contract.

**Architecture:** Reuse the existing ASP.NET Identity cookie/CSRF endpoints and secure-storage-backed Flutter API client. Add a Development-only outbox reader to the mobile client and make its result usable for email confirmation; make an unauthenticated inbox offer a direct account path. Do not add a mobile push provider or change the backend schema/API contract.

**Tech Stack:** Flutter/Dart, `http`, `flutter_secure_storage`, `flutter_test`; ASP.NET Core/SQL Server and Next.js are regression gates only.

**Spec:** `openspec/changes/add-user-notifications-webpush/specs/user-accounts/spec.md`, `openspec/changes/add-user-notifications-webpush/specs/notifications/spec.md`, and `openspec/changes/add-user-notifications-webpush/design.md`.

## Global Constraints

- Keep SQL Server as the sole application database provider; do not add SQLite.
- Use the existing Identity HttpOnly cookie and `X-CSRF-TOKEN` flow; keep cookies only in `flutter_secure_storage`.
- Mobile supports player account registration/confirmation/login and the same persistent inbox; Web Push remains browser-only until a mobile push provider is separately configured.
- Preserve Nova Haven dark-earth tokens, Vietnamese copy, password-manager autofill, and minimum 48dp Android controls.
- Do not apply migrations or mutate any existing local database during this parity pass.

## Review Focus

- Cookie storage must compile and survive login, then be removed on logout — cover login/logout through the real API client with an in-memory secure-store test double.
- A missing/empty Development outbox must not fabricate a confirmation token — cover empty, valid, malformed, and unavailable responses in account API tests.
- An unauthenticated inbox must offer login/registration instead of a dead retry loop — cover the rendered unauthorized state in a widget test.
- Confirmation must reject blank email/token locally and preserve manual paste for non-local email — cover validation and the confirm action in widget tests.
- Vietnamese content and large text must remain reachable on narrow screens — cover the account and inbox screens at 320dp and with enlarged text in widget tests.

---

### Task 1: Repair the account client and expose local confirmation mail

**Files:**
- Modify: `apps/mobile/lib/account_api.dart`
- Test: `apps/mobile/test/account_api_test.dart`

**Interfaces:**
- Consumes: existing `AccountSecretStore`, `AccountUser`, and account/notification API routes.
- Produces: `Future<LocalConfirmationMessage?> latestLocalConfirmation(String email)`; throws a localized `StateError` when the Development-only route is unavailable or malformed.

- [ ] **Step 1: Add focused failing client tests** for cookie cleanup after logout and local outbox parsing (latest message, URL-decoded token, empty list, and unavailable response).
- [ ] **Step 2: Run `dart analyze` and the focused Flutter test** to record the existing compile failure and confirm each behavioral test fails for its intended reason.
- [ ] **Step 3: Fix `AccountSecretStore` call sites** to use the declared positional interface and implement the outbox client against `/api/v1/dev/mailbox?email=...` with a bounded timeout.
- [ ] **Step 4: Run focused analysis/tests** and confirm the cookie and outbox tests pass.

### Task 2: Complete account and inbox mobile interaction

**Files:**
- Modify: `apps/mobile/lib/account_screens.dart`
- Test: `apps/mobile/test/account_screens_test.dart`

**Interfaces:**
- Consumes: `UserAccountApi.latestLocalConfirmation`, existing registration/confirmation/login/logout, and inbox/read operations.
- Produces: a local-mail confirmation action and a navigable sign-in state when inbox access returns 401.

- [ ] **Step 1: Add failing widget tests** for opening a local confirmation message, confirming its token, and navigating from an unauthorized inbox to the account screen.
- [ ] **Step 2: Run those tests** and verify the failures correspond to the missing interactions.
- [ ] **Step 3: Implement the local outbox/confirmation states, inline validation, loading/error feedback, and account CTA** with existing Nova Haven theme tokens and accessible touch targets.
- [ ] **Step 4: Run account widget tests at 320dp plus enlarged text**, then the full Flutter test and analyzer suites.

### Task 3: Final local acceptance evidence

**Files:**
- Modify: `docs/verification/2026-09-29-user-notifications-webpush.md`
- Modify: `docs/roadmap/NOVA-HAVEN-ROADMAP.md`
- Modify: `openspec/changes/add-user-notifications-webpush/tasks.md`

- [ ] **Step 1: Run full Node, .NET build/domain/integration, Next typecheck/build, and Flutter test/analyze gates.** Do not run a migration against an existing database.
- [ ] **Step 2: Record exact results and environment blockers**, explicitly separating tested code from SQL Server live integration, real SMTP/Web Push, emulator/device, and native push limitations.
- [ ] **Step 3: Mark only evidenced OpenSpec/roadmap checks complete** and retain remaining acceptance blockers.
