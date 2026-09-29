# Tasks

- [x] Update the architecture/project instructions and local setup docs to use PostgreSQL while explicitly preserving the old SQL Server database.
- [x] Pin the compatible Npgsql EF Core provider and use it consistently in API, design-time tooling and test configuration.
- [x] Map the concurrency token to PostgreSQL `xmin` without changing the HTTP ETag contract.
- [x] Isolate active PostgreSQL migrations from the historical SQL Server migration source; generate and review a PostgreSQL baseline from the current model and SQL script.
- [x] Add disposable local PostgreSQL integration-test configuration using a dedicated database and no committed credentials.
- [x] Translate only known PostgreSQL persistence conflicts and preserve current safe Problem Details/status behavior.
- [x] Build all backend projects, apply migrations to dedicated disposable PostgreSQL databases, run relational tests, and verify API contracts.
- [x] Update README and roadmap with actual setup commands and tested/blocked status.
