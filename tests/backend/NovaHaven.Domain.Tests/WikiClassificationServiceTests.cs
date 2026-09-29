using NovaHaven.Application.Common.Results;
using NovaHaven.Application.Common.Transactions;
using NovaHaven.Application.Features.Wiki.Repositories;
using NovaHaven.Application.Features.Wiki.Services;
using NovaHaven.Application.Wiki;
using NovaHaven.Domain.Wiki.Entities;
using Xunit;

namespace NovaHaven.Domain.Tests;

public sealed class WikiClassificationServiceTests
{
    [Fact]
    public async Task Category_update_requires_if_match_and_rolls_back_typed_failure()
    {
        var category = new WikiCategory
        {
            Name = "Starting zone", NormalizedName = "STARTING ZONE", Slug = "starting-zone", RowVersion = [1, 2]
        };
        var unitOfWork = new StubUnitOfWork();
        var service = new WikiCategoryService(new StubCategoryRepository(category), unitOfWork);

        var missing = await service.UpdateAsync(
            category.Id,
            new WikiCategoryUpdateInput("Starting zone", "starting-zone", 0, true),
            expectedVersion: null,
            CancellationToken.None);
        var stale = await service.UpdateAsync(
            category.Id,
            new WikiCategoryUpdateInput("Starting zone", "starting-zone", 0, true),
            expectedVersion: [9],
            CancellationToken.None);

        Assert.Equal("http.precondition-required", missing.Error!.Code);
        Assert.Equal("http.precondition-failed", stale.Error!.Code);
        Assert.False(unitOfWork.LastTransactionCommitted);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Tag_update_requires_if_match_and_rolls_back_typed_failure()
    {
        var tag = new WikiTag
        {
            Name = "Guide", NormalizedName = "GUIDE", Slug = "guide", RowVersion = [1, 2]
        };
        var unitOfWork = new StubUnitOfWork();
        var service = new WikiTagService(new StubTagRepository(tag), unitOfWork);

        var missing = await service.UpdateAsync(
            tag.Id,
            new WikiTagUpdateInput("Guide", "guide", true),
            expectedVersion: null,
            CancellationToken.None);
        var stale = await service.UpdateAsync(
            tag.Id,
            new WikiTagUpdateInput("Guide", "guide", true),
            expectedVersion: [9],
            CancellationToken.None);

        Assert.Equal("http.precondition-required", missing.Error!.Code);
        Assert.Equal("http.precondition-failed", stale.Error!.Code);
        Assert.False(unitOfWork.LastTransactionCommitted);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    private sealed class StubUnitOfWork : IUnitOfWork
    {
        public bool LastTransactionCommitted { get; private set; }

        public int SaveCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.FromResult(1);
        }

        public async Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            Func<T, bool> shouldCommit,
            TransactionIsolation isolation,
            CancellationToken cancellationToken)
        {
            var result = await operation(cancellationToken);
            LastTransactionCommitted = shouldCommit(result);
            return result;
        }
    }

    private sealed class StubCategoryRepository(WikiCategory category) : IWikiCategoryRepository
    {
        public Task<IReadOnlyList<WikiCategory>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<WikiCategory>>([category]);

        public Task<WikiCategory?> FindAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<WikiCategory?>(id == category.Id ? category : null);

        public Task<WikiCategory?> FindForUpdateAsync(Guid id, CancellationToken cancellationToken) =>
            FindAsync(id, cancellationToken);

        public Task<bool> HasDuplicateAsync(string normalizedName, string slug, Guid? excludingId,
            CancellationToken cancellationToken) => Task.FromResult(false);

        public Task<bool> HasArticleReferencesAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<bool> HasRevisionReferencesAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public void Add(WikiCategory value) { }

        public void Remove(WikiCategory value) { }
    }

    private sealed class StubTagRepository(WikiTag tag) : IWikiTagRepository
    {
        public Task<IReadOnlyList<WikiTag>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<WikiTag>>([tag]);

        public Task<WikiTag?> FindForUpdateAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<WikiTag?>(id == tag.Id ? tag : null);

        public Task<bool> HasDuplicateAsync(string normalizedName, string slug, Guid? excludingId,
            CancellationToken cancellationToken) => Task.FromResult(false);

        public Task<bool> HasDraftReferencesAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<bool> HasRevisionReferencesAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public void Add(WikiTag value) { }

        public void Remove(WikiTag value) { }
    }
}
