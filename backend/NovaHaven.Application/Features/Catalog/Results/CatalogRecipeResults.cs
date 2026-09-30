using NovaHaven.Domain.Catalog.Entities;

namespace NovaHaven.Application.Features.Catalog.Results;

public sealed record CatalogRecipeListItemResult(
    Guid Id,
    string Slug,
    string Name,
    string Summary,
    int Revision,
    DateTimeOffset PublishedAt,
    DateTimeOffset UpdatedAt);

public sealed record CatalogRecipePageResult(
    IReadOnlyList<CatalogRecipeListItemResult> Items,
    int Page,
    int PageSize,
    int Total);

public sealed record CatalogRecipeComponentResult(Guid ItemId, string ItemSlug, string ItemName, int Quantity);

public sealed record CatalogRecipeDraftComponentResult(Guid ItemId, string ItemName, int Quantity);

public sealed record CatalogRecipeDetailResult(
    Guid Id,
    string Slug,
    string Name,
    string Summary,
    string Markdown,
    int Revision,
    DateTimeOffset PublishedAt,
    IReadOnlyList<CatalogRecipeComponentResult> Ingredients,
    IReadOnlyList<CatalogRecipeComponentResult> Outputs);

public sealed record CatalogRecipeAdminListItemResult(
    Guid Id,
    string Slug,
    string Name,
    CatalogItemState State,
    int LatestRevisionNumber,
    DateTimeOffset UpdatedAt,
    uint RowVersion);

public sealed record CatalogRecipeAdminDraftResult(
    Guid Id,
    string Slug,
    string Name,
    string Summary,
    string Markdown,
    CatalogItemState State,
    int LatestRevisionNumber,
    IReadOnlyList<CatalogRecipeDraftComponentResult> Ingredients,
    IReadOnlyList<CatalogRecipeDraftComponentResult> Outputs,
    uint RowVersion);

public sealed record CatalogItemPublicationReferenceResult(
    Guid ItemId,
    CatalogItemState State,
    Guid? PublishedRevisionId);

public sealed record CatalogRecipeWriteResult(Guid Id, string Slug, uint RowVersion);

public sealed record CatalogRecipePublishResult(
    Guid Id,
    Guid RevisionId,
    int Revision,
    DateTimeOffset PublishedAt,
    uint RowVersion);
