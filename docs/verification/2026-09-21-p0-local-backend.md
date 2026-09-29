# P0 local backend verification — 2026-09-21

## Scope

This verification used only a disposable SQL Server LocalDB database named `NovaHaven_Local`. No production database, remote SQL Server, Docker container, or Minecraft/plugin database was used.

## Toolchain evidence

- Node `v24.16.0`, npm `11.13.0`.
- .NET SDK `10.0.302`, runtime `10.0.10`.
- `dotnet-ef` was updated from `10.0.9` to `10.0.12` to match the EF Core package references.
- Flutter `3.44.1`, Dart `3.12.1` from `D:\flutter`; the SDK cache required elevated local permissions because it is owned by another Windows user.
- Docker CLI was not available. SQL Server LocalDB `MSSQLLocalDB` was created and started locally.

## Commands and results

| Command | Result |
| --- | --- |
| `npm test` | PASS — 36 tests, 0 failures |
| `dotnet restore backend/NovaHaven.Api/NovaHaven.Api.csproj` | PASS after allowing access to the machine NuGet config |
| `dotnet build backend/NovaHaven.Api/NovaHaven.Api.csproj --no-restore` | PASS — 0 warnings, 0 errors |
| `dotnet test tests/backend/NovaHaven.Domain.Tests/NovaHaven.Domain.Tests.csproj --no-restore` | PASS — 20/20 |
| `dotnet ef migrations add InitialWiki ...` | PASS — generated migration, designer and model snapshot |
| `dotnet ef database update ...` | PASS — applied `20260921042138_InitialWiki` to `NovaHaven_Local` |
| `npm run smoke:wiki` against `http://127.0.0.1:5080` | PASS — Admin login/CSRF, category race, tags, draft privacy, publish, isolated edit, stale ETag, republish, restore, unpublish 404 |
| Anonymous `GET /api/v1/admin/wiki/categories` against LocalDB API | PASS — HTTP 401; server log shows Admin role authorization challenge |
| `apps/web: npm run typecheck` | PASS |
| `apps/web: npm run build` | PASS — Next.js 16.3.3 routes generated |
| `apps/mobile: flutter pub get` | PASS |
| `apps/mobile: flutter test` | PASS — 4 tests |
| `apps/mobile: flutter analyze` | PASS — no issues |

## Source changes made

- Added `Microsoft.EntityFrameworkCore.Design` `10.0.12` to the API startup project so EF CLI can construct the design-time host.
- Added the generated `InitialWiki` migration and model snapshot under `backend/NovaHaven.Infrastructure/Data/Migrations`.
- Fixed the Next.js Markdown image component to narrow `string | Blob` before calling `startsWith`.
- Fixed the Flutter UTF-8 mock response fixture and rewrote the Wiki home dynamic content list into analyzer-valid Dart without changing the reader behavior.

## Superseding follow-up

The completion batch after this P0 note added and verified media, audit, revision UI, News and Flutter bookmarks. See [2026-09-21-complete-platform.md](/D:/nova-haven-handoff/docs/verification/2026-09-21-complete-platform.md) for the current evidence. Remaining gates are Docker parity, Android APK, browser E2E, repeated concurrency stress and production hardening.
