# Nova Haven Catalog Recipes Vertical Slice

Recipes are an editorial graph over Catalog items. A recipe draft stores item UUID references and quantities; publishing snapshots the currently published revision of every referenced item. Public recipes expose only that immutable snapshot. No recipe values are seeded from Minecraft without an authoritative source.

## Acceptance

- Draft recipe create/edit uses the existing Admin, CSRF and ETag lifecycle.
- A recipe cannot publish when an ingredient or output item is missing or not currently published.
- Published recipe revisions are append-only and retain component quantities plus item revision snapshots.
- Public list/detail is anonymous and published-only.
- LocalDB migration, API integration, Node contract and Flutter parsing tests pass.
