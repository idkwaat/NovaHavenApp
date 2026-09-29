# Nova Haven API convention and mobile verification

**Date:** 2026-09-30
**Scope:** Finish the approved Controller → Application BLL → feature repository → Infrastructure DAL conversion, validate the shared API contract, and verify local Web/Flutter toolchains. No user content database, deployment or Git history was changed.

## Implementation

- Converted Knowledge, Community, Integration capabilities, Commerce offers/orders, and Notifications/Web Push routes to thin MVC controllers with typed API contracts, Application services/commands/results and Infrastructure repositories.
- Kept `/health` as the only direct Minimal API route. Removed the unused legacy `PublicWikiEndpoints.cs` implementation and its obsolete namespace import; removed an unused Community draft-row repository member.
- Kept PostgreSQL as the local provider and did not add/change a migration. Knowledge published reads now query the current published revision in a provider-translatable shape; the PostgreSQL integration test had caught and reproduced an EF query translation failure before it was corrected.
- Updated Commerce and Wiki tag source tests to assert the new controller/service/repository locations instead of deleted endpoint files.
- Compared the route attributes on all API controllers with `contracts/openapi/wiki-v1.json`: **106 controller method/path pairs, 106 OpenAPI operations, no missing or extra route/method pairs**.

## Verification run

| Gate | Result |
| --- | --- |
| `dotnet build NovaHaven.sln --no-restore -c Debug -p:BaseOutputPath=.local/build-final/` | Passed, 0 warnings and 0 errors |
| Domain tests | 80/80 passed |
| PostgreSQL integration tests | 78/78 passed on disposable local PostgreSQL 18.4, loopback port 15436 |
| EF `migrations has-pending-model-changes` | No pending model changes |
| `node --test tests/web/*.test.mjs` | 161 passed, 5 skipped, 0 failed (166 total) |
| Next `npm run typecheck` | Passed |
| Isolated Next production build (`NOVA_NEXT_DIST_DIR=.next-final-check npm run build`) | Passed; 20/20 routes generated |
| Flutter 3.47.3 `flutter test --no-pub` | 26/26 passed (rerun after APK build) |
| Flutter 3.47.3 `flutter analyze --no-pub` | No issues found (rerun after APK build) |
| Node web suite (continuation rerun) | 161 passed, 5 skipped, 0 failed |
| Next.js typecheck (continuation rerun) | Passed |

The integration test fixture created only `NovaHaven_Integration_<32 hex>` databases on a temporary cluster; post-run inventory returned **0** such databases. The temporary cluster was stopped. A separate stale disposable test cluster from an earlier run was also stopped. No migration, seed or write was directed at the user's PostgreSQL content database or historical SQL Server database.

## Flutter / Android Studio

Android Studio was present before this work, but it does not bundle the Flutter SDK. The Flutter and Dart IDE plugins were present; the standalone Flutter SDK was absent. Flutter stable **3.47.3** is now installed under the ignored local path `D:\nova-haven-handoff\.local-tools\flutter`; this path is not committed. Flutter commands work through the full path, but the SDK has not been added to the machine-wide PATH. `flutter doctor` sees Android SDK **36.1.0**, Java **21.0.11**, and accepted Android licenses. Its network checks to pub.dev/Google Maven/GitHub are blocked in this environment; the Windows C++ workload warning is unrelated to Android builds.

An Android debug APK is **verified** using the project-pinned Gradle 9.1.0 and Java 21. The Flutter wrapper initially hit sandbox network/cache-ownership restrictions and a Windows `engine.stamp` replacement error. For the successful local build, a one-line workaround was applied temporarily to the ignored Flutter SDK, the existing stamp was backed up, and both were restored after the build; no project source or Gradle version was changed. `apps/mobile/build/app/outputs/flutter-apk/app-debug.apk` was created (158,253,220 bytes; SHA-256 `C0BB793988BA30F4DCC1C3018C9865BBF4DE09427BEB67C984581611A73D44FB`). The APK archive contains `AndroidManifest.xml` and Flutter libraries for arm64-v8a, armeabi-v7a and x86_64. No Android emulator/AVD, system image, or phone is attached, so install, launch and visual/device acceptance remain open.

## Remaining acceptance limits

- Full authenticated browser end-to-end parity and manual keyboard/screen-reader acceptance have not been run. The local web/API ports were not running during this continuation; account and notification behavior is covered by the PostgreSQL integration suite and Flutter widget/API tests, but that is not a browser E2E claim.
- Actual external Web Push delivery is not verified; native Flutter push is not part of this local MVP.
- The migration is model-consistent and passed the isolated integration suite, but this turn intentionally did not apply it to the user's existing local content database.
- No commit, push or deployment was made. The pre-existing dirty working tree was preserved.

## Final continuation audit (2026-09-30)

- Re-ran `dotnet test tests/backend/NovaHaven.Integration.Tests/NovaHaven.Integration.Tests.csproj --no-restore -c Debug` against a newly initialized PostgreSQL 18.4 cluster bound only to `127.0.0.1:15437`: **78/78 passed**. The suite applied the active PostgreSQL migration to its generated `NovaHaven_Integration_<32 hex>` database and removed that database on fixture cleanup. The temporary cluster was stopped and its exact generated `.local` directory removed. The existing PostgreSQL Windows service/default port and populated content database were not used.
- Corrected the active player-access/notifications OpenSpec from SQL Server to PostgreSQL and clarified that only disposable test databases may be migrated. Updated the architecture design's progress: all business API families use Controller → Application BLL → Infrastructure DAL; `/health` remains the lone direct mapping; Web/Mobile feature-folder organization is still unfinished.
- `git diff --check` reported no whitespace errors. No application source was changed in this continuation; browser/device acceptance, external Web Push delivery and deployment remain open as listed above.

## Commit preflight (2026-09-30)

- Fresh solution build passed with 0 warnings/errors using an isolated output directory; Domain tests **80/80**, PostgreSQL integration **78/78** (temporary loopback cluster on port 15437), Node **161 passed / 5 skipped**, Next typecheck + production build (**20/20 routes**), and Flutter tests **26/26** + analyzer clean.
- A fresh APK rebuild was attempted but Gradle could not create its cache lock under `C:\Users\Admin\.gradle` from the sandbox identity (`CodexSandboxOffline`); fallback distribution download is network-blocked. The existing APK remains present and its SHA-256 matches the previously verified artifact; this is not a fresh APK build claim.
- A fresh `dotnet ef migrations has-pending-model-changes` invocation could not complete because its implicit build was denied access to the profile NuGet config; a subsequent default-output build also encountered locked DLLs. The isolated-output solution build and PostgreSQL integration migration tests passed. No model-check success is claimed for this preflight.
- No application code or populated database was changed by these checks. Browser/device acceptance, external push delivery and deployment remain open. The local preview-generated `apps/web/next-env.d.ts` reference is deliberately excluded from the source commit.
