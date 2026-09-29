namespace NovaHaven.Application.Features.Wiki.Results;

public sealed record WikiCategoryAdminResult(
    Guid Id,
    string Name,
    string Slug,
    int DisplayOrder,
    bool IsActive,
    byte[] RowVersion);
