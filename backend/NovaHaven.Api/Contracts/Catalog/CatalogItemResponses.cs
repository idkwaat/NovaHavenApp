namespace NovaHaven.Api.Contracts.Catalog;

public sealed record CatalogItemListItemResponse(
    Guid Id,
    string Slug,
    string Name,
    string Summary,
    string Kind,
    int Revision,
    DateTimeOffset PublishedAt,
    DateTimeOffset UpdatedAt);

public sealed record CatalogItemPageResponse(
    IReadOnlyList<CatalogItemListItemResponse> Items,
    int Page,
    int PageSize,
    int Total);

public sealed record CatalogItemDetailResponse(
    Guid Id,
    string Slug,
    string Name,
    string Summary,
    string Markdown,
    string Kind,
    int Revision,
    DateTimeOffset PublishedAt,
    DateTimeOffset UpdatedAt);

public sealed record CatalogItemRevisionResponse(
    Guid Id,
    int Number,
    string Name,
    string Summary,
    string Markdown,
    string Kind,
    DateTimeOffset PublishedAt);

public sealed record CatalogItemAdminListItemResponse(
    Guid Id,
    string Slug,
    string Name,
    string Kind,
    string State,
    int LatestRevisionNumber,
    DateTimeOffset UpdatedAt,
    string Etag);

public sealed record CatalogItemAdminDraftResponse(
    Guid Id,
    string Slug,
    string Name,
    string Summary,
    string Markdown,
    string Kind,
    string State,
    int LatestRevisionNumber,
    CatalogItemRevisionResponse? Published,
    string Etag);

public sealed record CatalogItemCreateResponse(Guid Id, string Slug, string Etag);
public sealed record CatalogItemWriteResponse(Guid Id, string Etag);
public sealed record CatalogItemPublishResponse(Guid Id, Guid RevisionId, int Number, DateTimeOffset PublishedAt, string Etag);
