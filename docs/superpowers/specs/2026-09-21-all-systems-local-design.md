# Nova Haven All Systems — Local-owned Design

**Date:** 2026-09-21  
**Status:** Approved for implementation by the owner's explicit continue/build instruction.  
**Scope:** Build the local-owned portions of the remaining roadmap without fabricating Minecraft-owned state.

## Goal

Turn Nova Haven from a Wiki/Catalog reader into a connected editorial platform. The first local slice covers NPC Directory, Quest Codex, World Atlas and Seasonal Hub, with real relations between those records and the existing Catalog. Later slices add community records (Events, Guild, Housing, Player profiles and editorial leaderboards) and explicit integration contracts for Minecraft-owned data.

## Boundaries

- SQL Server remains local/disposable only; no production deployment or external database writes.
- ASP.NET Core is the only API, Next.js is the web/admin client and Flutter is the reader client.
- Editorial records are not gameplay synchronization. Every record carries editorial ownership and publication state; no Minecraft statistic, reward execution, ownership claim or plugin result is invented.
- Public reads return only the current published revision. Admin mutations require Admin authorization, CSRF and ETag/If-Match where the record can be edited concurrently.
- UUIDs are serialized as strings, timestamps are UTC, and schema changes use EF Core migrations only.
- Public Markdown is rendered inert and sanitized using the existing rules.
- Relations are validated at publication time. A published relation may target only a published record/revision or a published Catalog item revision.

## First slice: Game Knowledge Graph

### Records

`GameKnowledgeEntry` is the lifecycle aggregate. Its `Kind` is one of `Npc`, `Quest`, `Location` or `Season`. Draft fields hold the editable record; `GameKnowledgeRevision` stores immutable published snapshots. Type-specific metadata is relational:

- `NpcProfile`: role, location entry, portrait media URL and editorial notes.
- `QuestDefinition`: difficulty, giver NPC, location, ordered steps and reward description.
- `WorldLocation`: region, map image URL, latitude, longitude and location type.
- `SeasonDefinition`: start/end UTC, theme and optional event description.

`GameKnowledgeDraftLink` and `GameKnowledgeRevisionLink` connect entries to one another and to published Catalog items. The revision link snapshots the target revision IDs so later edits cannot change an already published graph.

### API

- Anonymous: `GET /api/v1/knowledge`, `GET /api/v1/knowledge/{kind}`, `GET /api/v1/knowledge/{kind}/{slug}`.
- Admin: `GET/POST /api/v1/admin/knowledge/{kind}`, `GET/PATCH /api/v1/admin/knowledge/{kind}/{id}`, `POST .../publish`, `POST .../unpublish`.
- Admin payloads include typed fields and links; invalid kinds, duplicate links, unknown targets, unpublished targets and invalid date/coordinate ranges return Problem Details or validation errors.

### Clients

The web public navigation exposes Knowledge, NPCs, Quests, Atlas and Seasons. The Admin page gets a focused Knowledge manager. Flutter gets a published Knowledge list/detail reader with type filters and relation links. No client receives draft data.

## Second slice: Community-owned systems

Use the same lifecycle only where it provides editorial value:

- Events: local event definition, registration and participant moderation; gameplay results remain external.
- Guilds: community profiles and links; in-game membership remains external.
- Housing: showcase submissions and moderation; ownership remains external.
- Player Hub: profile and editorial badges; Minecraft statistics are optional imported snapshots only when a real adapter exists.
- Leaderboards: editorial/web event standings first; gameplay standings require a verified source.

These records must reuse the graph's publication and authorization rules, not become unrelated CRUD tables.

## Integration foundation

Add a capability contract and sync status model only. The adapter reports `Unavailable`, `Configured`, `Healthy` or `Stale`, last attempt and safe error text. No implementation may claim a gameplay sync or execute rewards without a real plugin ACK contract. Commerce stays definition-only until a payment provider and transaction design exist.

## Acceptance

For each delivered slice:

1. OpenSpec scenarios describe observable behavior.
2. EF migration applies to LocalDB and has no pending model changes.
3. Domain/application tests cover validation and publication isolation.
4. Integration tests cover lifecycle, relation validation, snapshots, ETag and public 404 for drafts.
5. Node/OpenAPI tests and Next.js typecheck/build pass.
6. Flutter tests/analyze/build pass when the installed SDK permits it.
7. Roadmap records `Implemented`, `Verified` and `Accepted` separately; missing browser/emulator/manual evidence is not called Accepted.
