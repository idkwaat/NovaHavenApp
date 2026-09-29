using NovaHaven.Domain.Wiki.Entities;

namespace NovaHaven.Application.Features.Wiki.Results;

public sealed record WikiArticleAdminListItemResult(
    Guid Id,
    string Slug,
    string DraftTitle,
    ArticleState State,
    DateTimeOffset UpdatedAt,
    int LatestRevisionNumber);
