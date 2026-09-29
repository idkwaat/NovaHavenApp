# Admin operations diagnostics

## Purpose

Expose a safe, local-only operational snapshot for Admins without exposing connection strings, credentials or external-provider telemetry.

## Requirements

- Admin can read `GET /api/v1/admin/operations/diagnostics` with the existing Admin cookie policy.
- The response reports database connectivity, applied/pending EF migration counts, published/editorial content counts and persisted integration capability statuses.
- The endpoint is read-only, does not create seed rows, does not run migrations and does not expose secrets or raw connection details.
- The Admin CMS renders the snapshot after authentication and shows a clear warning when migrations are pending or the database is unavailable.

## Scenarios

### Healthy local database

- **GIVEN** an authenticated Admin and a migrated local SQL Server database.
- **WHEN** the Admin requests the diagnostics endpoint.
- **THEN** the API returns HTTP 200 with `canConnect=true`, zero pending migrations and safe aggregate counts.

### Anonymous request

- **GIVEN** an anonymous caller.
- **WHEN** the caller requests the diagnostics endpoint.
- **THEN** the API returns HTTP 401 and does not return operational data.

### Read-only diagnostics

- **GIVEN** the database has no persisted integration capability rows.
- **WHEN** the public integration status or Admin diagnostics read is performed.
- **THEN** diagnostics does not create capability rows, run migrations or mutate content.
