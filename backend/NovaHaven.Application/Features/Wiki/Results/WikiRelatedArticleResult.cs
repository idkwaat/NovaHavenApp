namespace NovaHaven.Application.Features.Wiki.Results;

public sealed record WikiRelatedArticleResult(
    Guid Id,
    string Slug,
    string Title,
    string Summary,
    string Category,
    int Revision,
    DateTimeOffset PublishedAt);
