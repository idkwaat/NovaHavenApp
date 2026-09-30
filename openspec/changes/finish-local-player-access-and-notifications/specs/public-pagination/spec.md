# Public collection pagination

## Requirements

- Public News, Wiki, Catalog items and recipes, Knowledge, Community, Rewards and Commerce collection screens use bounded server pagination.
- Collection responses preserve the `{ items, page, pageSize, total }` contract and have deterministic ordering so records do not arbitrarily move between pages.
- Web next/previous controls preserve active search and category filters, expose the current page accessibly and do not render invalid destinations. Flutter notification inbox navigation follows the same page contract.
- Invalid, negative, excessively large or overflowing page inputs return a validation Problem Details response rather than a server exception.

## Scenarios

### Scenario: Navigate a filtered collection

- **WHEN** a visitor advances to another page of a filtered collection
- **THEN** the selected filters remain active and the response contains only that page with accurate totals

### Scenario: Out-of-range page

- **WHEN** a page value cannot be safely converted to a database offset
- **THEN** the API returns a client validation error and does not execute an overflowing offset
