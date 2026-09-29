# Integration capability foundation

## Purpose

Expose safe local capability state for Minecraft and commerce integrations without pretending an external adapter or payment provider exists.

## Requirements

- Admin can inspect known capability keys and update only allowed status transitions with cookie auth, CSRF and `If-Match`.
- Public status exposes capability, owner, status and safe message only; credentials and secret material are rejected.
- `Unavailable` cannot jump directly to `Healthy`; a configured adapter must establish that transition.
- The foundation does not execute Minecraft sync, reward grants, checkout, orders or payment webhooks.
