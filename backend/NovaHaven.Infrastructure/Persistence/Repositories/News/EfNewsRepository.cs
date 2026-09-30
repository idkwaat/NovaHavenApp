using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NovaHaven.Application.Features.News.Repositories;
using NovaHaven.Domain.News;
using NovaHaven.Domain.News.Entities;
using NovaHaven.Domain.Wiki.Entities;
using NovaHaven.Infrastructure.Data;

namespace NovaHaven.Infrastructure.Persistence.Repositories.News;

public sealed class EfNewsRepository(NovaDbContext dbContext) : INewsRepository
{
    public async Task<IReadOnlyList<NewsPost>> ListAdminAsync(CancellationToken cancellationToken) =>
        await dbContext.NewsPosts.AsNoTracking()
            .OrderByDescending(post => post.UpdatedAt)
            .Take(100)
            .ToArrayAsync(cancellationToken);

    public Task<int> CountPublishedAsync(string? search, CancellationToken cancellationToken) =>
        FilterPublished(search).CountAsync(cancellationToken);

    public async Task<IReadOnlyList<NewsPost>> ListPublishedAsync(
        string? search,
        int offset,
        int pageSize,
        CancellationToken cancellationToken) =>
        await FilterPublished(search)
            .OrderByDescending(post => post.PublishedAt)
            .ThenByDescending(post => post.Id)
            .Skip(offset)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);

    public Task<NewsPost?> FindPublishedBySlugAsync(string slug, CancellationToken cancellationToken) =>
        dbContext.NewsPosts.AsNoTracking()
            .SingleOrDefaultAsync(post => post.State == NewsState.Published && post.Slug == slug, cancellationToken);

    public Task<NewsPost?> FindAdminAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.NewsPosts.AsNoTracking().SingleOrDefaultAsync(post => post.Id == id, cancellationToken);

    public Task<NewsPost?> FindForUpdateAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.NewsPosts.SingleOrDefaultAsync(post => post.Id == id, cancellationToken);

    public Task<bool> IsSlugInUseAsync(string slug, Guid? excludingId, CancellationToken cancellationToken)
    {
        var posts = dbContext.NewsPosts.AsNoTracking().Where(post => post.Slug == slug);
        if (excludingId.HasValue)
            posts = posts.Where(post => post.Id != excludingId.Value);
        return posts.AnyAsync(cancellationToken);
    }

    public void Add(NewsPost post) => dbContext.NewsPosts.Add(post);

    public void AddAudit(Guid? actorId, string action, Guid postId, object? details = null)
    {
        if (actorId is null) return;

        var detailsJson = JsonSerializer.Serialize(details ?? new { });
        dbContext.AuditEvents.Add(new WikiAuditEvent
        {
            ActorUserId = actorId.Value,
            Action = action,
            EntityType = "NewsPost",
            EntityId = postId,
            DetailsJson = detailsJson.Length <= 4000 ? detailsJson : detailsJson[..4000],
            OccurredAt = DateTimeOffset.UtcNow
        });
    }

    private IQueryable<NewsPost> FilterPublished(string? search)
    {
        var query = dbContext.NewsPosts.AsNoTracking().Where(post => post.State == NewsState.Published);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(post => post.DraftTitle.Contains(search) || post.DraftSummary.Contains(search));
        return query;
    }
}
