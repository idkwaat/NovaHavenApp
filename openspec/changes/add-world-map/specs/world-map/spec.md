# Interactive World Map

## ADDED Requirements

### Requirement: Public world map shows source-derived terrain

The website SHALL provide a public `/map` view that renders terrain from a locally supplied Minecraft Java world save. The map SHALL be available without the REST API and SHALL identify missing or invalid map assets instead of presenting placeholder terrain as real data.

#### Scenario: Rendered world is available

- **GIVEN** the local renderer generated a valid terrain raster and manifest from a Java world ZIP
- **WHEN** an anonymous visitor opens `/map`
- **THEN** the visitor sees the derived terrain atlas and its coordinate convention

#### Scenario: Map assets are unavailable

- **GIVEN** the bundled manifest or terrain image is missing
- **WHEN** an anonymous visitor opens `/map`
- **THEN** the page explains that no rendered world map is installed and does not show fabricated terrain

#### Scenario: Fixed-angle isometric view is available

- **GIVEN** the local renderer emitted a valid one-block surface-color raster and height field
- **WHEN** an anonymous visitor opens `/map` on a browser with WebGL2 and adequate texture support
- **THEN** the visitor sees the source-derived terrain in a fixed elevated isometric view, without camera-rotation controls
- **AND** zooming exposes individual sampled block surface colors while terrain height remains source-derived

#### Scenario: Three-dimensional renderer is unavailable

- **GIVEN** WebGL2 is unavailable, the device texture limit is too small, or the local height field cannot be loaded
- **WHEN** the visitor opens or changes to `/map`
- **THEN** the usable two-dimensional atlas remains available with the same block-coordinate navigation
- **AND** the page explains the fallback instead of presenting a blank or fabricated map

### Requirement: Map navigation exposes world block coordinates

The map SHALL support zooming and panning with pointer/touch and accessible keyboard/buttons. Selecting a point SHALL display the containing Minecraft block's X/Z coordinates; copying them SHALL be an explicit user action. X SHALL increase rightward and Z downward.

#### Scenario: Select a world coordinate

- **GIVEN** a visitor selects a visible map point
- **WHEN** the selection is resolved against the rendered bounds
- **THEN** the UI shows the correct integer block X/Z, not chunk coordinates or geographic latitude/longitude

#### Scenario: Navigate without a pointer

- **GIVEN** keyboard focus is on the map or its controls
- **WHEN** the visitor uses arrow keys, `+`, `-`, Home, or the zoom/pan buttons
- **THEN** the map can be explored without dragging or scrolling a pointer

#### Scenario: Pan and zoom while preserving the fixed camera

- **GIVEN** the isometric terrain renderer is active
- **WHEN** the visitor pans or zooms with pointer, touch, wheel, keyboard or visible controls
- **THEN** the viewed world region changes and the scale stays between 1× and 32×
- **AND** the camera azimuth and elevation do not change

### Requirement: Map markers have traceable source data

The map SHALL show the saved spawn point only when it exists in the source save and lies within rendered terrain bounds. Optional sign markers SHALL use text and coordinates extracted from sign block entities only. The map SHALL NOT infer marker coordinates from CMS latitude/longitude or invent place names.

#### Scenario: Show saved spawn

- **GIVEN** `level.dat` contains a spawn X/Z inside the rendered bounds
- **WHEN** the map manifest is generated
- **THEN** the manifest includes that verified spawn point

#### Scenario: Exclude a missing or off-map spawn

- **GIVEN** spawn metadata is absent, incomplete or outside rendered bounds
- **WHEN** the map manifest is generated
- **THEN** no spawn marker is included

### Requirement: Source save is not republished

The renderer SHALL read only required level metadata and overworld region chunks, validate archive and Anvil bounds, and output only terrain pixels plus minimal source-derived map metadata. It SHALL NOT copy raw player, inventory, entity, seed or account data into public assets.

#### Scenario: Unsafe archive is rejected

- **GIVEN** a ZIP has traversal paths, excessive entry sizes, invalid region sectors or malformed NBT
- **WHEN** the renderer processes it
- **THEN** it exits with an error and does not emit a successful manifest

#### Scenario: Render from valid save

- **GIVEN** a valid world save
- **WHEN** the renderer completes
- **THEN** the output contains terrain and only minimal bounds, scale, spawn and sanitized sign labels
