# User accounts, notifications and Web Push — local verification

Date: 2026-09-29  
Scope: local Nova Haven source only. SQL Server remains the only application database provider; no SQLite provider was added and no database was migrated by this batch.

## Implemented

- Identity player registration, email confirmation, resend confirmation, login, current-user and logout endpoints. Login requires confirmed email; new accounts are never assigned the Admin role.
- Development-only local email outbox, optional SMTP sender, and same-origin confirmation UI at `/account`.
- Per-user SQL-backed notification inbox/read state, Admin broadcast, VAPID public key and browser push subscription endpoints, provider endpoint allowlist, stale subscription cleanup, and service worker click/open behavior.
- Next.js account, inbox, Web Push controls and Admin notification broadcast UI.
- Flutter account registration/login/email-confirmation and notification inbox; Identity and antiforgery cookies use `flutter_secure_storage`.
- SQL Server migration `20260929074952_AddUserNotifications`, with `UserNotifications`, `PushSubscriptions`, Identity foreign keys and query indexes. Push endpoints keep their full value for delivery and use a 64-character SHA-256 fingerprint for the unique SQL Server index.

## Verification evidence

| Check | Result |
| --- | --- |
| API build, `FeatureMigration` configuration | Passed, 0 warnings, 0 errors |
| EF `has-pending-model-changes` after migration | Passed: no changes since last migration |
| Domain tests | 78/78 passed |
| Push endpoint allowlist tests | 7/7 passed |
| Full Node suite | 153 passed, 4 skipped, 0 failed (157 total) |
| Next.js TypeScript check | Passed |
| Next.js production build to isolated `.next-feature-auth` | Passed; `/account` and `/notifications` included |
| User-account SQL integration | Blocked before endpoint assertions: LocalDB error 50, “Cannot create an automatic instance” |
| Flutter test/analyze | Not run: Flutter and Dart SDK are not installed/on PATH in this execution environment |
| Real SMTP/Web Push delivery | Not exercised; no SMTP credentials or external push service credentials are configured |

The SQL integration attempt first exposed a Minimal API binding error on `DELETE /notifications/push/subscriptions` (inferred body not supported); this was corrected with an explicit body binding. A schema review also replaced the oversized unique endpoint index with a fixed-width SHA-256 fingerprint. The rerun starts the HTTP host and reaches `LocalApiFactory.InitializeAsync`'s SQL migration step, where LocalDB fails to start. A diagnostic run suppressed the test host's Windows Event Log provider so the underlying failure was visible. No migration was applied and no DB was created or modified.

## Local next steps

1. Start a local SQL Server/LocalDB instance under the same Windows account that runs the API, use a disposable local database for the first migration, then run the account integration test again.
2. Run API in `Development`; local email confirmations are available from the account screen's local mailbox control. Development VAPID keys are stored only under ignored `.local/`.
3. Run `npm run dev` for Next and open `/account`, create a test account, confirm it from the local outbox, then visit `/notifications`.
4. Run Flutter `pub get`, `test` and `analyze` on a machine with Flutter SDK. Native mobile push remains out of scope until a mobile push provider is selected/configured; the mobile inbox is backed by the same API.
