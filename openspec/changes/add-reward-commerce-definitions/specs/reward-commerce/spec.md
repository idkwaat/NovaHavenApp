# Reward and commerce definitions

## Purpose

Provide local editorial catalogs for reward definitions and commerce offers while keeping external execution out of the MVP.

## Requirements

- Admin can create, edit, publish and unpublish reward definitions with immutable revisions and ETags.
- Published reward definitions clearly require external acknowledgement; no grant endpoint is exposed.
- Admin can create, edit, publish and unpublish commerce offers with immutable revisions and ETags.
- Published commerce offers are definition-only; no checkout, order, payment, refund or webhook endpoint is exposed.
- Public reads are anonymous and published-only.
- Provider product codes are references only; secrets are rejected by validation.
