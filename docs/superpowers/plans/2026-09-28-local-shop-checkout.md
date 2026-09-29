# Nova Haven Local Shop and Demo Checkout Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a locally persisted shop, browser cart and visibly simulated checkout without real payment or game fulfilment.

**Architecture:** Extend existing Commerce offer revisions with opt-in purchase prices, persist orders/lines/local-demo payment rows in the existing EF Core SQL Server context, and expose a server-priced idempotent checkout API. Build the storefront/cart/checkout in the existing Next.js App Router using only slugs and quantities in browser storage; retain the existing modular monolith and Admin security model.

**Tech Stack:** ASP.NET Core .NET 10, EF Core 10.0.12, SQL Server LocalDB, Next.js 16 App Router, TypeScript, native Node test runner, xUnit.

**Spec:** `openspec/changes/add-local-demo-commerce/specs/commerce-checkout/spec.md`; architecture: `docs/superpowers/specs/2026-09-28-local-shop-checkout-design.md`.

## Global Constraints

- Database target stays local SQL Server; do not point migrations or tests at a remote database.
- No real payment provider, refund, webhook, buyer contact/card/bank storage, email receipt, reward grant or game delivery.
- Only explicitly purchasable published revisions may be checked out; older/current offers default to non-purchasable.
- API price and order totals are server authoritative; use checked integer VND arithmetic and immutable order snapshots.
- Use one OpenAPI contract and RFC 9457 Problem Details for errors; UUID values serialize as strings and timestamps are UTC.
- Keep Admin changes protected by existing authorization, CSRF and ETag conventions; public order reads are forbidden.
- Preserve `design-system/nova-haven/MASTER.md`, Vietnamese content, WCAG AA contrast, visible focus, 44px targets, reduced motion and 320–390px overflow-free layouts.

## Review Focus

- Client price/total tampering: checkout ignores all client prices and recalculates from the current published revision.
- Duplicate submit/retry: identical idempotency key and cart yields one order/payment; key reuse with different cart is 409.
- Offer withdrawn or made non-purchasable: checkout returns 409 and persists no partial records.
- Empty, duplicate, oversized and overflowing cart: validation returns 400/422 with no partial records.
- Privacy/authorization boundary: no anonymous order lookup or customer PII/payment secret; Admin history is 401/403 protected.

---

### Task 1: Commerce purchase policy and order domain

**Files:**
- Create: `backend/NovaHaven.Application/Commerce/CommerceCheckoutValidator.cs`
- Create: `backend/NovaHaven.Domain/Commerce/CommerceOrder.cs`
- Modify: `backend/NovaHaven.Domain/Commerce/CommerceOffer.cs`
- Modify: `backend/NovaHaven.Application/Commerce/CommerceValidator.cs`
- Test: `tests/backend/NovaHaven.Domain.Tests/RewardsCommerceTests.cs`
- Test: `tests/backend/NovaHaven.Domain.Tests/CommerceCheckoutTests.cs`

**Interfaces:**
- Produces: `CommerceOffer.DraftIsPurchasable`, nullable `DraftPriceMinorUnits`; revision `IsPurchasable` and `PriceMinorUnits`; `CommerceCheckoutValidator.Validate(CommerceCheckoutInput)` and order/order-line/payment snapshot entities.
- Checkout limits: 1–20 distinct slugs, quantity 1–99, VND unit price 1–1,000,000,000,000, checked totals.

- [x] **Step 1: Write failing domain tests** for default-disabled offer revisions, valid line bounds, duplicate/empty slug rejection, quantity/line limits and amount overflow.
- [x] **Step 2: Run** the checkout domain tests and confirm failures were missing behavior.
- [x] **Step 3: Implement** offer snapshot properties, immutable order models and the pure checkout validator.
- [x] **Step 4: Run** the focused and full domain suites; 49/49 pass.

### Task 2: EF Core order persistence and additive migration

**Files:**
- Modify: `backend/NovaHaven.Infrastructure/Data/NovaDbContext.cs`
- Modify: `backend/NovaHaven.Infrastructure/Data/Migrations/NovaDbContextModelSnapshot.cs` (generated)
- Create: `backend/NovaHaven.Infrastructure/Data/Migrations/<timestamp>_AddLocalDemoCommerce.cs` (generated)
- Test: `tests/backend/NovaHaven.Integration.Tests/LocalDemoCommerceApiIntegrationTests.cs`

**Interfaces:**
- Produces: DbSets/configuration for `CommerceOrders`, `CommerceOrderLines`, and `CommercePayments`; unique order reference/idempotency indexes; restrictive FKs to offer and revision history.
- Migration adds purchase columns with safe defaults preserving all existing offers as not purchasable, plus the three order/payment tables.

- [x] **Step 1: Add LocalDB integration coverage** for persistence, indexes, snapshots and API behaviors.
- [x] **Step 2: Run** the focused test before implementation; initial model gaps were observed. Later SQL fixture attempts are blocked before test bodies by LocalDB error 50.
- [x] **Step 3: Add EF mappings and generate** the additive `AddLocalDemoCommerce` migration.
- [x] **Step 4: Review the migration** for add-only changes, VND/default-disabled compatibility and restrictive history relationships. It was not applied because the LocalDB fixture cannot initialize.
- [ ] **Step 5: Pass** the focused integration suite after migration on a disposable SQL Server database; model check is clean (`has-pending-model-changes` reports no changes).

### Task 3: Transactional checkout and private Admin history API

**Files:**
- Create: `backend/NovaHaven.Api/Endpoints/CommerceOrderEndpoints.cs`
- Modify: `backend/NovaHaven.Api/Endpoints/CommerceEndpoints.cs`
- Modify: `backend/NovaHaven.Api/Program.cs`
- Modify: `tests/backend/NovaHaven.Integration.Tests/RewardsCommerceApiIntegrationTests.cs`
- Test: `tests/backend/NovaHaven.Integration.Tests/LocalDemoCommerceApiIntegrationTests.cs`

**Interfaces:**
- Consumes: domain checkout validator and new EF model from Tasks 1–2.
- Produces: `POST /api/v1/commerce/orders` accepting `{items:[{slug,quantity}]}` plus UUID `Idempotency-Key`; `GET /api/v1/admin/commerce/orders?page=1&pageSize=20` requiring `AdminOnly`.
- Successful response contains order reference, UTC timestamp, VND total, item snapshots, and fixed `paymentStatus:"simulated"`, `paymentMethod:"localDemo"`, `realCharge:false`, `fulfilment:"none"`.

- [x] **Step 1: Write integration tests** for authoritative total/snapshot, altered client price ignored, same-key idempotency, key/cart mismatch 409, unavailable offer rejection, authorization and no public lookup.
- [ ] **Step 2: Execute** those tests against SQL Server; the attempt stops in fixture initialization with LocalDB error 50 before any assertions run.
- [x] **Step 3: Implement** offer DTO purchase fields and order endpoint; resolve current published revisions and insert order, lines and LocalDemo payment in one Serializable transaction; map validation/conflict errors to Problem Details.
- [ ] **Step 4: Run** focused integration tests and the full integration project; verify no duplicate order/payment and no partial records on failure.

### Task 4: Storefront, browser cart and checkout UI

**Files:**
- Create: `apps/web/src/lib/commerce-cart.ts`
- Create: `apps/web/app/CartStatus.tsx`
- Create: `apps/web/app/commerce/CommerceStore.tsx`
- Create: `apps/web/app/cart/page.tsx`
- Create: `apps/web/app/cart/CartClient.tsx`
- Create: `apps/web/app/checkout/page.tsx`
- Create: `apps/web/app/checkout/CheckoutClient.tsx`
- Create: `apps/web/app/commerce/store.css`
- Modify: `apps/web/app/commerce/page.tsx`
- Modify: `apps/web/src/lib/commerce-api.ts`
- Modify: `apps/web/app/SiteNavigation.tsx`
- Modify: `apps/web/app/layout.tsx`
- Test: `tests/web/commerce-cart.test.mjs`
- Test: `tests/web/reward-commerce.test.mjs`

**Interfaces:**
- Produces: browser-cart helpers `readCart`, `setCartQuantity`, `removeCartItem`, `cartCount`; `CommerceStore` add-to-cart control; checkout client posts only slugs/quantities with a stable request key.
- Store uses existing `/api` rewrite; page copy always discloses local simulation/no charge/no fulfilment.

- [x] **Step 1: Write failing Node tests** for malformed cart recovery, slug de-duplication, quantity bounds and cart count; update contract assertions.
- [x] **Step 2: Run** the focused suite and confirm it failed on the missing helper module.
- [x] **Step 3: Implement** pure cart helpers and storefront/cart/checkout components using current public offer prices and unavailable-item feedback.
- [x] **Step 4: Add** global cart count, accessible checkout/error states and responsive dark-earth styles.
- [x] **Step 5: Run** focused Node tests, full `npm test`, web typecheck and production build. Browser inspection at 320px/desktop with real API offers remains unverified.

### Task 5: Admin offer pricing, order review and contract/docs reconciliation

**Files:**
- Modify: `apps/web/app/admin/RewardCommerceManager.tsx`
- Modify: `contracts/openapi/wiki-v1.json`
- Modify: `tests/web/all-systems-smoke.test.mjs`
- Modify: `scripts/all-systems-smoke.mjs`
- Modify: `tests/web/demo-seed.test.mjs`
- Modify: `openspec/changes/add-reward-commerce-definitions/specs/reward-commerce/spec.md`
- Modify: `openspec/changes/add-local-demo-commerce/tasks.md`
- Modify: `README.md`
- Modify: `docs/roadmap/48H-ALL-SYSTEMS-STATUS.md`

**Interfaces:**
- Admin can opt an offer into local demo purchasing and set the VND amount; Admin order list is read-only and visibly simulated.
- OpenAPI becomes authoritative for the new checkout/order and purchase-price contracts; obsolete “no checkout endpoint” smoke assertions are replaced by local-demo boundary checks.

- [x] **Step 1: Write failing Node assertions** for OpenAPI request/response/header contracts, Admin pricing/history and safe smoke behavior; retain the no-fabricated-commerce seed rule.
- [x] **Step 2: Run** focused Node tests and observe missing-contract/smoke assertions.
- [x] **Step 3: Implement** Admin pricing/history, OpenAPI schemas and paths, non-mutating smoke checks and documentation status.
- [ ] **Step 4: Pass** all SQL-backed integration tests and `npm run smoke:all` against a uniquely named disposable LocalDB/API; blocked by LocalDB startup in this execution context.
- [x] **Step 5: Record exact evidence and remaining limits**; `NovaHaven_Local` was not migrated or changed.
