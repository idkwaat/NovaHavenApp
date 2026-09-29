namespace NovaHaven.Application.Features.Wiki.Queries;

public sealed record WikiArticleListQuery(
    string? Query,
    string? Category,
    string? Tag,
    int Page = 1,
    int PageSize = 20);
