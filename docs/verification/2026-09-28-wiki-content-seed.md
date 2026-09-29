# Wiki content seed expansion — 2026-09-28

## What changed

Added twelve original Vietnamese editorial articles to `scripts/demo-wiki-content.json`, with four articles in each existing Wiki category:

- **Khởi hành:** reading the map, keeping a useful travel journal, contributing to the Wiki, and planning a first expedition.
- **Lớp nhân vật:** introductory Swordsman guidance, choosing a play style, party communication, and a practice journal.
- **Vùng đất:** Bến Nguyệt, Rừng Thông Mù Sương, the riverside path, and the Thung lũng Sao lampkeeper tale.

The content also adds the focused tags `Khám phá`, `Cẩm nang`, `Cộng đồng` and `Nhật ký`. Articles are structured in Vietnamese Markdown and distinguish lore, personal observations and information that still needs verification. They are project-authored content, **not** synchronized from a Minecraft plugin and do not assert unverified statistics, item drops, commands, coordinates or recipes.

## Verification

- `node --test tests/web/demo-seed.test.mjs` → 2/2 passed, including uniqueness, category balance, tag references, Vietnamese text and substantial sectioned content.
- Fresh disposable SQL Server LocalDB: all ten reviewed migrations applied; `npm run seed:demo` published 15 Wiki articles (the three existing fixtures plus twelve new articles); anonymous detail reads returned non-empty Markdown for every article.
- A second seed run left the published slug set and count unchanged (15), confirming idempotence.
- The disposable database `NovaHaven_DemoSeedVerify_20260928_2ec18457b3c7452b9836ac1a5aa4ec9f` was dropped after verification. It was separate from `NovaHaven_Local`.

## Existing preview database

The current API at `http://127.0.0.1:5080` was queried anonymously before the change and returned three published articles. It was not seeded: the current task environment had no Admin login credentials, and bypassing the CMS authorization would be inappropriate. `NovaHaven_Local` therefore remains unchanged. To load these fixtures into that local preview, sign in with the existing local Admin credentials and run the documented `npm run seed:demo` command with `NOVA_API_URL=http://127.0.0.1:5080` and the Admin email/password supplied via environment variables.
