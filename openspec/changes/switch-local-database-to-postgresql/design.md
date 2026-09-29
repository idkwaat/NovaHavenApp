# Design: PostgreSQL local persistence

## Architecture

Keep `NovaHaven.Api` → `NovaHaven.Application` (BLL) → Application-owned persistence ports → `NovaHaven.Infrastructure` (DAL) and the existing `NovaDbContext`. Replace the provider at composition/design-time with PostgreSQL/Npgsql. The API remains the only composition root; Domain and client API contracts remain provider-agnostic.

## Concurrency and ETags

PostgreSQL does not provide SQL Server `rowversion`. Change the Domain concurrency properties from `byte[]` to `uint` and configure them as EF row-version tokens so Npgsql maps them to PostgreSQL's system `xmin` transaction identifier. Keep HTTP `ETag` and `If-Match` values encoded as base64 strings; encode/decode the unsigned token at the API boundary and retain 428/412 semantics. Do not add an application-maintained fake rowversion or expose provider details to clients.

## Migrations and local data

Generate a PostgreSQL baseline from the current model into `Infrastructure/Data/PostgreSqlMigrations`. Keep existing SQL Server migrations on disk as historical source but exclude `Infrastructure/Data/Migrations/**/*.cs` from compilation so EF tooling discovers only the PostgreSQL baseline. Apply migrations only through the documented local `dotnet ef database update` command. The pre-existing SQL Server database and its migration files remain untouched; no data transfer is implied.

The local API and integration tests read a connection string from environment/user-secrets. The repository contains only a safe example with placeholders. Tests use a dedicated disposable PostgreSQL database and must never target the user's normal local content database.

## Provider-specific behavior

- Map the existing ETag concurrency token to `xmin` and verify two simultaneous writes produce one success and one stale-precondition response.
- Translate PostgreSQL unique, foreign-key, serialization, and deadlock SQLSTATEs only at the Infrastructure/API boundary; do not expose raw exception details.
- Preserve serializable transaction boundaries for Wiki publishing and local-demo checkout.
- Keep generated timestamps UTC and retain SQL-backed diagnostics/pagination semantics.

## Verification

1. Restore/build all .NET projects with the pinned EF Core 10-compatible Npgsql provider.
2. Generate and inspect an initial PostgreSQL migration from the current model, including Identity, Wiki, commerce, notifications, FKs, indexes and enum storage.
3. Apply it only to the dedicated local integration database.
4. Run integration tests for accounts, authorization, notifications, CRUD, ETags/concurrency, transaction rollback, and diagnostics.
5. Confirm API/OpenAPI response contracts are unchanged and document local setup without storing credentials.
