using NovaHaven.Domain.Catalog.Entities;

namespace NovaHaven.Application.Features.Catalog.Results;

public sealed record CatalogItemListItemResult(
    Guid Id,
    string Slug,
    string Name,
    string Summary,
    CatalogItemKind Kind,
    int Revision,
    DateTimeOffset PublishedAt,
    DateTimeOffset UpdatedAt);

public sealed record CatalogItemPageResult(
    IReadOnlyList<CatalogItemListItemResult> Items,
    int Page,
    int PageSize,
    int Total);

public sealed record CatalogItemDetailResult(
    Guid Id,
    string Slug,
    string Name,
    string Summary,
    string Markdown,
    CatalogItemKind Kind,
    int Revision,
    DateTimeOffset PublishedAt,
    DateTimeOffset UpdatedAt);

public sealed record CatalogItemRevisionResult(
    Guid Id,
    int Number,
    string Name,
    string Summary,
    string Markdown,
    CatalogItemKind Kind,
    DateTimeOffset PublishedAt);

public sealed record CatalogItemAdminListItemResult(
    Guid Id,
    string Slug,
    string Name,
    CatalogItemKind Kind,
    CatalogItemState State,
    int LatestRevisionNumber,
    DateTimeOffset UpdatedAt,
    uint RowVersion);

public sealed record CatalogItemAdminDraftResult(
    Guid Id,
    string Slug,
    string Name,
    string Summary,
    string Markdown,
    CatalogItemKind Kind,
    CatalogItemState State,
    int LatestRevisionNumber,
    CatalogItemRevisionResult? PublishedRevision,
    uint RowVersion);

public sealed record CatalogItemWriteResult(Guid Id, string Slug, uint RowVersion);

public sealed record CatalogItemPublishResult(
    Guid Id,
    Guid RevisionId,
    int Revision,
    DateTimeOffset PublishedAt,
    uint RowVersion);
