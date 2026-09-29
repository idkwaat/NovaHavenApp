namespace NovaHaven.Application.Features.Wiki.Results;

public sealed record WikiArticleResult(
    Guid Id,
    string Slug,
    string Title,
    string Summary,
    string Markdown,
    string Category,
    IReadOnlyList<string> Tags,
    IReadOnlyList<WikiArticleMediaResult> Media,
    IReadOnlyList<WikiRelatedArticleResult> Related,
    int Revision,
    DateTimeOffset PublishedAt);
