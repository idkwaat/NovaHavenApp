# Fixed Isometric Block Map Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the map's default flat atlas with a fixed-angle, source-derived WebGL terrain view that can pan and zoom to individual block colors.

**Architecture:** Extend the local world renderer to emit one surface-color sample per world block plus a compact height field, without retaining millions of Python objects. A small WebGL2 client component renders a fixed isometric heightfield using those local assets; existing 2D atlas remains the manual and automatic fallback. No external map tiles, Wynncraft assets, AI art, backend or new package.

**Tech Stack:** Python standard library, Next.js 16 / React 19, TypeScript, WebGL2, Node test runner.

**Spec:** `openspec/changes/add-world-map/{proposal.md,design.md,specs/world-map/spec.md,tasks.md}`

## Global Constraints

- The map remains a public, backend-independent view of the owner-supplied local Minecraft Java save.
- X increases rightward; Z increases downward; displayed coordinates are integer Minecraft blocks.
- Only rendered terrain, bounds, saved spawn and sanitized sign labels may enter public assets; never publish the raw save, player data, entities, inventories, seed or account data.
- Use source-derived block colors and heights; no external/generated artwork, invented place names, Wynncraft branding or map tiles.
- Keep the camera direction fixed; provide pan, zoom and keyboard/button alternatives; retain 2D fallback if WebGL2 or high-resolution texture support is unavailable.
- Follow `design-system/nova-haven/MASTER.md`; controls stay at least 44px, visible focus, AA contrast, reduced motion and no mobile horizontal overflow.
- Do not add dependencies or claim online deployment, database integration or full voxel rendering.

## Review Focus

- Sparse/empty chunks must stay holes rather than being joined into invented terrain — Task 1 renderer test and Task 2 mesh-mask test.
- Negative coordinates, map edges and projected pointer picks must resolve to bounded block coordinates — Tasks 2 and 3 tests.
- WebGL2 absence or a device texture limit below the asset dimensions must leave a usable 2D map — Tasks 2 and 3 tests.
- Pointer drag must not be the only pan method; keyboard and visible buttons work at 320px with focus visible — Task 3 tests and browser check.
- Missing/corrupt height data must degrade to the atlas and explain why, not show a blank canvas — Task 3 test and browser check.

---

### Task 1: One-block terrain assets and OpenSpec contract

**Files:**
- Modify: `openspec/changes/add-world-map/proposal.md`
- Modify: `openspec/changes/add-world-map/design.md`
- Modify: `openspec/changes/add-world-map/specs/world-map/spec.md`
- Modify: `openspec/changes/add-world-map/tasks.md`
- Modify: `scripts/render_minecraft_map.py`
- Test: `tests/map/test_render_minecraft_map.py`
- Generate: `apps/web/public/maps/nova-haven/terrain.png`, `heightmap.bin`, `manifest.json`

**Interfaces:**
- Produce `RenderResult(png, heightmap, manifest, chunks_rendered)`.
- Manifest adds `heightmapUrl`, `minY`, `maxY`; `blockScale=1`, with one atlas pixel per block.
- Pack per-chunk records before allocating final rasters; zero height code denotes an unsampled cell.

- [x] Write tests that require 256 surface positions per chunk and verify height bytes preserve signed Y and leave missing cells zero.
- [x] Run `python -m unittest tests.map.test_render_minecraft_map -v`; tests first failed on absent one-block height output.
- [x] Update the OpenSpec for fixed-angle isometric terrain with block-level color zoom, pan and a 2D fallback.
- [x] Implement compact chunk buffering, one-block color raster, signed-height binary output, manifest fields and manifest-last atomic asset replacement.
- [x] Run renderer tests: 15 passed.
- [x] Render the supplied local `world.zip`; output is 3,200×2,304, 27,991 chunks, terrain PNG 911,322 bytes and height field 14,745,600 bytes.

### Task 2: Tested fixed-angle WebGL terrain renderer

**Files:**
- Create: `apps/web/src/lib/world-map-3d.ts`
- Create: `apps/web/app/map/IsometricTerrainCanvas.tsx`
- Test: `tests/web/world-map-3d.test.mjs`

**Interfaces:**
- `buildTerrainGrid(width, height, validMask, step)` returns source-pixel vertices and indices, omitting any quad with a missing height sample.
- `projectTerrainPoint` / `unprojectTerrainPoint` share the fixed camera basis for viewport geometry and coordinate calculations.
- Canvas accepts the manifest and a zoom value; emits bounded block coordinates on selection and reports renderer capability/errors to its parent.

- [x] Add tests for fixed orientation, block-coordinate projection round-trip, zoom bounds, missing-cell mesh holes, row alignment, marker coordinates and shader interface.
- [x] Run focused tests before implementation; tests were RED for absent helper, row alignment and marker support.
- [x] Implement pure geometry/projection helpers and a WebGL2 heightfield with nearest-neighbor block colors, terrain lighting, bounded GPU picking, resource cleanup and texture-limit fallback.
- [x] Run the focused map tests; 10 fixed-angle renderer tests pass.

### Task 3: Make the 3D map the default without losing fallback or access

**Files:**
- Modify: `apps/web/src/lib/world-map.ts`
- Modify: `apps/web/src/lib/world-map-server.ts`
- Modify: `apps/web/app/map/WorldMapExplorer.tsx`
- Modify: `apps/web/app/map/world-map.module.css`
- Test: `tests/web/world-map.test.mjs`
- Test: `tests/web/world-map-page.test.mjs`

**Interfaces:**
- Server loader accepts only the fixed local height asset path, matching terrain dimensions, and a bounded min/max Y range.
- Isometric mode is default when validated height data exists; an explicit flat-map control and automatic capability/error fallback preserve navigation.
- Fixed-angle map supports pan, zoom 1–32, coordinate selection/copy, visible center/cursor readouts, and saved-source markers.

- [x] Add failing contract tests for local height assets, the 3D default, accessibility controls and fallback behavior.
- [x] Run the targeted tests before implementation; absent local height validation and controls were RED.
- [x] Implement local asset validation, 3D/flat modes, keyboard/buttons/pointer interactions, GPU-derived cursor/center readouts, responsive styles and accessible fallback.
- [x] Run full Node suite: 150 tests, 146 passed, 0 failed, 4 existing skips.

### Task 4: Verify actual build, browser rendering and handoff

**Files:**
- Modify: `docs/verification/2026-09-29-world-map.md`
- Modify: this plan and its adjacent ledger

- [x] Run full Node tests, renderer tests, web typecheck and production build; record exact counts and skips.
- [x] Open local `/map`; verify WebGL shader/rendering, source marker, zoom, pan, GPU pick/readout and flat-map mode.
- [x] Check 320×800 and 390×844 mobile screenshots, desktop and accessibility contracts; record the four-block mesh sampling and lack of resource-pack faces.
- [x] Complete independent read-only review; reviewer found no critical/high findings and made no edits.
