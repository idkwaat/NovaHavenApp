# Local BlueMap Java 21 integration — verification

**Date:** 2026-09-29  
**Scope:** local-only BlueMap CLI setup and `/map` integration. No live Minecraft server, database, deployment, or source world edits.

## Source and tool evidence

- `java -version`: Java 21.0.11 LTS.
- Supplied `world.zip`: 255,277,237 bytes; 127 overworld region files; `DataVersion=3955`; saved spawn X=191, Z=-71. The extracted save has 127 overworld region files. The runner now snapshots only `level.dat` and those regions to a checksum-addressed private cache; playerdata and entity files are not copied.
- Official BlueMap v5.16 release states Java 21 and CLI support through Minecraft 1.21.11: [v5.16 release](https://github.com/BlueMap-Minecraft/BlueMap/releases/tag/v5.16). The official CLI instructions confirm standalone operation without setting up a Minecraft server: [BlueMap CLI installation](https://bluemap.bluecolored.de/wiki/getting-started/Installation.html).
- Downloaded CLI reports `5.16`, size 6,563,805 bytes, SHA-256 `7940d561890373897f8f6be91a52e765461f40e5be4e1c4401004073ee0d2580`.
- `python scripts/bluemap_local.py setup --world "<local extracted world>"` completed successfully. The actual snapshot is under `.local/bluemap/world-cache/514851969cd321bf836ee69911c23147a97d123bc9d2f5e187d8f6be0794f1cc/world`; BlueMap generated its local webapp/settings under ignored `.local/bluemap/`; settings list only `nova_haven`.
- Generated map config uses the verified snapshot path/spawn, enables the perspective view and high-resolution 3D model layer, and disables flat/free-flight. The webapp defaults to perspective and `/map` supplies a spawn-centered, tilted camera anchor. The webserver config is enabled on `127.0.0.1:8100`; metrics telemetry is disabled. No generated BlueMap assets/world files are written under `apps/web/public`.
- Running `start` without opt-in stopped before rendering as designed. After the owner explicitly agreed to the Mojang terms, `--accept-mojang-downloads` set `accept-download: true` in the ignored local config.
- The first sandboxed render attempt failed while fetching `https://piston-meta.mojang.com/` with `java.net.SocketException: Permission denied: getsockopt`. A later authorized direct Java CLI run loaded the already-consented Mojang rendering resources and started rendering the verified snapshot at `.local/bluemap/world-cache/bdb52708ccd2b007011bf4d2e75e6242b3c6799347897fbef990e06b6868bf96/world`.
- The renderer was restarted with the angled perspective configuration and finished with `Your maps are now all up-to-date!`. `http://127.0.0.1:8100/settings.json` returned HTTP 200 with BlueMap 5.16 and `defaultToFlatView:false`. The `/map?preview=perspective` browser screenshot showed the actual 3D terrain from an oblique angle. The CLI completion is verified; detailed pan/zoom checks across distant regions and block-level tiles were not performed.

## Automated verification

- Focused Python tests: **12 passed**, including the regression assertion for perspective enabled and flat/free-flight disabled.
- Map Python tests: `python -m unittest tests.map.test_bluemap_local tests.map.test_render_minecraft_map -v` → **27 passed**.
- `npm test` → **155 total, 151 passed, 0 failed, 4 skipped** (the existing content-dependent skips).
- `apps/web`: `npm run typecheck` → passed.
- Browser check: `/map?preview=perspective` showed the real local BlueMap iframe centered at the saved spawn with an oblique camera. This is a partial-render preview, not a full-world completion check.

## Not yet verified

The owner has explicitly opted in to BlueMap's Mojang resource download; the local ignored config has `accept-download: true`. The BlueMap CLI reported the world render up-to-date and the local viewer is responding. Still not verified: visual tile coverage throughout the complete world bounds, pan/zoom at block-detail scale and a production build after the latest camera-anchor change. The `/map` browser preview was exercised in the local development server.

```powershell
python scripts/bluemap_local.py status
```

The consent flag is no longer needed in this machine-local config; on a fresh machine, use it only after accepting Mojang's terms. Then open the local Next.js site at `/map`. Until tiles render, `/map` keeps the tested source-derived isometric atlas as its fallback. Tile rendering time, generated tile size, BlueMap HTTP viewer response, and BlueMap browser visuals remain unverified.
