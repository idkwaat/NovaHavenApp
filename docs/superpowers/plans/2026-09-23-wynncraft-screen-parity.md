# Wynncraft-Inspired Screen Parity Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reshape Nova Haven's public website screens to follow the real Wynncraft site's composition and navigation patterns while retaining Nova content and behavior.

**Architecture:** Keep the existing Next.js app, API-backed page data and shared navigation. Make homepage, Wiki, News, Catalog, Knowledge and Community visually distinct compositions; do not copy Wynncraft logos/artwork or invent game, player or commerce data.

**Tech Stack:** Next.js 16 App Router, React 19, global CSS, Node built-in test runner, browser screenshots.

**Spec:** `docs/superpowers/specs/2026-09-21-complete-platform-design.md`; public content contracts in `openspec/changes/add-wiki-cms/specs/wiki-content/spec.md`, `openspec/changes/add-news/specs/news/spec.md`, `openspec/changes/add-catalog-items/specs/catalog-items/spec.md`, `openspec/changes/add-game-knowledge/specs/game-knowledge/spec.md`, and `openspec/changes/add-community-systems/specs/community/spec.md`.

## Global Constraints

- Public Wiki, News, Catalog, Knowledge and Community screens remain anonymous and published-only.
- Do not add fabricated Minecraft, server, player, video, store or gameplay facts.
- The public site is a local-preview surface; no checkout, external writes or deployment.
- Keep keyboard-accessible navigation, search, focus indicators and responsive layouts.
- Use externally sourced real imagery with visible attribution; do not generate or reuse Wynncraft-owned artwork.

## Review Focus

- API unavailable/empty results: page still renders an honest empty/error state.
- Long Vietnamese text: no clipping, broken glyphs or low contrast.
- Narrow viewport: toolbar remains usable; side-by-side layouts collapse cleanly.
- User-provided server identity: the centered host remains readable without overflow.
- Wynncraft routes blocked or loading: do not treat security challenges/skeletons as the target design.

---

### Task 1: Reference-backed screen structure

**Files:** `apps/web/app/page.tsx`, `apps/web/app/SiteNavigation.tsx`, `tests/web/wynncraft-reference.test.mjs`

- [ ] Add failing assertions for a clean centered home hero, removal of the generated-looking popup/vertical rail, and an explicit expandable navigation destination list.
- [ ] Run `node --experimental-strip-types --test tests/web/wynncraft-reference.test.mjs` and confirm it fails because the structure is not implemented.
- [ ] Replace the fake signboard/character/promo treatments with a restrained brand lockup, server line and two real destinations; add missing public routes to the drawer.
- [ ] Run the focused test and confirm it passes.

### Task 2: Wynn-like content screen compositions

**Files:** `apps/web/app/wiki/page.tsx`, `apps/web/app/news/page.tsx`, `apps/web/app/catalog/page.tsx`, `tests/web/wynncraft-reference.test.mjs`

- [ ] Add failing assertions for two-pane Wiki filters/results, News feature/feed layout, and Catalog filter/results layout while keeping existing API data fields.
- [ ] Run the focused test and confirm it fails for the missing structures.
- [ ] Implement the page wrappers, descriptive accessible labels, and honest empty states; retain existing query parameters and published-only API reads.
- [ ] Run the focused test and confirm it passes.

### Task 3: Reference visual system and responsive verification

**Files:** `apps/web/app/reference-theme.css`, `apps/web/app/layout.tsx`, `tests/web/wynncraft-reference.test.mjs`

- [ ] Add failing assertions for the separate bright world hero, warm parchment item-filter surface, image-led News panel, legible neutral Wiki surfaces, and compact responsive footer.
- [ ] Run the focused test and confirm it fails because the reference theme is not loaded.
- [ ] Add a final, page-scoped stylesheet after existing styles; keep admin/editor behavior styled and preserve the current drawer/backdrop close behavior.
- [ ] Run `node --experimental-strip-types --test tests/web/*.test.mjs`, `npm run typecheck --prefix apps/web`, and `npm run build --prefix apps/web`.
- [ ] Compare the local screenshots at desktop and mobile widths; reset temporary viewport overrides and leave the user's browser on the refreshed Nova homepage.
