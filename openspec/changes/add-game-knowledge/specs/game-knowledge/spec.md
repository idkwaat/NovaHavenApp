# Game Knowledge Graph

## ADDED Requirements

### Requirement: Editorial knowledge records have typed lifecycle
The system SHALL allow an Admin to create, edit, publish and unpublish typed records with kind `npc`, `quest`, `location` or `season`. Each published operation SHALL create an immutable revision and each anonymous read SHALL return only the current published revision.

#### Scenario: Draft privacy
- GIVEN an Admin creates an NPC draft
- WHEN an anonymous client requests its slug
- THEN the API returns 404 until that draft is published

#### Scenario: Immutable re-publication
- GIVEN a published location is edited in a new draft
- WHEN the Admin publishes the edit
- THEN the public response returns a new revision and the previous revision remains unchanged

### Requirement: Typed metadata is validated
The system SHALL validate NPC role, Quest difficulty/steps/reward description, World Atlas coordinate bounds and Seasonal start/end ordering before saving or publishing.

#### Scenario: Invalid atlas coordinates
- GIVEN a location has latitude outside -90..90 or longitude outside -180..180
- WHEN the Admin saves it
- THEN the API returns field validation errors and stores no invalid draft

#### Scenario: Invalid seasonal window
- GIVEN a season ends before it starts
- WHEN the Admin saves it
- THEN the API returns a validation error for `endsAt`

### Requirement: Published graph links are snapshots
The system SHALL permit typed links between Knowledge records and published Catalog items. Publication SHALL reject missing or unpublished targets and SHALL snapshot target revision IDs, names, slugs and kinds into the published Knowledge revision.

#### Scenario: Unpublished target is blocked
- GIVEN a Quest draft links to an unpublished NPC
- WHEN the Admin attempts to publish the Quest
- THEN the API returns validation errors and leaves the Quest unpublished

#### Scenario: Published link is readable
- GIVEN an NPC is published with a published World Atlas location relation
- WHEN an anonymous client reads the NPC
- THEN the response includes the location's published name and slug

### Requirement: Clients consume the same contract
The web and Flutter clients SHALL expose published Knowledge lists/details, typed metadata and graph links. Admin web SHALL expose typed CRUD and publish controls with ETag preconditions.

### Requirement: Published Knowledge card previews
Public Knowledge summaries SHALL expose a nullable preview image URL from the current published revision's NPC portrait or location map image. The website SHALL use a sourced, credited fallback image when a summary has no valid preview URL.

#### Scenario: NPC and location previews follow the published revision
- **GIVEN** a published NPC has a portrait URL or a published location has a map image URL.
- **WHEN** an anonymous client lists that Knowledge kind.
- **THEN** the summary returns the image URL from that published revision, not from a newer draft.

#### Scenario: Knowledge record has no preview
- **GIVEN** a published Knowledge revision has no supported image metadata.
- **WHEN** the website renders its card.
- **THEN** the website shows a stable, credited fallback image without inventing a portrait or map.
