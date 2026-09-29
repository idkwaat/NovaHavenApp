# Local demo commerce design

## Architecture

Keep commerce inside the existing ASP.NET Core modular monolith and SQL Server EF Core context. Next.js uses the existing same-origin `/api` rewrite. The client cart stores only offer slugs and quantities; names, prices, availability, totals and order snapshots are authoritative on the API. No new service, provider SDK, account system, Minecraft integration or online database is introduced.

## Data and transaction

- A draft/published offer gains `IsPurchasable` and integer `PriceMinorUnits`, with VND as the only supported currency for this local slice. Existing offers default to not purchasable.
- Published offer revisions snapshot those values alongside the current descriptive data.
- A checkout order snapshots its generated reference, created-at UTC, VND total and immutable line data (offer/revision references, name, slug, quantity, unit price and line total).
- A one-to-one payment simulation row records `LocalDemo` / `Simulated`, the order amount/currency and UTC time. It contains no provider transaction data.
- Checkout resolves every unique requested slug against its currently published revision, verifies it is purchasable and priced, enforces 1–20 unique lines, quantity 1–99 and unit price up to 1,000,000,000,000 VND, and computes totals with checked integer arithmetic. Order, lines and simulated payment are written in one Serializable SQL transaction.
- A required `Idempotency-Key` UUID is unique per order and is stored with a fingerprint of the normalized cart; retrying the same key and cart returns the original receipt, while reusing it for a different cart returns 409. No buyer email/address or payment secret is persisted.

## API surfaces

- Extend existing commerce offer read/admin contracts with numeric VND price and purchase availability.
- `POST /api/v1/commerce/orders` is anonymous because the app has no customer accounts. It accepts only slugs and quantities and requires `Idempotency-Key`; its response clearly says payment was simulated locally and no real payment or fulfilment occurred.
- `GET /api/v1/admin/commerce/orders` is Admin-only and read-only, returning a paged list with line snapshots and simulation status. Cookie-authenticated mutations remain protected by the existing CSRF policy; checkout has no cookie-authenticated customer mutation.
- Update OpenAPI and local integration tests before treating the endpoint as supported.

## Web flow

`/commerce` becomes a storefront that retains published non-purchasable definitions but only offers “add to cart” for explicitly purchasable revisions. `/cart` lets users edit quantities/remove entries; `/checkout` reloads current offer data, shows the server-confirmed subtotal and asks the user to acknowledge the no-charge simulation before submitting. The receipt stays in the checkout UI and contains only a non-sensitive order reference, items, total and `Mô phỏng local — không thu tiền/không giao vật phẩm` status. A cart link is added to global navigation.

Use the approved dark-earth tokens and editorial compositions in `design-system/nova-haven/MASTER.md`. Keep Vietnamese copy, visible focus, labeled controls, 44px touch targets, responsive single-column mobile layout, inline validation and a focusable error summary. Do not add generated or uncredited art.

## Failure behavior and verification

Invalid/duplicate/too-large carts return validation Problem Details; unpublished or disabled offers return conflict without creating partial order/payment records. A price or publication change during checkout is resolved from the transaction's authoritative current revision, never from client totals. Admin order reads require Admin authorization. Verify validator unit tests, SQL Server integration (offer lifecycle, total snapshots, idempotency, rollback/disabled offer, Admin guard), Node cart tests, OpenAPI contract, web typecheck/build, and 320–390px responsive/keyboard UI checks. Report if LocalDB is unavailable; do not substitute structural tests for database acceptance.
