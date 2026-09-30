using NovaHaven.Domain.News.Entities;

namespace NovaHaven.Application.Features.News.Repositories;

public interface INewsRepository
{
    Task<IReadOnlyList<NewsPost>> ListAdminAsync(CancellationToken cancellationToken);

    Task<int> CountPublishedAsync(string? search, CancellationToken cancellationToken);

    Task<IReadOnlyList<NewsPost>> ListPublishedAsync(
        string? search,
        int offset,
        int pageSize,
        CancellationToken cancellationToken);

    Task<NewsPost?> FindPublishedBySlugAsync(string slug, CancellationToken cancellationToken);

    Task<NewsPost?> FindAdminAsync(Guid id, CancellationToken cancellationToken);

    Task<NewsPost?> FindForUpdateAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> IsSlugInUseAsync(string slug, Guid? excludingId, CancellationToken cancellationToken);

    void Add(NewsPost post);

    void AddAudit(Guid? actorId, string action, Guid postId, object? details = null);
}
