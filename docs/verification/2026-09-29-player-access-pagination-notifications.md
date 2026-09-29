# Local account, pagination and notification verification (2026-09-29)

## Delivered

- Removed email-confirmation, resend and development-mailbox routes/services/UI. New registration creates an immediately usable standard account and signs it in; existing unconfirmed Identity accounts can sign in. Account/session remains the existing HttpOnly Identity cookie plus CSRF, not browser localStorage. Admin role is not granted on registration.
- Added bounded API inbox paging (`page`, `pageSize`, `total`, full `unreadCount`) and the authenticated unread-count endpoint. Public News, Catalog, Recipes, Knowledge categories, Community categories, Rewards and Commerce now render previous/next navigation; Catalog preserves active filters. Web and Flutter inboxes page through the same API.
- Added the accessible responsive navigation bell. It requests only the authenticated user's unread count, hides the badge for anonymous sessions, refreshes on focus/visibility and at a bounded interval.
- Admin publication of Wiki, News, Catalog items/recipes, Knowledge, Community, Rewards and Commerce stages notices for registered accounts in the same EF save/transaction as publication. Push is attempted after commit. Draft edits do not call the publisher. Expired subscriptions are removed; push failure does not erase the durable inbox row.
- Reused the existing SQL Server `UserNotifications` and `PushSubscriptions` schema; no new migration was necessary. Development Identity Data Protection keys now live under ignored `.local/data-protection-keys` and use Windows DPAPI when available.

## Local database operation

- Target was explicitly verified as `(localdb)\MSSQLLocalDB` / `NovaHaven_Local`, online. Before migration it had 11 migrations through `20260928164302_AddLocalDemoCommerce`, 6 users and 3 Wiki articles.
- Created `.local/localdb-backups/NovaHaven_Local_before_20260929_notifications.bak` with `COPY_ONLY` and `CHECKSUM`; `RESTORE VERIFYONLY ... WITH CHECKSUM` reported a valid backup.
- Applied existing migration `20260929074952_AddUserNotifications`. Read-only post-check reported 12 migrations with that migration latest, both notification tables present, and the same 6 users / 3 Wiki articles.
- The backup and local DPAPI/VAPID material are under ignored `.local/`; they are not source-controlled or sent externally.

## Verification evidence

- `dotnet build backend/NovaHaven.Api/NovaHaven.Api.csproj --no-restore` — passed, 0 warnings/errors.
- `dotnet build tests/backend/NovaHaven.Integration.Tests/NovaHaven.Integration.Tests.csproj --configuration FeatureTests --no-restore` — passed.
- `dotnet vstest tests/backend/NovaHaven.Integration.Tests/bin/FeatureTests/net10.0/NovaHaven.Integration.Tests.dll` — **62 passed, 0 failed, 0 skipped**, SQL Server LocalDB disposable test databases.
- `npm test` — **157 passed, 0 failed, 4 skipped**.
- `npm run typecheck` in `apps/web` — passed.
- `NOVA_NEXT_DIST_DIR=.next-verify npm run build` in `apps/web` — optimized production build passed; all 20 static pages generated. Build output is isolated from the running `.next` development server.
- `D:\flutter\bin\flutter.bat test --no-pub` — **25 passed**; `flutter analyze --no-pub` — no issues.
- `D:\flutter\bin\flutter.bat build apk --debug --no-pub --split-per-abi` — passed for `armeabi-v7a`, `arm64-v8a` and `x86_64`. Existing universal `app-debug.apk` was not overwritten.
- Local HTTP smoke: API `/health` returned `ok`; SQL-backed public News returned page 1 with 1 item / total 1; anonymous push-config request returned 401. `/`, `/news`, `/catalog`, `/community`, `/commerce`, `/notifications`, `/account` each returned HTTP 200 from Next on port 3002.

## Limits still explicit

- The push publisher and SQL persistence are integration-tested with a deterministic test gateway that simulates provider failure; VAPID generation/reuse and subscription CRUD are tested. A push was **not** sent to a real external browser/provider endpoint, so delivery through an actual push service remains unverified. No mobile-native FCM push is implemented.
- The API and web preview are currently running locally at `http://127.0.0.1:5080` and `http://127.0.0.1:3002`. LocalDB access required the elevated local API process in this sandbox; no production or remote database was used.
- No deployment or commit/push was performed.
