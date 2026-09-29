# Nova Haven Catalog Items Vertical Slice

## Goal

Add the first real Catalog slice to the existing modular monolith: editorially managed game item records with draft/edit, immutable published revisions, unpublish, an anonymous published-only API, and thin web/mobile readers. No Minecraft data is seeded or fabricated; external game synchronization remains a separate blocked integration.

## Invariants

- Public endpoints expose only the current published revision.
- Admin writes require the existing Admin policy, CSRF token and `If-Match` for mutations after creation.
- Published revisions are append-only and retain the publisher and UTC publish time.
- Slugs are lowercase and unique.
- SQL Server schema changes use an EF Core migration.
- Web and Flutter consume the API contract instead of owning catalog truth.

## Execution order

1. Add failing domain/API contract tests for validation and publish visibility.
2. Add Catalog domain entities and validator.
3. Add EF Core mapping, API endpoints and migration.
4. Add public web catalog list/detail and a small Admin catalog editor.
5. Add Flutter catalog list/detail reader.
6. Run local LocalDB integration, API/build, web and Flutter verification; fix failures before reporting.

## Explicit non-goals for this slice

- Recipes, drops, NPCs, economy or live Minecraft/plugin synchronization.
- Production hosting, cloud database or external seed data.
