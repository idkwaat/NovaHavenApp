# Catalog items

## Requirements

### Requirement: Admin can maintain typed catalog item drafts
The system MUST allow an Admin to create and edit a catalog item draft with a unique lowercase slug, name, summary, Markdown body and one of the supported kinds: `item`, `weapon`, `armor`, `material`, `fish`, or `crop`.

### Requirement: Publishing creates an immutable revision
When an Admin publishes a valid draft, the system MUST append a revision containing the draft values, publisher identity and UTC publish time. Existing revisions MUST NOT be changed.

### Requirement: Public catalog is published-only
Anonymous catalog list and detail endpoints MUST return only items with a current published revision. Draft and unpublished items MUST return no public detail.

### Requirement: Admin mutations are protected
Catalog mutations MUST require the existing Admin authorization policy, CSRF protection and an `If-Match` ETag for edits, publish and unpublish operations.

### Requirement: No external game truth is invented
The Catalog slice MUST store editorial content only. It MUST NOT claim to synchronize with Minecraft or write to a Minecraft database without an explicit external contract.

### Requirement: Recipe data must be source-backed
Recipes, drops and other item relationships MUST NOT be populated with guessed game values. They may be implemented as empty editorial structures, but visible data requires an approved source.
