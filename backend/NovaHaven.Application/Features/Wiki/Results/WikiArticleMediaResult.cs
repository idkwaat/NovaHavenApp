namespace NovaHaven.Application.Features.Wiki.Results;

public sealed record WikiArticleMediaResult(
    Guid Id,
    string ContentType,
    string OriginalFileName,
    int Width,
    int Height,
    string Alt);
