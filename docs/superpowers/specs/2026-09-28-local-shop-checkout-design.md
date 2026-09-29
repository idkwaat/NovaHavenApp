# Nova Haven local shop and demo checkout

**Status:** Implementation design for the owner-requested shop/cart/checkout slice.

**Behavior source:** [`openspec/changes/add-local-demo-commerce/specs/commerce-checkout/spec.md`](../../../openspec/changes/add-local-demo-commerce/specs/commerce-checkout/spec.md)

## Context and intent

The project already has public Commerce offer definitions, Admin revision editing, EF Core persistence and a single OpenAPI contract, but it deliberately has no checkout, order or payment endpoints. This change adds a usable local demo-store flow without turning the site into a real online shop. The earlier owner instruction keeps the database local; this design therefore records simulated transactions only and makes no real charge.

## Chosen approach

1. Keep the existing Commerce aggregate and extend it with an explicit purchase switch and bounded integer VND price; a published revision snapshots both. Definitions are not purchasable unless an Admin opts in.
2. Persist immutable local demo orders, order lines and one simulated payment record in the existing SQL Server database. Checkout validates offer state and computes totals inside a Serializable transaction. A client-supplied UUID idempotency key prevents accidental duplicate orders.
3. Keep the browser cart small and local: only offer slug and quantity are stored. The server remains authoritative for price and availability. A guest checkout collects no personal or payment information because this simulation has no fulfilment.
4. Add a read-only Admin order history; no refunds, edits, gateway callbacks, reward grants or delivery are included.
5. Reuse the existing Next.js App Router and same-origin `/api` rewrite. Preserve the approved dark-earth site direction and provide `/commerce`, `/cart`, `/checkout`, and a global cart entry.

## Alternatives considered

- Real payment provider: rejected for this slice because no provider, merchant account, test credentials, callback domain or refund/fulfilment design has been supplied. It cannot be honestly or safely completed as a local-only change.
- Frontend-only checkout mock: rejected because the user requested an implemented flow; it would lose orders on refresh and could not verify transactional totals/idempotency.
- Server-side customer cart/account: rejected because this application currently has no customer identity subsystem and the local demo does not need cross-device carts.

## Data flow

The storefront fetches published offers. Add-to-cart writes `{slug, quantity}` to browser storage. Cart/checkout resolves slugs against the public API and flags stale entries. On confirmation, Next.js sends only slugs/quantities plus an `Idempotency-Key` UUID to `POST /api/v1/commerce/orders`. The API loads current published revisions, validates purchase eligibility and amounts, calculates totals, inserts order/line/payment snapshots atomically, then returns a safe receipt summary. Retrying the same key and same cart returns that order; reusing the key with a different cart returns 409. The browser shows a receipt marked “Mô phỏng local — không thu tiền, không giao vật phẩm.”

## Data model and invariants

- `CommerceOffer`: draft purchase flag defaults false and draft numeric price is nullable; currency is fixed to VND for this MVP.
- `CommerceOfferRevision`: immutable purchase flag/price snapshot.
- `CommerceOrder`: opaque reference, UTC creation time, VND total, unique idempotency UUID and cart fingerprint.
- `CommerceOrderLine`: immutable offer/revision/name/slug/quantity/unit-price/line-total snapshot.
- `CommercePayment`: one-to-one with order; status and method are fixed to `Simulated` and `LocalDemo`.
- No email, postal address, card/bank information, provider secret or game entitlement is stored.
- Bounds: 1–20 unique lines, quantity 1–99, positive VND price up to 1,000,000,000,000 VND per unit; totals use checked 64-bit integer arithmetic.

## API and security

- Extend public and Admin Commerce offer DTOs without leaking draft purchase fields.
- `POST /api/v1/commerce/orders`: anonymous, bounded request, required `Idempotency-Key`, validation Problem Details, current-price resolution and one SQL transaction.
- `GET /api/v1/admin/commerce/orders`: Admin-only, paginated and read-only; no public order lookup endpoint.
- Retain Admin authorization, CSRF, rowversion/ETag rules on offer mutations. Checkout has no cookie-backed customer session; it accepts no client price or payment data.
- Update OpenAPI, endpoint integration tests, local smoke scripts, seed guidance and README/readiness notes. The existing completed Commerce change's former “no checkout endpoint” assertion must be reconciled with this newer change.

## User experience

The shop remains within the current earthy palette and surface hierarchy. Product cards show factual offer text and current server price; no generated/game artwork or fabricated gameplay effects are added. Cart supports quantity edits/removal and an empty state. Checkout presents the lines and total, explains the simulation before confirmation, prevents a second submit while pending, maps 4xx errors back to the cart and provides a keyboard-focusable error summary plus field/line-level details. All key links and controls remain at least 44px, Vietnamese labels wrap without horizontal scrolling at 320px, and reduced-motion settings are respected.

## Verification and boundaries

Run Domain tests, LocalDB-backed API integration tests, Node contract/cart tests, `npm test`, web typecheck and production build. If LocalDB is unavailable in the current process account, state that relational tests were blocked; do not claim SQL checkout acceptance based on structural checks. Keep the existing preview DB untouched during test setup; use the integration fixture's unique disposable database. Real provider acceptance and any Minecraft fulfilment remain explicitly not done.
