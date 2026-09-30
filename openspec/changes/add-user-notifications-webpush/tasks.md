# Implementation tasks

- [x] Add SQL entities/configuration/migration for notification and push-subscription persistence. `20260929074952_AddUserNotifications` is verified through disposable SQL Server integration fixtures; intentionally not applied to `NovaHaven_Local`.
- [x] Add account register/confirm/resend/login/current-user API, local development mail outbox and optional SMTP sender.
- [x] Add user-scoped notification inbox/read endpoints, Admin announcement endpoint, VAPID config/subscription/delivery service and OpenAPI contract.
- [x] Add Next.js account, confirmation, inbox and push-permission UI plus service worker.
- [x] Add Flutter secure cookie/CSRF account client and account/inbox screens.
- [x] Run Node tests, .NET build/domain/integration, web typecheck/build and Flutter test/analyze/build; update README, roadmap and verification evidence.

Verification summary (2026-09-29): Node 154 passed/4 skipped; API build 0 warnings/errors; EF model has no pending changes; domain 78/78; SQL Server LocalDB integration 58/58; Next typecheck/production build passed; Flutter test 30/30, analyze clean and debug APK build passed. The migration was not applied to the existing `NovaHaven_Local` database (11/12 migrations). The preview route smoke did not run with the API, and real SMTP/Web Push, emulator install/launch and authenticated browser E2E remain unverified. Detailed evidence: `docs/verification/2026-09-29-local-platform-hardening.md`.
