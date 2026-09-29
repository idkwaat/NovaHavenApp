using NovaHaven.Application.Common.Results;
using NovaHaven.Application.Features.Wiki.Queries;
using NovaHaven.Application.Features.Wiki.Results;

namespace NovaHaven.Application.Features.Wiki.Repositories;

public interface IWikiReadRepository
{
    Task<ApplicationResult<WikiArticlePageResult>> ListArticlesAsync(
        WikiArticleListQuery query,
        CancellationToken cancellationToken);

    Task<ApplicationResult<WikiArticleResult>> GetArticleAsync(
        string slug,
        CancellationToken cancellationToken);

    Task<ApplicationResult<IReadOnlyList<WikiTagResult>>> ListTagsAsync(CancellationToken cancellationToken);

    Task<ApplicationResult<IReadOnlyList<WikiCategoryResult>>> ListCategoriesAsync(CancellationToken cancellationToken);
}
