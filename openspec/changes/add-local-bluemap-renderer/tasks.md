# Local BlueMap renderer tasks

- [x] Pin and verify the Java 21 / BlueMap 5.16 / Minecraft 1.21.1 compatibility contract in tests and docs.
- [x] Implement safe local setup/render/serve/status orchestration under ignored `.local/bluemap/`, including checksummed read-only snapshots, tamper-detecting ZIP cache reuse, and output-root overlap confinement; do not mutate the source world or terminate unrelated processes.
- [x] Configure perspective as the default and only map view with the high-resolution 3D model layer; disable flat/free-flight/live players/metrics telemetry and bind the viewer to loopback.
- [x] Keep Mojang resource-download acceptance opt-in; never set the EULA-related config automatically.
- [x] Add `/map` loopback health detection and responsive BlueMap viewer with the existing tested atlas as an offline fallback.
- [x] Add local setup/start/render/serve instructions and privacy/resource-download limitations to README.
- [x] Run focused safety/contract tests before and after implementation; full Node suite, web typecheck/build pass.
- [x] Record exact results and limitations in `docs/verification/2026-09-29-local-bluemap.md`.
- [x] Load Mojang rendering resources after the owner's explicit opt-in; verify the loopback webserver, HTTP 200 settings response, perspective configuration and an actual angled `/map` preview.
- [x] Complete the BlueMap CLI render; it reported `Your maps are now all up-to-date!`.
- [ ] Verify visual coverage across the world bounds and pan/zoom from overview to block-detail scale.
