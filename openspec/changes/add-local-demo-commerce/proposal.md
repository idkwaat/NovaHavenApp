# Add local demo commerce

## Why

Nova Haven currently publishes commerce definitions but has no shopping cart or order flow. The owner asked for a shop, purchasing, cart and checkout while keeping the project database local.

## What changes

- Extend published offers with an explicit purchasable flag and a numeric VND price.
- Add a browser-local cart and a guest checkout that creates an immutable order and payment simulation record in the local SQL database.
- Add read-only Admin order history and accessible Vietnamese storefront/cart/checkout pages.
- Keep the existing modular monolith and API contract; do not integrate a real payment provider or Minecraft grant/delivery adapter.

## Compatibility and scope

Unconfigured offers remain non-purchasable. A checkout is visibly marked as a local simulation and must never be described as a real charge, payment confirmation, or item delivery. Real payment, refunds, fulfilment, buyer contact collection and production deployment remain out of scope.
