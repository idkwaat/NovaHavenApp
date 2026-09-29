using NovaHaven.Application.Common.Results;
using NovaHaven.Application.Features.Wiki.Queries;
using NovaHaven.Application.Features.Wiki.Repositories;
using NovaHaven.Application.Features.Wiki.Results;
using NovaHaven.Application.Features.Wiki.Services;
using Xunit;

namespace NovaHaven.Domain.Tests;

public sealed class WikiReadServiceTests
{
    [Fact]
    public async Task List_rejects_invalid_pagination_before_calling_repository()
    {
        var repository = new StubWikiReadRepository();
        var service = new WikiReadService(repository);

        var result = await service.ListArticlesAsync(
            new WikiArticleListQuery(null, null, null, Page: 0, PageSize: 20),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("validation.failed", result.Error!.Code);
        Assert.Equal("Invalid pagination or filter.", result.Error.Message);
        Assert.False(repository.ListArticlesWasCalled);
    }

    [Fact]
    public async Task List_applies_defaults_and_trims_search_before_repository_call()
    {
        var repository = new StubWikiReadRepository();
        var service = new WikiReadService(repository);

        var result = await service.ListArticlesAsync(
            new WikiArticleListQuery("  quest  ", null, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("quest", repository.LastQuery!.Query);
        Assert.Equal(1, repository.LastQuery.Page);
        Assert.Equal(20, repository.LastQuery.PageSize);
    }

    private sealed class StubWikiReadRepository : IWikiReadRepository
    {
        public bool ListArticlesWasCalled { get; private set; }

        public WikiArticleListQuery? LastQuery { get; private set; }

        public Task<ApplicationResult<WikiArticlePageResult>> ListArticlesAsync(
            WikiArticleListQuery query,
            CancellationToken cancellationToken)
        {
            ListArticlesWasCalled = true;
            LastQuery = query;
            return Task.FromResult(ApplicationResult<WikiArticlePageResult>.Success(
                new WikiArticlePageResult([], query.Page, query.PageSize, 0)));
        }

        public Task<ApplicationResult<WikiArticleResult>> GetArticleAsync(
            string slug,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<ApplicationResult<IReadOnlyList<WikiTagResult>>> ListTagsAsync(
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<ApplicationResult<IReadOnlyList<WikiCategoryResult>>> ListCategoriesAsync(
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
