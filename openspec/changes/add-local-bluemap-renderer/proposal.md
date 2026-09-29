# Add a local BlueMap world viewer

## Why

The current `/map` is a source-derived terrain atlas, not the detailed Minecraft map experience requested for Nova Haven. The owner approved rendering the supplied local Java world without a live server and selected Java 21.

## What changes

- Add a reproducible, local-only BlueMap CLI 5.16 workflow pinned to Java 21 and Minecraft 1.21.1 (`DataVersion` 3955).
- Safely snapshot only metadata and overworld region files from either an extracted directory or ZIP into a content-addressed ignored local working directory. Never modify, publish, or pass the source save directly to BlueMap.
- Render with BlueMap's perspective view and high-resolution 3D model layer enabled; disable flat and free-flight modes and all live-player integration.
- Bind BlueMap's built-in viewer to loopback, and make `/map` show it when the local viewer is running. Retain the existing source-derived atlas as the usable fallback.
- Keep the CLI jar, config, downloaded resources and rendered tiles in ignored local artifacts, with repeatable start/stop/status instructions.

## Non-goals

- No Minecraft server, plugin, live player tracking, world auto-update, public deployment, API/database changes, Wynn assets, or in-game gameplay data integration.
- No replacing the original save or removing the existing tested atlas renderer.

## Acceptance

- The setup rejects a Java major version other than 21 and uses only the pinned official `bluemap-5.16-cli.jar` release.
- The first render does not silently accept Mojang terms; the resource-download opt-in requires a clearly named user flag or a previously reviewed local config value.
- World archive handling rejects traversal/duplicate paths and extracts only `level.dat` and `region/*.mca` when extraction is needed.
- World folder and ZIP inputs can be rerun safely; cached snapshots are checksummed against source data, and output-root path resolution rejects any overlap with public web paths through symlinks/junctions.
- The generated map config targets the verified world directory, selects the overworld and saved spawn, and disables rotating/free-flight camera modes and live web serving on non-loopback interfaces.
- `/map` dynamically prefers the local BlueMap viewer when reachable and otherwise presents the existing map with a clear local-start instruction.
- Focused tests, Node suite, web typecheck/build, and an actual local BlueMap render/browser check are reported independently; no live-server or online capability is claimed.
