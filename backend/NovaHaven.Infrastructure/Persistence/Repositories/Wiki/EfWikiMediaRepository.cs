using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NovaHaven.Application.Features.Media.Repositories;
using NovaHaven.Domain.Wiki.Entities;
using NovaHaven.Infrastructure.Data;

namespace NovaHaven.Infrastructure.Persistence.Repositories.Wiki;

public sealed class EfWikiMediaRepository(NovaDbContext dbContext) : IWikiMediaRepository
{
    public async Task<IReadOnlyList<WikiMediaListItemResult>> ListAsync(CancellationToken cancellationToken) =>
        await dbContext.Media.AsNoTracking()
            .OrderByDescending(media => media.CreatedAt)
            .Take(100)
            .Select(media => new WikiMediaListItemResult(
                media.Id, media.OriginalFileName, media.ContentType, media.Length, media.Width, media.Height,
                media.CreatedAt))
            .ToArrayAsync(cancellationToken);

    public Task<WikiMedia?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Media.AsNoTracking().SingleOrDefaultAsync(media => media.Id == id, cancellationToken);

    public Task<WikiMedia?> FindCurrentlyPublishedAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Media.AsNoTracking().Where(media => media.Id == id &&
                dbContext.RevisionMedia.AsNoTracking().Any(reference => reference.MediaId == id &&
                    dbContext.Revisions.AsNoTracking().Any(revision => revision.Id == reference.RevisionId &&
                        dbContext.Articles.AsNoTracking().Any(article =>
                            article.PublishedRevisionId == revision.Id && article.State == ArticleState.Published))))
            .SingleOrDefaultAsync(cancellationToken);

    public void Add(WikiMedia media) => dbContext.Media.Add(media);

    public void AddAudit(Guid? actorId, Guid mediaId, string contentType, long length, int width, int height)
    {
        if (actorId is null) return;
        var detailsJson = JsonSerializer.Serialize(new
        {
            ContentType = contentType,
            Length = length,
            Width = width,
            Height = height
        });
        dbContext.AuditEvents.Add(new WikiAuditEvent
        {
            ActorUserId = actorId.Value,
            Action = "media.uploaded",
            EntityType = "WikiMedia",
            EntityId = mediaId,
            DetailsJson = detailsJson.Length <= 4000 ? detailsJson : detailsJson[..4000],
            OccurredAt = DateTimeOffset.UtcNow
        });
    }
}
