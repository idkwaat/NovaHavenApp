using NovaHaven.Domain.News.Entities;

namespace NovaHaven.Application.Features.News.Results;

public sealed record NewsPostResult(
    Guid Id,
    string Slug,
    string Title,
    string Summary,
    string Markdown,
    DateTimeOffset? PublishedAt,
    DateTimeOffset UpdatedAt);

public sealed record NewsPageResult(
    IReadOnlyList<NewsPostResult> Items,
    int Page,
    int PageSize,
    int Total);

public sealed record AdminNewsListItemResult(
    Guid Id,
    string Slug,
    string Title,
    NewsState State,
    DateTimeOffset? PublishedAt,
    DateTimeOffset UpdatedAt,
    uint RowVersion);

public sealed record AdminNewsPostResult(
    Guid Id,
    string Slug,
    string Title,
    string Summary,
    string Markdown,
    NewsState State,
    DateTimeOffset? PublishedAt,
    uint RowVersion);

public sealed record NewsWriteResult(Guid Id, string Slug, uint RowVersion);

public sealed record NewsPublishResult(Guid Id, DateTimeOffset PublishedAt, uint RowVersion);
