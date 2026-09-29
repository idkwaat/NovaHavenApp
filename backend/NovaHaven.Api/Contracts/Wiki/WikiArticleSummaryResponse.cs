namespace NovaHaven.Api.Contracts.Wiki;

public sealed record WikiArticleSummaryResponse(
    Guid Id,
    string Slug,
    string Title,
    string Summary,
    string Category,
    IReadOnlyList<string> Tags,
    DateTimeOffset PublishedAt,
    int Revision,
    string? PreviewImageUrl,
    string? PreviewImageAlt);

public sealed record WikiArticlePageResponse(
    IReadOnlyList<WikiArticleSummaryResponse> Items,
    int Page,
    int PageSize,
    int Total);
