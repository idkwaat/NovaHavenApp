namespace NovaHaven.Api.Contracts.Wiki;

public sealed record WikiDraftRequest(
    string Title,
    string Slug,
    string Summary,
    string Markdown,
    Guid CategoryId,
    Guid[]? TagIds = null,
    Guid[]? MediaIds = null);

public sealed record AdminWikiArticleListItemResponse(
    Guid Id,
    string Slug,
    string DraftTitle,
    int State,
    DateTimeOffset UpdatedAt,
    int LatestRevisionNumber);

public sealed record AdminWikiArticleDraftResponse(
    Guid Id,
    string Slug,
    string Title,
    string Summary,
    string Markdown,
    Guid CategoryId,
    IReadOnlyList<Guid> TagIds,
    int State,
    IReadOnlyList<Guid> MediaIds,
    int LatestRevisionNumber,
    Guid? PublishedRevisionId);

public sealed record AdminWikiArticleRevisionResponse(
    Guid Id,
    int Number,
    string Title,
    DateTimeOffset PublishedAt,
    Guid PublishedBy);

public sealed record AdminWikiArticleCreateResponse(Guid Id, string Slug, string Etag);

public sealed record AdminWikiArticleWriteResponse(Guid Id, string Etag);

public sealed record AdminWikiArticlePublishResponse(Guid Id, string Slug, Guid RevisionId, int Revision);
