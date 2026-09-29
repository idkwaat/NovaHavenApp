# Tasks

- [x] Implement explicit opt-in VND pricing, immutable order/payment snapshots and the additive EF migration. Runtime application of the migration remains pending.
- [x] Implement server-priced, idempotent local-demo checkout and Admin-only order history; add focused LocalDB integration tests. SQL fixture execution remains blocked by LocalDB error 50 before test bodies.
- [x] Implement the public storefront, browser cart, checkout receipt, global cart navigation and responsive accessible states; Node tests and web production build pass.
- [x] Reconcile Admin UI, OpenAPI, smoke tests, roadmap and README; run all gates available without SQL Server.
- [ ] Apply the migration and pass focused integration/smoke tests against a fresh disposable local SQL Server database; complete a browser purchase journey.

Detailed TDD execution sequence: [`docs/superpowers/plans/2026-09-28-local-shop-checkout.md`](../../../docs/superpowers/plans/2026-09-28-local-shop-checkout.md).
