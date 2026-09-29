# World Map design

## Decision

Render one surface-color sample and one height sample for every Minecraft block coordinate. The current save produces an approximately 3,200×2,304 terrain texture; this lets close zoom show each top-material color instead of enlarging a coarse atlas. Keep the WebGL terrain mesh at four-block spacing to bound geometry/memory while the color texture remains one pixel per block. A native WebGL2 orthographic renderer presents that heightfield at a fixed isometric angle; pan and zoom do not rotate the camera. Use source height for relief and the existing block-name color mapping for top surfaces. This is not a full voxel/building reconstruction and does not bundle Minecraft's copyrighted resource-pack textures. No external or AI-generated image is used.

Keep terrain colors in `terrain.png` and encode each signed surface height as a little-endian unsigned 16-bit offset in `heightmap.bin`: `0` means no sampled surface, otherwise `code = y - MIN_BUILD_Y + 1`. The renderer buffers compact 16×16 chunk records and only then allocates final rasters; it must not keep millions of Python tuple/dict samples in memory. The manifest lists the same dimensions, one block per pixel, the fixed local height asset path and the observed minimum/maximum Y. Publish the manifest last after both asset files have been replaced.

The renderer reads only `level.dat` and `region/*.mca` entries from the supplied ZIP. It skips playerdata, advancements, entities, datapacks and other save files. It emits a compressed raster plus a small manifest; it does not copy the raw world into the repository. It may extract text only from sign block entities and never serializes arbitrary NBT fields. Archive paths, sizes, chunk sector offsets, NBT depth and array lengths are validated before processing.

The web page is static with respect to the backend. A Server Component validates the bundled manifest and local terrain/height assets. A leaf Client Component uses WebGL2 for the fixed-angle isometric heightfield, preserves per-block colors with nearest-neighbor sampling and lighting, and supports pointer/touch pan, wheel and button zoom, keyboard arrows/`+`/`-`/Home, click-to-read-and-copy coordinates, marker search and layer toggles. The prior 2D image explorer remains available as an explicit mode and automatic fallback when WebGL2, a valid height asset or sufficient texture dimensions are unavailable. The marker model distinguishes the level.dat spawn from source sign labels. Marker strings render as escaped React text. Knowledge's `latitude`/`longitude` remain geographic and are not consumed as Minecraft coordinates.

## Coordinate convention

- X increases to the right; Z increases downward on the rendered map.
- Coordinates are Minecraft block coordinates, not chunk coordinates or latitude/longitude.
- Click position maps linearly to the rendered block bounds and is rounded down to the containing block.
- In the 3D view, a color-picking pass resolves the visible terrain surface to its block pixel; no geographic or fabricated coordinates are used.
- The saved spawn is included only when the `level.dat` metadata has both coordinates and they lie within rendered bounds.

## Failure and privacy behavior

- Invalid archive, unsupported compression, corrupt chunk NBT or empty render fails the command with a contextual error and a non-zero exit; it must not leave a partial manifest that looks valid.
- Missing renderer input is an explicit invocation error. The web route handles a missing bundled manifest/image with a useful unavailable state.
- The generated manifest and height field exclude seed, account IDs, player inventory/position, entity data and raw NBT. Sign text is trimmed, length-limited, deduplicated by coordinate/text and rendered as text only.
- This is a private local preview. Source ownership/licensing for redistribution of the supplied map has not been independently verified; do not imply permission to publish it online.

## UI/accessibility

Preserve `design-system/nova-haven/MASTER.md`. Use its dark earth/timber palette, readable Vietnamese text and existing layout conventions. Keep controls at least 44×44 px with visible focus, labels and pressed states; provide keyboard alternatives to drag/wheel, fit a 320px viewport without page-level horizontal overflow, respect reduced motion, and make coordinate selection understandable without relying on color alone. The camera direction is fixed; zoom is bounded and does not cause continuous motion. The flat map is always one visible action away.

## Files

- `scripts/render_minecraft_map.py` and `tests/map/test_render_minecraft_map.py`: safe save reader, one-block surface renderer and unit tests.
- `apps/web/public/maps/nova-haven/terrain.png`, `heightmap.bin`, `manifest.json`: generated derivatives only; never copy the source world ZIP.
- `apps/web/app/map/page.tsx`, `apps/web/app/map/WorldMapExplorer.tsx`, `apps/web/app/map/IsometricTerrainCanvas.tsx`, `apps/web/app/map/world-map.module.css`: server route, accessible fixed-angle view/fallback and route styles.
- `apps/web/src/lib/world-map.ts`, `world-map-3d.ts`, `tests/web/world-map.test.mjs`, `tests/web/world-map-3d.test.mjs`: validated assets and tested coordinate/mesh math.
- `apps/web/app/SiteNavigation.tsx`, `apps/web/app/layout.tsx`: route entry points.
- `tests/web/world-map-page.test.mjs`: route contract/accessibility safeguards.
