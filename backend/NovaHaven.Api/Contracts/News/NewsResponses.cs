using NovaHaven.Domain.News.Entities;

namespace NovaHaven.Api.Contracts.News;

public sealed record NewsSummaryResponse(
    Guid Id,
    string Slug,
    string Title,
    string Summary,
    string Markdown,
    DateTimeOffset? PublishedAt,
    DateTimeOffset UpdatedAt);

public sealed record NewsPageResponse(
    IReadOnlyList<NewsSummaryResponse> Items,
    int Page,
    int PageSize,
    int Total);

public sealed record AdminNewsListItemResponse(
    Guid Id,
    string Slug,
    string Title,
    NewsState State,
    DateTimeOffset? PublishedAt,
    DateTimeOffset UpdatedAt,
    string Etag);

public sealed record AdminNewsPostResponse(
    Guid Id,
    string Slug,
    string Title,
    string Summary,
    string Markdown,
    NewsState State,
    DateTimeOffset? PublishedAt);

public sealed record NewsCreateResponse(Guid Id, string Slug, string Etag);

public sealed record NewsWriteResponse(Guid Id, string Etag);

public sealed record NewsPublishResponse(Guid Id, DateTimeOffset PublishedAt, string Etag);
