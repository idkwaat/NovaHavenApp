# Web Wiki bookmarks

## ADDED Requirements

### Requirement: Readers can keep a browser-local Wiki reading list
The website SHALL let a reader save and remove published Wiki articles from a reading list stored only in that browser. Saving an article SHALL store its validated slug, not an article snapshot, credentials, or server-side user data. The list page SHALL resolve every stored slug through the anonymous published Wiki API and SHALL show only responses that still identify that same published slug. A missing or unpublished article SHALL not be rendered from stale browser data.

#### Scenario: Save and revisit a published article
- GIVEN an anonymous reader opens a published Wiki article
- WHEN the reader activates its save button
- THEN the article slug is stored locally and the button exposes its pressed state
- AND the article appears on the reading-list page after being revalidated through the public API

#### Scenario: Removed article is never served from a local snapshot
- GIVEN a saved article has since been unpublished or removed
- WHEN the reading list resolves that slug
- THEN the page omits its content and reports that the saved entry is no longer public

#### Scenario: Browser storage is unavailable
- GIVEN the browser blocks local storage
- WHEN the reader attempts to use the bookmark control
- THEN the page explains that local saving is unavailable and does not claim the entry was saved

#### Scenario: Temporary public API failure
- GIVEN one or more saved slugs exist
- WHEN the public Wiki API cannot be reached
- THEN the reading list preserves the local slugs, shows a retry action, and does not substitute stale article content

#### Scenario: Search saved articles in Vietnamese
- GIVEN the reading list contains current public articles
- WHEN the reader enters a title, summary, category or tag query
- THEN only matching saved articles remain visible
- AND Vietnamese diacritics and letter case do not prevent a match

### Requirement: Readers can reopen recently viewed published articles
The website SHALL maintain a browser-local history of at most 20 distinct Wiki article slugs in most-recently-viewed order. The history SHALL contain no article snapshots or account data, SHALL be clearable by the reader, and SHALL resolve every entry using the anonymous published Wiki API before displaying it. Only a successfully rendered public article may be recorded.

#### Scenario: Public article is added to recent history
- GIVEN an anonymous reader opens a published Wiki article
- WHEN the article page has loaded successfully
- THEN its slug is moved to the front of the local recent-history list
- AND reopening it does not create a duplicate entry

#### Scenario: Recent history is bounded and clearable
- GIVEN more than 20 different public articles have been opened
- WHEN the local history is read
- THEN only the 20 most recently viewed slugs remain
- AND the reader can clear this history without affecting saved bookmarks or other local preferences

#### Scenario: Unpublished recent article is hidden
- GIVEN an article in recent history has since been unpublished
- WHEN the reader opens the recent-history page
- THEN the current public API response is checked and no stale article content is displayed
