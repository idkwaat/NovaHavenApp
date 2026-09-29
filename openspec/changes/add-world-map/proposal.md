# Add a source-derived interactive world map

## Why

Nova Haven has no public map view, while a Minecraft Java world save has now been supplied for local development. Render that real world into a browsable atlas instead of showing placeholder terrain or inventing locations.

## What changes

- Add a repeatable, dependency-free local renderer for terrain sampled from a supplied Java world ZIP.
- Add a public `/map` page with a fixed-angle isometric terrain view, block-level zoom, pan, coordinate readout and search/filter for verifiable markers, using Nova Haven's approved visual system and no Wynn-owned assets.
- Include the saved spawn point and only map-sign labels extracted from actual chunks. Do not derive coordinates from the Knowledge CMS's geographic latitude/longitude fields.
- Keep the raw save, player data, entities, inventories, seed and other private save metadata out of the web source; only publish the rendered terrain and minimal map manifest.

## Non-goals

- No camera rotation/free-orbit, full voxel-by-voxel building reconstruction, external basemap, player tracking, Minecraft plugin/API/database integration, editable marker CMS, backend endpoint, database migration or fabricated gameplay information.
- No copying Wynncraft artwork, labels, map tiles or branding.

## Acceptance

- Renderer validates ZIP/Anvil input and produces deterministic one-block surface-color and height assets plus a manifest with accurate world block bounds and saved spawn coordinates.
- Public map loads without the API and defaults to a fixed-angle isometric terrain view; visitors can pan and zoom far enough to distinguish individual sampled block colors, with a 2D fallback when WebGL2 or its texture limits are unavailable.
- Pointer/touch pan, wheel/buttons and keyboard zoom/pan/reset report bounded X/Z block coordinates on selection.
- Map markers originate only from save metadata or sign block entities, are escaped as text, and remain keyboard accessible.
- Node tests, renderer unit tests, web typecheck/build, and an actual local browser check are recorded separately; no unsupported build/SQL claim is made.
