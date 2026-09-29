namespace NovaHaven.Application.Features.Media.Results;

public sealed record WikiMediaUploadResult(
    Guid Id,
    string OriginalFileName,
    string ContentType,
    long Length,
    int Width,
    int Height);

public sealed record WikiMediaContentResult(string ContentType, Stream Content);
