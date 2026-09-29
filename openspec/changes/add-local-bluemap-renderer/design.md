# Local BlueMap renderer design

## Decision

Use the official BlueMap 5.16 standalone CLI artifact. The source ZIP reports `DataVersion=3955` and spawn `(191, -71)`, corresponding to Minecraft 1.21.1; official v5.16 targets Java 21 and CLI worlds through Minecraft 1.21.11. Do not use current releases that require Java 25.

The runner stores all tool/config/output files beneath repository `.local/bluemap/` (ignored, never public). It verifies Java 21 before any CLI invocation, pins the official release URL, SHA-256 and version, initializes BlueMap's own defaults, and writes the map-specific settings. Whether given a folder or ZIP, create a content-addressed local snapshot with only `level.dat` and overworld `region/*.mca`; never pass the user's source directory to BlueMap, and omit player/entity/private data. Reuse a folder snapshot only when its content hash matches; for a ZIP cache, retain and verify a snapshot-content checksum manifest bound to that ZIP's SHA-256. Reject unsafe ZIP members, symlinks, and duplicate paths. Validate output paths after resolving links and reject overlap in either direction so BlueMap data can never land under or contain `apps/web/public`.

BlueMap runs as a local standalone Java process with its built-in webapp. Enable its perspective view and high-resolution 3D model layer, disable flat and free-flight modes, and default the webapp to perspective. This gives `/map` an angled 3D camera rather than the previous straight-down/isometric view, without enabling free-flight. Keep live player markers off. Bind the HTTP server only to the configured loopback host and local port (default 127.0.0.1:8100), and disable optional metrics telemetry. Rendering reads the private snapshot; updates require rerunning the local render. BlueMap's first render requires downloading Minecraft rendering resources and its config explicitly represents acceptance of Mojang's terms. The runner must not accept those terms automatically: require either the owner to pre-set `accept-download: true` after review or to pass a clearly named `--accept-mojang-downloads` flag for render/start.

The Next.js `/map` route checks the loopback viewer from the server on each request (short timeout, no caching). When reachable, render a responsive, titled iframe in the existing Nova page shell. When unavailable, render the already verified local isometric atlas and show the exact Java 21 setup/start command. The iframe remains isolated from the Nova app origin and grants only the browser capabilities BlueMap needs. No external host is accepted for this local integration.

## Failure and privacy behavior

- Refuse any Java version except 21, a missing/invalid world, a non-loopback bind address, or an unsupported world format with a concise non-zero failure.
- Do not remove existing output, overwrite the source world, kill arbitrary Java processes, or delete any directory recursively. A stop command may terminate only the process launched by this runner, identified by a PID record beneath `.local/bluemap/` and validated against the configured BlueMap JAR command line.
- Keep map tiles, BlueMap jar, downloaded Mojang render resources and extracted fallback world data outside public source paths. The public route is local preview only.
- BlueMap may download Minecraft rendering resources from Mojang when first rendering; do not set `accept-download` or claim first render readiness without explicit owner opt-in.

## Verification

Unit-test Java-major parsing, safe ZIP allowlisting, snapshot hashes, output-root confinement, generated config values, BlueMap-specific loopback health validation and local-viewer availability fallback. Then run the pinned CLI on the supplied save if the owner opts into Mojang resource downloads, inspect the BlueMap loopback page and generated map data, and verify `/map` embeds it while the server is up and returns to the current atlas when it is down. Finally run focused tests, full Node tests and Next typecheck/build.
