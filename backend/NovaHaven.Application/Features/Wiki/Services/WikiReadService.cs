using NovaHaven.Application.Common.Results;
using NovaHaven.Application.Features.Wiki.Queries;
using NovaHaven.Application.Features.Wiki.Repositories;
using NovaHaven.Application.Features.Wiki.Results;

namespace NovaHaven.Application.Features.Wiki.Services;

public sealed class WikiReadService(IWikiReadRepository repository)
{
    public Task<ApplicationResult<WikiArticlePageResult>> ListArticlesAsync(
        WikiArticleListQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.Page < 1
            || query.PageSize is < 1 or > 50
            || (query.Query?.Length ?? 0) > 100
            || (query.Category?.Length ?? 0) > 80
            || (query.Tag?.Length ?? 0) > 80)
        {
            return Task.FromResult(ApplicationResult<WikiArticlePageResult>.Failure(
                new ApplicationError("validation.failed", "Invalid pagination or filter.")));
        }

        var offset = ((long)query.Page - 1) * query.PageSize;
        if (offset > int.MaxValue)
        {
            return Task.FromResult(ApplicationResult<WikiArticlePageResult>.Failure(
                new ApplicationError("validation.failed", "Page is out of range.")));
        }

        return repository.ListArticlesAsync(query with { Query = query.Query?.Trim() }, cancellationToken);
    }

    public Task<ApplicationResult<WikiArticleResult>> GetArticleAsync(
        string slug,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        return repository.GetArticleAsync(slug, cancellationToken);
    }

    public Task<ApplicationResult<IReadOnlyList<WikiTagResult>>> ListTagsAsync(CancellationToken cancellationToken) =>
        repository.ListTagsAsync(cancellationToken);

    public Task<ApplicationResult<IReadOnlyList<WikiCategoryResult>>> ListCategoriesAsync(
        CancellationToken cancellationToken) => repository.ListCategoriesAsync(cancellationToken);
}
