# Implementation tasks

- [x] Remove email-confirmation dependencies and make registration/login work for new and legacy users.
- [x] Update the web and Flutter account flows and the OpenAPI contract; remove confirmation-mail UI/API references.
- [x] Add safe pagination to public collections and notification inbox; add accessible web controls and Flutter inbox paging.
- [x] Persist publication notices for content published by Admin and add the responsive nav bell/unread count.
- [x] Verify the PostgreSQL notification migration on disposable integration databases; never migrate, seed or modify the owner's populated local content database.
- [x] Verify VAPID validation/subscription rules and failure-safe inbox behavior against the PostgreSQL schema; run API, web and mobile checks and update docs. External push-provider delivery remains unverified.
