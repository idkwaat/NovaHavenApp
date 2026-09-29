# Add user accounts, notifications and Web Push

> **Superseded:** This is the historical proposal that required email confirmation. The current local account/no-email behavior, pagination and notification scope are defined by [`finish-local-player-access-and-notifications`](../finish-local-player-access-and-notifications/proposal.md); do not treat this document as the active implementation contract.

Nova Haven currently has Identity-backed Admin cookie authentication but no player account lifecycle, notification inbox or browser push subscription. This change adds those capabilities inside the existing ASP.NET Core + SQL Server modular monolith and keeps all state local.

## Scope

- User registration, email confirmation, sign-in, sign-out and current-user profile; registration never grants Admin.
- Local development email outbox when SMTP is not configured; configured SMTP can deliver confirmation messages.
- Per-user notification inbox, read state and Admin-only announcement broadcast.
- Optional standards-based Web Push using configured VAPID keys; browser push is explicitly unavailable until configured.
- Next.js account/inbox/push controls and Flutter account/inbox using the same API contract.

No external identity provider, broker, hosted deployment, FCM setup or real email credentials are introduced.
