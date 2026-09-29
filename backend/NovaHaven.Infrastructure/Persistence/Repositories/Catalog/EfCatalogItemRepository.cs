using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NovaHaven.Application.Features.Catalog.Repositories;
using NovaHaven.Application.Features.Catalog.Results;
using NovaHaven.Domain.Catalog.Entities;
using NovaHaven.Domain.Wiki.Entities;
using NovaHaven.Infrastructure.Data;

namespace NovaHaven.Infrastructure.Persistence.Repositories.Catalog;

public sealed class EfCatalogItemRepository(NovaDbContext dbContext) : ICatalogItemRepository
{
    public Task<int> CountPublishedAsync(
        string? search,
        CatalogItemKind? kind,
        CancellationToken cancellationToken)
    {
        var query = from item in dbContext.CatalogItems.AsNoTracking()
                    join revision in dbContext.CatalogItemRevisions.AsNoTracking()
                        on item.PublishedRevisionId equals (Guid?)revision.Id
                    where item.State == CatalogItemState.Published
                    select new { Item = item, Revision = revision };
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(row => row.Revision.Name.Contains(search) || row.Revision.Summary.Contains(search));
        if (kind.HasValue) query = query.Where(row => row.Revision.Kind == kind.Value);
        return query.CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CatalogItemListItemResult>> ListPublishedAsync(
        string? search,
        CatalogItemKind? kind,
        int offset,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = from item in dbContext.CatalogItems.AsNoTracking()
                    join revision in dbContext.CatalogItemRevisions.AsNoTracking()
                        on item.PublishedRevisionId equals (Guid?)revision.Id
                    where item.State == CatalogItemState.Published
                    select new { Item = item, Revision = revision };
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(row => row.Revision.Name.Contains(search) || row.Revision.Summary.Contains(search));
        if (kind.HasValue) query = query.Where(row => row.Revision.Kind == kind.Value);
        return await query
            .OrderBy(row => row.Revision.Name)
            .ThenBy(row => row.Item.Id)
            .Skip(offset)
            .Take(pageSize)
            .Select(row => new CatalogItemListItemResult(
                row.Item.Id,
                row.Item.Slug,
                row.Revision.Name,
                row.Revision.Summary,
                row.Revision.Kind,
                row.Revision.Number,
                row.Revision.PublishedAt,
                row.Item.UpdatedAt))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<CatalogItemDetailResult?> FindPublishedBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        var row = await (from item in dbContext.CatalogItems.AsNoTracking()
            join revision in dbContext.CatalogItemRevisions.AsNoTracking()
                on item.PublishedRevisionId equals (Guid?)revision.Id
            where item.State == CatalogItemState.Published && item.Slug == slug
            select new { Item = item, Revision = revision })
            .Select(row => new CatalogItemDetailResult(
                row.Item.Id,
                row.Item.Slug,
                row.Revision.Name,
                row.Revision.Summary,
                row.Revision.Markdown,
                row.Revision.Kind,
                row.Revision.Number,
                row.Revision.PublishedAt,
                row.Item.UpdatedAt))
            .SingleOrDefaultAsync(cancellationToken);
        return row;
    }

    public async Task<IReadOnlyList<CatalogItemAdminListItemResult>> ListAdminAsync(CancellationToken cancellationToken) =>
        await dbContext.CatalogItems.AsNoTracking()
            .OrderByDescending(item => item.UpdatedAt)
            .Take(100)
            .Select(item => new CatalogItemAdminListItemResult(
                item.Id, item.Slug, item.DraftName, item.DraftKind, item.State,
                item.LatestRevisionNumber, item.UpdatedAt, item.RowVersion))
            .ToArrayAsync(cancellationToken);

    public async Task<CatalogItemAdminDraftResult?> FindAdminDraftAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.CatalogItems.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (item is null) return null;

        var published = item.PublishedRevisionId is Guid revisionId
            ? await dbContext.CatalogItemRevisions.AsNoTracking()
                .Where(revision => revision.Id == revisionId)
                .Select(revision => new CatalogItemRevisionResult(
                    revision.Id, revision.Number, revision.Name, revision.Summary,
                    revision.Markdown, revision.Kind, revision.PublishedAt))
                .SingleOrDefaultAsync(cancellationToken)
            : null;

        return new CatalogItemAdminDraftResult(
            item.Id, item.Slug, item.DraftName, item.DraftSummary, item.DraftMarkdown,
            item.DraftKind, item.State, item.LatestRevisionNumber, published, item.RowVersion);
    }

    public Task<GameCatalogItem?> FindForUpdateAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.CatalogItems.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

    public Task<bool> IsSlugInUseAsync(string slug, Guid? excludingId, CancellationToken cancellationToken)
    {
        var query = dbContext.CatalogItems.AsNoTracking().Where(item => item.Slug == slug);
        if (excludingId.HasValue) query = query.Where(item => item.Id != excludingId.Value);
        return query.AnyAsync(cancellationToken);
    }

    public void Add(GameCatalogItem item) => dbContext.CatalogItems.Add(item);
    public void AddRevision(GameCatalogItemRevision revision) => dbContext.CatalogItemRevisions.Add(revision);

    public void AddAudit(Guid? actorId, string action, Guid itemId, object? details = null)
    {
        if (actorId is null) return;
        var serialized = JsonSerializer.Serialize(details ?? new { });
        dbContext.AuditEvents.Add(new WikiAuditEvent
        {
            ActorUserId = actorId.Value,
            Action = action,
            EntityType = "GameCatalogItem",
            EntityId = itemId,
            DetailsJson = serialized.Length <= 4000 ? serialized : serialized[..4000],
            OccurredAt = DateTimeOffset.UtcNow
        });
    }
}
