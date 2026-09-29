namespace NovaHaven.Api.Contracts.Wiki;

public sealed record WikiCategoryRequest(string Name, string Slug, int DisplayOrder);

public sealed record WikiCategoryUpdateRequest(string Name, string Slug, int DisplayOrder, bool IsActive);

public sealed record AdminWikiCategoryResponse(
    Guid Id,
    string Name,
    string Slug,
    int DisplayOrder,
    bool IsActive,
    string Etag);

public sealed record AdminWikiCategoryDetailResponse(
    Guid Id,
    string Name,
    string Slug,
    int DisplayOrder,
    bool IsActive);
