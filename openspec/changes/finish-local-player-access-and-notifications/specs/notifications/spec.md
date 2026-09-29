# Player notifications and Web Push

## Requirements

- An authenticated player can page through only their own notifications, mark one read and mark all read. Another player's item is indistinguishable from not found. Inbox returns page, pageSize, total and the player's full unreadCount.
- The navigation bell links to the inbox and shows the signed-in player's unread count without exposing data to anonymous visitors.
- Publishing public content from Admin (Wiki, News, catalog/recipes, knowledge, community, rewards or commerce) creates one persisted notification per registered account in the same database save/transaction as publication, with a same-site destination. Draft saves create no notification. A rejected stale publish request creates no notification.
- Admin announcements are validated, Admin-only and CSRF-protected; registered players do not need email confirmation to receive them.
- Inbox rows are saved before optional Web Push is attempted. Push unavailability/failure never removes inbox data; expired subscriptions are removed. Private account data is not included in push payloads.
- Browser push is available only with valid server-side VAPID configuration and a successfully migrated local PostgreSQL schema. No SQLite database or SMTP dependency is introduced.
- Flutter uses the same paginated inbox API; native push is not claimed without an FCM implementation.

## Scenarios

### Scenario: Published content notice

- **WHEN** an Admin publishes a public content revision
- **THEN** registered accounts receive one persisted notice linking to that public content, while drafts remain invisible

### Scenario: Read only own inbox page

- **WHEN** a signed-in player requests a page or marks an item read
- **THEN** only that player's notifications and unread count are returned or changed

### Scenario: Push disabled or delivery fails

- **WHEN** VAPID is absent or a push endpoint rejects delivery
- **THEN** the inbox notification remains stored and the API reports push availability honestly
