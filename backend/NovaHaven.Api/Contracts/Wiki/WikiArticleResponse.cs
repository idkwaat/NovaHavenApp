namespace NovaHaven.Api.Contracts.Wiki;

public sealed record WikiArticleResponse(
    Guid Id,
    string Slug,
    string Title,
    string Summary,
    string Markdown,
    string Category,
    IReadOnlyList<string> Tags,
    IReadOnlyList<WikiArticleMediaResponse> Media,
    IReadOnlyList<WikiRelatedArticleResponse> Related,
    int Revision,
    DateTimeOffset PublishedAt);

public sealed record WikiArticleMediaResponse(
    Guid Id,
    string Url,
    string ContentType,
    string OriginalFileName,
    int Width,
    int Height,
    string Alt);

public sealed record WikiRelatedArticleResponse(
    Guid Id,
    string Slug,
    string Title,
    string Summary,
    string Category,
    int Revision,
    DateTimeOffset PublishedAt);
