namespace NovaHaven.Api.Contracts.Catalog;

public sealed record CatalogRecipeListItemResponse(
    Guid Id,
    string Slug,
    string Name,
    string Summary,
    int Revision,
    DateTimeOffset PublishedAt,
    DateTimeOffset UpdatedAt);

public sealed record CatalogRecipePageResponse(
    IReadOnlyList<CatalogRecipeListItemResponse> Items,
    int Page,
    int PageSize,
    int Total);

public sealed record CatalogRecipeComponentResponse(Guid ItemId, string ItemSlug, string ItemName, int Quantity);
public sealed record CatalogRecipeDraftComponentResponse(Guid ItemId, string ItemName, int Quantity);

public sealed record CatalogRecipeDetailResponse(
    Guid Id,
    string Slug,
    string Name,
    string Summary,
    string Markdown,
    int Revision,
    DateTimeOffset PublishedAt,
    IReadOnlyList<CatalogRecipeComponentResponse> Ingredients,
    IReadOnlyList<CatalogRecipeComponentResponse> Outputs);

public sealed record CatalogRecipeAdminListItemResponse(
    Guid Id,
    string Slug,
    string Name,
    string State,
    int LatestRevisionNumber,
    DateTimeOffset UpdatedAt,
    string Etag);

public sealed record CatalogRecipeAdminDraftResponse(
    Guid Id,
    string Slug,
    string Name,
    string Summary,
    string Markdown,
    string State,
    int LatestRevisionNumber,
    IReadOnlyList<CatalogRecipeDraftComponentResponse> Ingredients,
    IReadOnlyList<CatalogRecipeDraftComponentResponse> Outputs,
    string Etag);

public sealed record CatalogRecipeCreateResponse(Guid Id, string Slug, string Etag);
public sealed record CatalogRecipeWriteResponse(Guid Id, string Etag);
public sealed record CatalogRecipePublishResponse(Guid Id, Guid RevisionId, int Number, DateTimeOffset PublishedAt, string Etag);
