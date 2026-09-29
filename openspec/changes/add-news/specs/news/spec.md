## Purpose

Cung cấp kênh News/changelog độc lập với Wiki, có draft/published isolation và dùng chung authentication/CSRF/ETag của modular monolith.

## ADDED Requirements

### Requirement: Published-only public news
Public clients SHALL read only NewsPost ở trạng thái Published qua `/api/v1/news`; draft và unpublished không được lộ qua list hoặc detail.

### Requirement: Admin news lifecycle
Admin SHALL tạo, sửa, publish và unpublish NewsPost với CSRF cho mutation và If-Match cho mọi thay đổi sau khi tạo.

### Requirement: Local persistence
NewsPost SHALL dùng SQL Server local qua EF Core migration và không dùng chung bảng revision/media của Wiki.

### Requirement: Safe rendering
News Markdown SHALL đi qua cùng renderer/sanitizer an toàn của website; raw HTML/script/event handler không được thực thi.

### Requirement: Readable News card previews
Public News cards SHALL display a stable preview image. The website SHALL prefer the first safe Markdown image in a published post and SHALL use a sourced, credited fallback image when no safe image is present. Image captions SHALL link to the source and license where attribution is required.

#### Scenario: Published post includes a safe image
- **GIVEN** a published News post begins with a Markdown image from an approved image host.
- **WHEN** the News list is rendered.
- **THEN** the image's URL and descriptive alt text are used in the card preview.

#### Scenario: Image is missing or unsafe
- **GIVEN** a published News post has no image or contains an image URL outside the approved host list.
- **WHEN** the News list is rendered.
- **THEN** the unsafe image is ignored and a stable, credited fallback preview is shown.
