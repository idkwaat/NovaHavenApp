namespace NovaHaven.Application.Features.News.Queries;

public sealed record NewsListQuery(string? Search, int? Page, int? PageSize);
