# Design

- Reuse ASP.NET Core Identity and the existing HttpOnly cookie + antiforgery pattern. User login is separate from Admin authorization; only the explicit Admin role policy grants CMS access.
- Store notifications and Web Push subscriptions in the current NovaDbContext and SQL Server migration chain. Each read/write query is scoped to the authenticated user's UUID; Admin broadcast is Admin-only and CSRF-protected.
- Confirmation messages use a configured SMTP transport when available. Development without SMTP writes a short-lived confirmation message into the ignored local `.local` outbox, retrievable only from Development endpoints; it is not an email-delivery claim.
- Web Push uses browser Push API subscriptions and RFC 8291 payload encryption plus VAPID ES256. Private VAPID key stays server-side in environment configuration. Restrict subscription endpoints to HTTPS and known push-service hosts; remove expired endpoints on 404/410.
- Keep web API calls same-origin through the existing `/api` rewrite. Flutter uses the same cookie/CSRF API flow and persists the opaque cookie only in platform secure storage. Native Firebase push is out of scope without platform credentials; mobile receives the same persistent inbox.
- Notification delivery is best-effort after persistence in this local MVP; failed push delivery never rolls back the durable inbox item. No broker or distributed worker is added.
