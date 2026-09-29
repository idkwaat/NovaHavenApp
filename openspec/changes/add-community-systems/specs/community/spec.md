# Community Systems

## ADDED Requirements

### Requirement: Community records use published revisions
The system SHALL support editorial Event, Guild, Player, Housing and Leaderboard records with Admin CRUD, ETag-protected edits, publication and unpublication. Public clients SHALL see only current published revisions.

#### Scenario: Event privacy and publication
- GIVEN an Admin creates an Event draft
- WHEN an anonymous client requests its slug before publication
- THEN the API returns 404
- WHEN the Admin publishes it
- THEN the public Event detail includes its current immutable revision metadata

### Requirement: Events manage local participants
The system SHALL let an Admin add, list and approve/reject local Event registrations. Registration records SHALL be community-owned and SHALL not claim a Minecraft account, gameplay result or in-game reward.

#### Scenario: Registration moderation
- GIVEN a published Event
- WHEN an Admin adds a participant
- THEN the participant starts as `pending` and can be changed with an ETag-protected Admin mutation

### Requirement: Editorial leaderboards are explicit
Leaderboard rows SHALL store participant display names, scores and editorial notes. They SHALL be labeled as editorial standings and SHALL not be presented as authoritative Minecraft gameplay statistics without an external adapter.

#### Scenario: Standings snapshot
- GIVEN a leaderboard draft with unique ranks
- WHEN the Admin publishes it
- THEN the public response contains an immutable sorted standings snapshot

### Requirement: Community relations respect Knowledge publication
Event and Housing location links SHALL target published World Atlas revisions. Leaderboard season links SHALL target published Seasonal Hub revisions. Publication SHALL reject missing or unpublished targets.

### Requirement: Clients expose community slices
The Next.js public site, Admin CMS and Flutter reader SHALL expose the five community kinds using the shared REST contract. No client SHALL store a long-lived authentication secret in localStorage.
