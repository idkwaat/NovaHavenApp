namespace NovaHaven.Application.Features.Wiki.Results;

public sealed record WikiArticlePageResult(
    IReadOnlyList<WikiArticleSummaryResult> Items,
    int Page,
    int PageSize,
    int Total);
