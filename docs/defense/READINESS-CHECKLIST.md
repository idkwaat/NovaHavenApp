# Nova Haven — Defense Readiness Checklist

Evidence is scoped to the local source and the exact checks named below. `PASS` means the listed command/check ran successfully; `PARTIAL` means automated or limited evidence exists but a required end-to-end/manual acceptance is missing; `NOT RUN` means no evidence is claimed. Passing code tests does not mean the project is production-ready.

## Local capstone gates

| Gate | Status | Evidence / remaining boundary |
| --- | --- | --- |
| Scope and architecture | PASS | Modular monolith and local-only DB boundary documented; no direct Minecraft plugin DB writes. |
| SQL schema and ERD | PASS | Ten reviewed EF migrations; [`ERD.md`](ERD.md) cross-checked against `NovaDbContext` and read-only local FK metadata. |
| API/domain compilation and business rules | PASS | Integration test run compiled API/projects; domain xUnit 39/39 on 2026-09-28. |
| SQL Server relational integration | PASS | LocalDB integration suite 22/22 on 2026-09-28; each fixture uses its own generated disposable DB. |
| Wiki ETag race | PASS | Ten same-ETag concurrent edit rounds; one write wins per round, loser is rejected, stored draft matches winner, published revision remains unchanged. |
| Sequential stale ETag / lifecycle | PASS | Existing integration and local smoke evidence covers preconditions and draft/publish/restore/unpublish behavior; see current readiness report. |
| Web/API public read parity | PASS | `npm run smoke:defense`: 3 published articles match API list, API detail and Next SSR title/summary/revision. Read-only GETs to loopback only. |
| Node contract/client suite | PASS | `npm test`: 83/83 on 2026-09-28. This suite includes source/contract tests and does not replace browser E2E. |
| Flutter widget suite and analyzer | PASS | `flutter test --no-pub`: 17/17; `flutter analyze --no-pub`: no issues on 2026-09-28. Includes revision label and responsive widget checks. |
| Website TypeScript | PASS | `apps/web npm run typecheck` passed on 2026-09-28. |
| Website production build | PASS (dated evidence) | Next production build passed earlier in the 2026-09-28 baseline report. It was not rerun in this defense-closure batch because the development server was kept available. |
| Android install and live visual acceptance | PARTIAL | A prior latest APK install was terminated by the 2 GB emulator with `LOW_MEMORY`; no current screenshot/stable visual proof. Widget tests do not substitute. |
| Authenticated browser E2E | NOT RUN | No automated browser flow yet proves Admin login → draft → publish → edit → republish → restore → unpublish. Manual steps are in [`DEMO-GUIDE.md`](DEMO-GUIDE.md). |
| WCAG/manual accessibility acceptance | PARTIAL | Automated contrast/touch/focus/responsive checks exist; no complete manual keyboard + screen-reader audit has been recorded. |
| Current DB backup/restore rehearsal | PARTIAL | A disposable local restore rehearsal is documented/tested; not a fresh rehearsal on the data intended for the defense. |
| Evaluation-rubric crosswalk | NOT RUN | School rubric has not been supplied; do not claim rubric coverage yet. |

## Production / online readiness

| Gate | Status | Evidence / remaining boundary |
| --- | --- | --- |
| Production deployment, domain, TLS | NOT RUN | No deployment was requested or performed. |
| Production secrets/key persistence/rate limits | NOT RUN | Local Development configuration only; no production security review. |
| Backups and recovery objectives | NOT RUN | No production backup schedule, restore SLA or disaster-recovery rehearsal. |
| Monitoring, alerting, operational ownership | NOT RUN | No production observability/alerting setup. |
| Discord, payments, Minecraft plugin integration | NOT RUN / OUT OF MVP | No live integrations; gameplay authority stays with plugins and checkout/grant execution is intentionally absent. |

## Before presenting

- [ ] Run the local demo with a disposable database; verify the exact credentials work privately.
- [ ] Run `npm test`, the .NET domain/integration suites, Flutter tests/analyzer and `npm run smoke:defense` on the presentation machine.
- [ ] Capture fresh screenshots/video only after a stable browser and Android run; label them as local demo evidence.
- [ ] Complete browser-auth lifecycle E2E and the mobile same-revision check if the defense rubric requires full end-to-end proof.
- [ ] Obtain the school rubric and mark each required item with a real artifact/test/demo.
- [ ] Explain the outstanding production gates rather than describing the app as online-ready.
