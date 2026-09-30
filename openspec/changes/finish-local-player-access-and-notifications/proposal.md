# Finish local player access and notifications

## Why

Player registration currently cannot complete without an email sender, unconfirmed accounts cannot sign in, collection APIs expose pagination that most screens do not use, and notification behavior needs verification against the project's local PostgreSQL migration baseline.

## Scope

- Remove confirmation-email delivery and confirmation-only routes. Let players register and sign in immediately while retaining Identity password policy, lockout, cookie, CSRF and Admin-role boundaries; allow existing unconfirmed users to sign in too.
- Give public content collections and the signed-in notification inbox bounded, deterministic pagination, and expose next/previous controls that preserve filters. Keep the web and Flutter inbox contract aligned.
- Show a notification bell with the signed-in user's unread count. Persist deduplicated notifications for newly published News and Wiki revisions; never notify on drafts. Persist inbox rows before attempting optional Web Push.
- Verify the PostgreSQL notification schema through disposable integration databases only. Never back up, migrate, seed or otherwise mutate the owner's populated local content database as part of this change. Keep all data local; do not introduce SQLite, SMTP, external identity providers or production deployment.
- Exercise VAPID/Web Push only after the PostgreSQL notification schema is verified. Clearly report when local VAPID configuration is absent; external provider delivery is not implied by local tests.

## Out of scope

- Native FCM push for Flutter, production hosting, external mail, payment processing, and new Minecraft gameplay integrations.
- Replacing the existing modular-monolith/API architecture or adding a broker/background service.
