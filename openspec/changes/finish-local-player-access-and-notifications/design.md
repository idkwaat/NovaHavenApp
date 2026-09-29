# Design

- Reuse ASP.NET Core Identity's existing PostgreSQL store and HttpOnly cookie. Registration marks the local account usable immediately; account creation never assigns Admin. Remove sender/outbox dependencies rather than replacing SMTP with another mail path.
- Keep the established `{ items, page, pageSize, total }` collection contract. Validate page/pageSize before computing offsets and use deterministic secondary ordering. Inbox responses additionally include total and the user's unreadCount.
- Use one PostgreSQL-backed notification row per recipient and publish event key, guarded by a unique index for retry safety. Only successful News publication and Wiki publication of a revision produce automatic content notices. Persist notices before best-effort push; delivery failure must not roll back inbox data.
- Keep the API as the source of unread state. A small client-side nav bell loads only the current user's unread count, refreshes on focus and at a bounded interval, and links to the paginated inbox. Unauthenticated visitors see the same link without private data.
- Use the current account, API error and design-system conventions in web and Flutter. OpenAPI remains the client contract. Verify migrations on disposable PostgreSQL databases; do not migrate or seed an existing populated local content database.
