# Nova Haven — Local Demo and Defense Walkthrough

This guide runs the project locally. It does not deploy, connect to a public server, or touch a Minecraft plugin database. Use a **disposable local database** for the editorial lifecycle demo. Keep `NovaHaven_Local` and any personal data out of mutation-oriented rehearsals unless you deliberately choose that database yourself.

## 1. Start a disposable local SQL database

Prerequisites: Windows LocalDB (`MSSQLLocalDB`), .NET SDK 10, EF tool 10.0.12, Node.js 22+, npm and Flutter 3.44+ if demonstrating mobile.

In a PowerShell window, choose a database name that does not already exist. This example uses `NovaHaven_DefenseDemo`:

```powershell
$demoDb = 'Server=(localdb)\MSSQLLocalDB;Database=NovaHaven_DefenseDemo;Trusted_Connection=True;TrustServerCertificate=True'
$env:NOVA_DB_CONNECTION = $demoDb
$env:ConnectionStrings__NovaDb = $demoDb
dotnet ef database update --project backend/NovaHaven.Infrastructure --startup-project backend/NovaHaven.Api --context NovaDbContext
```

The database update applies the reviewed migration chain to that named local database. Do not run it against a database you intend to preserve without first taking a verified backup and reviewing pending migrations. Never use `EnsureCreated`.

## 2. Bootstrap a local-only Admin and start the API

Set a private email/password for your own demo; do not paste credentials into this document or commit them. Admin bootstrap is opt-in and Development-only:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:SeedAdmin__Enabled = 'true'
$env:SeedAdmin__Email = 'admin@local.test'
$env:SeedAdmin__Password = '<choose-a-unique-local-password>'
dotnet run --project backend/NovaHaven.Api --urls http://127.0.0.1:5080
```

After the Admin account is created, stop the API with `Ctrl+C`, clear the bootstrap variables, then start it again:

```powershell
Remove-Item Env:SeedAdmin__Enabled, Env:SeedAdmin__Email, Env:SeedAdmin__Password -ErrorAction SilentlyContinue
dotnet run --project backend/NovaHaven.Api --urls http://127.0.0.1:5080
```

The API must use the same `ConnectionStrings__NovaDb` value in this window. Check `http://127.0.0.1:5080/health` returns `{"status":"ok"}`.

## 3. Start the website

In a second PowerShell window:

```powershell
Set-Location apps/web
$env:NOVA_API_ORIGIN = 'http://127.0.0.1:5080'
npm install
npm run dev -- --hostname 127.0.0.1 --port 3001
```

Open `http://127.0.0.1:3001/`, then `/wiki`. Admin is at `/admin`; use the local-only account created above. Do not put real credentials in `.env.local` committed files or screenshots.

## 4. Optional Vietnamese demo records

The demo seed writes content. Point it only at the disposable API/database from step 1. In a third PowerShell window, set credentials in that shell and run:

```powershell
$env:NOVA_API_URL = 'http://127.0.0.1:5080'
$env:NOVA_ADMIN_EMAIL = 'admin@local.test'
$env:NOVA_ADMIN_PASSWORD = '<your-local-admin-password>'
npm run seed:demo
```

The seed is localhost-only and idempotent. Do not point it at `NovaHaven_Local` if you want to preserve the current project data, and never point it at a remote host. Close the third window when done to clear the shell variables.

## 5. Demonstrate the editorial lifecycle

Use a test article with non-sensitive text. Keep a second browser window logged out to represent an anonymous reader.

1. In Admin, create a draft with a Vietnamese title, summary and Markdown body. Save it; verify it is absent from `/wiki` and its public detail route returns not found.
2. Publish it. Open the same slug in the logged-out window and verify title/body and revision number are visible.
3. Change the draft title/body but do not publish. The public page should continue to show the previous published snapshot.
4. Publish again. The public revision number should advance and the new snapshot should appear.
5. Restore an older revision to a draft. Confirm restore does not directly replace public content; only publishing does.
6. Unpublish. Confirm the public detail route is no longer available.
7. Optionally test two Admin edits from separate sessions with an old ETag: the stale write must be rejected, and refresh/retry is required.

This manual walkthrough is a rehearsal script, not an assertion that an authenticated browser E2E suite has passed. A fresh demo database avoids changing the existing three published local Wiki records.

## 6. Compare web and mobile published data

The mobile app uses the same anonymous API snapshots. For an Android emulator, from `apps/mobile` run:

```powershell
flutter pub get
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5080/
```

`10.0.2.2` routes the Android emulator to the host machine. A physical phone requires a reachable local-network address and firewall allowance; use HTTPS for any non-local environment. Open the same article and compare its title and `Phiên bản N` label with the web. The current 2 GB Pixel 7 emulator previously terminated the latest app for low memory, so a stable visual/device run remains an acceptance item; widget tests are not a substitute.

## 7. Reproducible safe checks

From repository root:

```powershell
npm test
npm run smoke:defense
dotnet test tests/backend/NovaHaven.Domain.Tests/NovaHaven.Domain.Tests.csproj --no-restore
dotnet test tests/backend/NovaHaven.Integration.Tests/NovaHaven.Integration.Tests.csproj --no-restore
```

`smoke:defense` only issues GET requests and refuses non-loopback origins; it compares the live public API list/detail with Next.js server-rendered Wiki pages. The C# integration suite creates per-test disposable LocalDB databases. Flutter checks:

```powershell
Set-Location apps/mobile
flutter test --no-pub
flutter analyze --no-pub
```

Do not run `npm run smoke:wiki`, `npm run smoke:all` or `npm run seed:demo` against a database whose content you need to preserve: those flows include authenticated operations or writes. The first two are intended for an isolated local API/database.

## 8. Suggested defense order

1. State the scope: modular monolith, local SQL Server, public published-only Wiki, protected Admin CMS, Flutter reader; Discord is community-owned and Minecraft plugins retain gameplay authority.
2. Show the ERD at [`ERD.md`](ERD.md), then explain immutable revisions, draft/public separation, FK history guards and ETag concurrency.
3. Demonstrate one draft → publish → private edit → republish → restore-to-draft → unpublish lifecycle.
4. Show the Node/domain/integration/mobile test results and explain their boundaries.
5. State remaining limits honestly: no deployment, no production readiness, browser-auth E2E and stable mobile device visual proof still open; consult [`READINESS-CHECKLIST.md`](READINESS-CHECKLIST.md).

Match this walkthrough to the actual school rubric once it is available; no rubric was supplied, so this guide does not claim rubric compliance.
