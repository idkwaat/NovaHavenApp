namespace NovaHaven.Application.Features.Wiki.Results;

public sealed record WikiArticlePublishedResult(
    Guid Id,
    string Slug,
    Guid RevisionId,
    int RevisionNumber,
    byte[] RowVersion);
