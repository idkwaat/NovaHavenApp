namespace NovaHaven.Application.Features.Wiki.Results;

public sealed record WikiArticleRevisionResult(
    Guid Id,
    int Number,
    string Title,
    DateTimeOffset PublishedAt,
    Guid PublishedBy);
