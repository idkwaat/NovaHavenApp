namespace NovaHaven.Api.Contracts.Media;

public sealed record AdminWikiMediaResponse(
    Guid Id,
    string OriginalFileName,
    string ContentType,
    long Length,
    int Width,
    int Height,
    DateTimeOffset CreatedAt,
    string Url);

public sealed record WikiMediaUploadResponse(
    Guid Id,
    string OriginalFileName,
    string ContentType,
    long Length,
    int Width,
    int Height,
    string Url);
