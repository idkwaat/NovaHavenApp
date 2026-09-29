using NovaHaven.Domain.Wiki.Entities;

namespace NovaHaven.Application.Features.Wiki.Results;

public sealed record WikiArticleDraftResult(
    Guid Id,
    string Slug,
    string Title,
    string Summary,
    string Markdown,
    Guid CategoryId,
    IReadOnlyList<Guid> TagIds,
    ArticleState State,
    IReadOnlyList<Guid> MediaIds,
    int LatestRevisionNumber,
    Guid? PublishedRevisionId,
    byte[] RowVersion);
