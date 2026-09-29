# Commerce checkout

## Purpose

Extend the editorial Commerce catalog with a local-only shop and simulated checkout without claiming real payment or game fulfilment.

## Requirements

### Requirement: Explicit purchasable offers

An Admin MAY mark a Commerce offer purchasable and set a positive VND price. Offers remain non-purchasable by default. Publishing MUST snapshot the purchase flag and numeric price into the immutable revision. Public clients MUST receive the current published revision's purchase state and price; draft values MUST remain private.

#### Scenario: Published purchasable offer

- GIVEN an Admin has enabled purchasing and set a valid VND price on an offer
- WHEN that offer is published
- THEN the public detail and storefront expose the published price and allow adding the offer to a cart

#### Scenario: Definition-only offer

- GIVEN an offer is not enabled for purchasing
- WHEN it is published and read publicly
- THEN its definition may be viewed, but checkout MUST reject it and it MUST NOT be represented as buyable

### Requirement: Local cart

The web client MUST support adding, removing and changing quantities for offers. The cart MAY persist on the current browser only and MUST store identifiers and quantities, not trusted prices or personal data. The storefront and cart MUST show current server data and clearly handle offers that became unavailable.

#### Scenario: Cart quantity changes

- GIVEN a user has one or more offers in the browser cart
- WHEN the user increments, decrements or removes a line
- THEN the cart count and displayed subtotal update from current public offer data

#### Scenario: Cart offer becomes unavailable

- GIVEN an offer in the browser cart is unpublished or no longer purchasable
- WHEN the user opens the cart or checkout
- THEN the UI identifies the unavailable line and prevents submitting that line

### Requirement: Server-priced demo checkout

The public API MUST accept a cart containing 1–20 distinct published purchasable offers with quantities from 1–99 and VND unit prices no greater than 1,000,000,000,000, together with a required UUID `Idempotency-Key`. It MUST ignore client-supplied prices, recompute totals from current published revisions using checked integer arithmetic, and create the order, immutable line snapshots and payment simulation atomically in SQL Server. Invalid input MUST NOT create partial records.

#### Scenario: Successful simulated checkout

- GIVEN every requested offer is currently published, purchasable and priced in VND
- WHEN the user confirms the clearly labeled local demo checkout
- THEN one order, its immutable line snapshots and one `LocalDemo` payment record are stored in a single transaction
- AND the response reports the authoritative VND total and a non-sensitive order reference
- AND the response states that no real money was charged and no game item/benefit was delivered

#### Scenario: Invalid or unavailable cart

- GIVEN a cart is empty, contains duplicate slugs, exceeds its line/quantity limits, or references a missing/unpublished/non-purchasable offer
- WHEN checkout is submitted
- THEN the API returns a consistent 4xx Problem Details response and stores no order, line or payment record

#### Scenario: Idempotent retry

- GIVEN checkout with an `Idempotency-Key` has already created an order
- WHEN the same key is submitted again
- THEN the API returns the original order receipt and MUST NOT create a second order or payment
- AND reuse of that key with a different cart returns 409 without creating additional records

### Requirement: Demo payment boundary

Every payment created by this local slice MUST be marked `LocalDemo` and `Simulated`. The flow MUST NOT connect to an external gateway, store provider credentials, collect card/bank data, send payment webhooks, issue refunds, send receipt emails, grant rewards, or claim fulfilment. UI, API and documentation MUST clearly distinguish simulation from real transactions.

#### Scenario: Local checkout is not real payment

- GIVEN a visitor completes the local demo checkout
- WHEN the order is shown
- THEN the visitor sees that the status is simulated, no money was collected, and no in-game item or benefit was delivered

### Requirement: Private order review

Only an Admin MAY read the local demo order history. The API MUST expose no anonymous order lookup by predictable identifier and MUST persist no buyer email, address, card or bank details for this simulation.

#### Scenario: Anonymous order list

- GIVEN an unauthenticated visitor
- WHEN they request the Admin order history
- THEN the API responds 401

#### Scenario: Admin order list

- GIVEN an authenticated Admin
- WHEN they request local order history
- THEN the API returns paged order and immutable line summaries with simulation status and UTC timestamps
