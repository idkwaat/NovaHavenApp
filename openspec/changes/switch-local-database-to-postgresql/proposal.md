# Switch local persistence to PostgreSQL

## Why

The current local integration-test path depends on SQL Server LocalDB, but the runtime cannot create its automatic instance in this environment. The owner has directed the project to use PostgreSQL when SQL Server keeps failing. The local PostgreSQL 18 service is reachable on loopback, and this also aligns persistence with the WMS reference project.

## Scope

- Use PostgreSQL through EF Core/Npgsql for the local Nova Haven database and disposable integration-test databases.
- Keep the existing modular monolith, API routes, JSON contracts, auth/CSRF rules, SQL semantics, and local-only data boundary.
- Preserve the existing SQL Server database and historical migration source without deleting or modifying that database; PostgreSQL starts as a separate local database and does not implicitly import SQL Server data.
- Keep migrations explicit; never call `EnsureCreated` or apply schema changes automatically at API startup.
- Do not add SQLite, remote database hosting, or a second runtime database provider.

## Non-goals

- Moving local SQL Server data into PostgreSQL.
- Changing public API behavior or the approved Web/Mobile designs.
- Introducing a database abstraction framework beyond EF Core.
