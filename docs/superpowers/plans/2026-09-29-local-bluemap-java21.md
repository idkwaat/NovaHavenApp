# Local BlueMap Java 21 Implementation Plan

## Goal

Render the user's offline Minecraft 1.21.1 world with the Java 21-compatible BlueMap CLI, display its fixed-direction flat map at `/map`, and preserve the current map as a no-service fallback.

## Decisions already made

- BlueMap release: 5.16 CLI, pinned; Java major must be 21.
- World evidence: `DataVersion=3955`, saved spawn X=191, Z=-71, source ZIP 255,277,237 bytes and 127 overworld regions.
- Local-only: no server/plugin/player tracking; loopback bind; local outputs ignored; never modify the source ZIP/world.
- UI: BlueMap angled perspective view plus high-resolution 3D block models, retain Nova page shell, mobile-sized viewer and current atlas fallback. Flat and free-flight modes are disabled; perspective is the default.
- No Git history/branch is available in the supplied checkpoint; don't create fictitious commits.

## Tasks

1. **Tests first** — add Python unit tests for Java 21 version validation, ZIP member allowlisting, HOCON configuration generation, loopback-only origin and server availability fallback. Run and capture expected RED results.
2. **Local runner** — implement a standard-library Python CLI that pins/downloads BlueMap 5.16, bootstraps its config, validates a world directory or safely extracts only needed world files, configures flat-only/local serving, and supports setup/render/start/serve/status without touching unrelated processes.
3. **Web slice** — add a server-only local BlueMap health resolver and accessible responsive iframe; prefer BlueMap only while the loopback service is healthy, else preserve the existing terrain explorer and show local run guidance.
4. **Docs** — add local commands, Java/version matrix, first-run Mojang resource download note, output locations, and explicit no-live/no-publication boundary.
5. **Verification** — focused Python and Node map tests, full `npm test`, web typecheck/build, then BlueMap render and actual local viewer check if download/network permits; report any external dependency blocker accurately.

## Review risks

- Wrong major in the chosen Java executable; validate before launch.
- Archive traversal, duplicate names, or irrelevant private data copied from ZIP; strict member/path/size allowlist.
- BlueMap accidentally starts a LAN-accessible webserver; enforce loopback in generated config and verify listening address.
- Next route caches viewer availability or iframe overflows on mobile; force dynamic, short bounded server probe and CSS viewport sizing.
- Java CLI works but render resources are unavailable offline; preserve old map and do not report BlueMap output as generated.

## Status

- [x] Product scope, current map architecture, UI baseline, and OpenSpec read.
- [x] Verify Java 21.0.11 and source world data version 3955 / 1.21.1.
- [x] Pin BlueMap v5.16 from official release metadata; v5.17 moved to Java 25.
- [x] Write failing focused tests; they were red before implementation.
- [x] Implement runner/configuration, including explicit Mojang resource-download opt-in.
- [x] Integrate `/map` iframe and existing atlas fallback.
- [x] Verify runner setup, Java/config generation, Node/Python tests, web typecheck/build; record exact evidence.
- [x] Start the authorized direct CLI render, confirm the loopback viewer and inspect the angled perspective preview in-browser.
- [x] Finish the CLI render; BlueMap reported all maps up-to-date.
- [ ] Inspect complete tile coverage and pan/zoom at overview and block-detail scales.
