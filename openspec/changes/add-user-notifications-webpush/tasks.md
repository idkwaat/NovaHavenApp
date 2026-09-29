# Implementation tasks

- [x] Add SQL entities/configuration/migration for notification and push-subscription persistence. Migration generated: `20260929074952_AddUserNotifications`; not applied.
- [x] Add account register/confirm/resend/login/current-user API, local development mail outbox and optional SMTP sender.
- [x] Add user-scoped notification inbox/read endpoints, Admin announcement endpoint, VAPID config/subscription/delivery service and OpenAPI contract.
- [x] Add Next.js account, confirmation, inbox and push-permission UI plus service worker.
- [x] Add Flutter secure cookie/CSRF account client and account/inbox screens.
- [ ] Run Node tests, .NET build/domain/integration where LocalDB is available, web typecheck/build and Flutter test/analyze where SDK is available; update README, roadmap and verification evidence.

Verification summary: Node 153 passed/4 skipped; API build succeeded with 0 warnings/errors; EF model has no pending changes; domain 78/78; push endpoint rules 7/7; Next typecheck and production build passed. SQL LocalDB and Flutter gates remain blocked by missing local runtime/SDK. Detailed evidence: `docs/verification/2026-09-29-user-notifications-webpush.md`.
