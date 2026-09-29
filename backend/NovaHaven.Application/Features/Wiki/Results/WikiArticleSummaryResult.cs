namespace NovaHaven.Application.Features.Wiki.Results;

public sealed record WikiArticleSummaryResult(
    Guid Id,
    string Slug,
    string Title,
    string Summary,
    string Category,
    IReadOnlyList<string> Tags,
    DateTimeOffset PublishedAt,
    int Revision,
    Guid? PreviewMediaId,
    string? PreviewImageAlt);
