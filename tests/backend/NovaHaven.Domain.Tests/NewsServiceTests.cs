using NovaHaven.Application.Common.Transactions;
using NovaHaven.Application.Features.News.Queries;
using NovaHaven.Application.Features.News.Repositories;
using NovaHaven.Application.Features.News.Services;
using NovaHaven.Application.Features.Notifications;
using NovaHaven.Domain.News.Entities;
using Xunit;

namespace NovaHaven.Domain.Tests;

public sealed class NewsServiceTests
{
    [Fact]
    public async Task Public_list_rejects_invalid_pagination_with_a_safe_validation_result()
    {
        var repository = new NewsRepositoryStub();
        var service = CreateService(repository);

        var result = await service.ListPublishedAsync(new NewsListQuery(null, 0, 20), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("validation.failed", result.Error!.Code);
        Assert.Equal("Invalid news pagination or filter.", result.Error.Message);
        Assert.Equal(0, repository.CountCalls);
        Assert.Equal(0, repository.ListCalls);
    }

    [Fact]
    public async Task Public_list_trims_search_and_uses_the_requested_page_offset()
    {
        var repository = new NewsRepositoryStub { PublishedCount = 47 };
        var service = CreateService(repository);

        var result = await service.ListPublishedAsync(new NewsListQuery("  guild  ", 3, 10), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("guild", repository.LastSearch);
        Assert.Equal(20, repository.LastOffset);
        Assert.Equal(10, repository.LastPageSize);
        Assert.Equal(3, result.Value!.Page);
        Assert.Equal(47, result.Value.Total);
    }

    private static NewsService CreateService(NewsRepositoryStub repository) =>
        new(repository, new UnitOfWorkStub(), new NotificationPublisherStub());

    private sealed class NewsRepositoryStub : INewsRepository
    {
        public int PublishedCount { get; init; }
        public int CountCalls { get; private set; }
        public int ListCalls { get; private set; }
        public string? LastSearch { get; private set; }
        public int LastOffset { get; private set; }
        public int LastPageSize { get; private set; }

        public Task<IReadOnlyList<NewsPost>> ListAdminAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<NewsPost>>([]);

        public Task<int> CountPublishedAsync(string? search, CancellationToken cancellationToken)
        {
            CountCalls++;
            LastSearch = search;
            return Task.FromResult(PublishedCount);
        }

        public Task<IReadOnlyList<NewsPost>> ListPublishedAsync(
            string? search,
            int offset,
            int pageSize,
            CancellationToken cancellationToken)
        {
            ListCalls++;
            LastSearch = search;
            LastOffset = offset;
            LastPageSize = pageSize;
            return Task.FromResult<IReadOnlyList<NewsPost>>([]);
        }

        public Task<NewsPost?> FindPublishedBySlugAsync(string slug, CancellationToken cancellationToken) =>
            Task.FromResult<NewsPost?>(null);

        public Task<NewsPost?> FindAdminAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<NewsPost?>(null);

        public Task<NewsPost?> FindForUpdateAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<NewsPost?>(null);

        public Task<bool> IsSlugInUseAsync(string slug, Guid? excludingId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public void Add(NewsPost post) { }

        public void AddAudit(Guid? actorId, string action, Guid postId, object? details = null) { }
    }

    private sealed class UnitOfWorkStub : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(0);

        public Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            Func<T, bool> shouldCommit,
            TransactionIsolation isolation,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException("The pagination tests do not perform writes.");
    }

    private sealed class NotificationPublisherStub : IUserNotificationPublisher
    {
        public Task<NotificationBatch> StageForAllUsersAsync(
            string title,
            string body,
            string? href,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new NotificationBatch([], title, body, href));

        public Task<NotificationDeliveryResult> DeliverPushAsync(
            NotificationBatch batch,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new NotificationDeliveryResult(0, 0, 0));
    }
}
