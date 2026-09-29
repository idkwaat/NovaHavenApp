# Foundation verification — 2026-09-18

Scope: reconstructed source from the approved design because the previous source/branch did not exist in the mounted workspace. This log covers **only the reconstructed checkout**.

| Check | Command / observation | Result |
| --- | --- | --- |
| Native web helper, API client and contract tests | `npm test` | 10 tests passed, 0 failed. |
| JavaScript smoke script syntax | `node --check scripts/wiki-smoke.mjs` | Exit 0; does not test HTTP or database. |
| JSON & project XML well-formed | Python json + ElementTree parse | 4 JSON files and 5 csproj files parsed. |
| Next dependencies | `cd apps/web && npm install --offline --ignore-scripts --no-audit --no-fund` | FAILED `ENOTCACHED`: @types/node not available in offline npm cache. No `next build` or `tsc` performed. |
| .NET compilation / xUnit | `command -v dotnet` | Missing SDK: not run. |
| Flutter compile/tests | `command -v flutter` | Missing SDK: not run. |
| SQL Server / integration / live smoke | `command -v docker` | Docker missing and no SQL server configured: not run. |

**Not verified:** build readiness, NuGet package restore, relational concurrency, CSRF login via live endpoint, XSS rendering in browser, Android app, end-to-end course demo, production security/deployment.

**Next gate:** install the toolchain and dependency packages, generate EF migrations, run all real builds/tests and the local API smoke script. Do not interpret source presence or 10 Node tests as proof of those gates.
