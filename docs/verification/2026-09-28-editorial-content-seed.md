# Editorial content seed verification — 2026-09-28

## What was added

- Four Vietnamese News posts documenting shipped project features: the expanded Wiki, published category/tag search, browser-local bookmarks/recent history, and the draft/published revision boundary.
- Four additional Knowledge locations and four NPC profiles, linked through the existing published-location relationship.
- All new locations have null latitude/longitude. The place and NPC texts identify themselves as original Nova Haven lore, not verified locations, NPCs, quests or mechanics in the online game.
- The earlier Wiki expansion remains in the same seed: 12 substantial articles plus the three baseline entries, for 15 total on a fresh database.
- Rewrote the baseline sword/map sample copy to label it as unverified project lore and remove invented combat advice and directional claims.

At the time of the initial verification below, Catalog and Community fixtures were intentionally omitted because no authoritative Nova Haven game or participant records were available. The incremental update below adds clearly identified vanilla references and fictional local examples without presenting them as authoritative server data. Rewards and Commerce offers remain unseeded.

## Catalog and Community preview dataset — incremental update

- Added 12 Vietnamese Catalog references across all six item kinds. Each points to an official Minecraft.net source and explicitly separates vanilla reference material from unverified Nova Haven server content. No custom server values, recipes, drops or coordinates are asserted.
- Added 10 fictional Community samples across all five groups: two closed-registration event examples, two guild profiles, three fictional player profiles, two housing concepts and one editorial-only leaderboard. They contain no real Discord links, player UUIDs, contact details or server-derived scores.
- Added a visible local-demo provenance notice to Catalog and Community overview/list/detail routes in development. Housing entries use text-only gallery captions rather than claiming to show real player builds.
- Seed creation uses the existing authenticated Admin API lifecycle, CSRF protection, ETags and published-only contract. Re-running skips matching records and never deletes or rewrites existing records; an existing matching draft may be published as with the other seed content.
- Added a Windows helper that pins the seed target to preview API port 5081, health-checks it and prompts for Admin credentials using a hidden secure input; it restores the caller's environment and clears the temporary secure buffer after execution.
- This source update passed `node --test tests/web/demo-seed.test.mjs` (7/7), `npm test` (121/121), `cd apps/web && npm run typecheck` and PowerShell parser validation. It has **not** been inserted into a SQL Server database in this run; see the preview status below.

## Verification performed

- Focused seed suite: `node --test tests/web/demo-seed.test.mjs` — 5/5 passed.
- Full Node suite: `npm test` — 115/115 passed.
- A fresh disposable SQL Server LocalDB database applied all 10 reviewed migrations and ran the real API plus `npm run seed:demo` twice.
- Anonymous public API assertions: Wiki 15, News 4, locations 5, NPCs 4. Every public detail returned substantive content; all new map coordinates were null; every NPC's published location snapshot matched the intended location. A second run retained the same counts and location/NPC slugs.
- The exact temporary database `NovaHaven_ContentSeedVerify_20260928_36670cb152a444acad6c5b229075b2aa` was dropped after the check.
- `npm run smoke:defense` passed read-only against the current local preview and matched its three published Wiki articles across API and web rendering.

## Existing preview database

`NovaHaven_Local` on API port 5080 was not migrated, seeded, edited or deleted. The separate local web preview on port 3002 calls API port 5081; both endpoints are healthy, but its public Catalog and Community endpoints still return `total: 0`, and `/admin` shows the sign-in form. No authorized Admin credentials were available in this task. The current Windows execution context also could not access the `MSSQLLocalDB` instance registry, so it could not verify or write the preview database directly. The new records therefore exist in the source seed files but have **not** been published to either database. Existing legacy rows remain untouched. To populate the intended local preview, enter the authorized account locally (do not send the password in chat) and run:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/seed-preview-content.ps1
```

The script refuses non-loopback API URLs. Do not share the local password in chat or commit it to the repository.
