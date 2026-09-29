using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NovaHaven.Application.Features.Commerce.Repositories;
using NovaHaven.Application.Features.Commerce.Results;
using NovaHaven.Domain.Commerce.Entities;
using NovaHaven.Domain.Wiki.Entities;
using NovaHaven.Infrastructure.Data;

namespace NovaHaven.Infrastructure.Persistence.Repositories.Commerce;

public sealed class EfCommerceOfferRepository(NovaDbContext dbContext) : ICommerceOfferRepository
{
    public Task<int> CountPublishedAsync(string? search, CancellationToken cancellationToken) =>
        PublishedRevisions(search).CountAsync(cancellationToken);

    public async Task<IReadOnlyList<CommerceOfferSummaryResult>> ListPublishedAsync(
        string? search, int offset, int pageSize, CancellationToken cancellationToken) =>
        await PublishedRevisions(search).OrderByDescending(revision => revision.PublishedAt)
            .ThenBy(revision => revision.OfferId).Skip(offset).Take(pageSize)
            .Select(revision => new CommerceOfferSummaryResult(revision.OfferId, revision.Slug,
                revision.Name, revision.Summary, revision.Kind, revision.Number,
                revision.IsPurchasable, revision.PriceMinorUnits, revision.PublishedAt))
            .ToArrayAsync(cancellationToken);

    public async Task<CommerceOfferDetailResult?> FindPublishedAsync(
        string slug, CancellationToken cancellationToken)
    {
        var revision = await PublishedRevisions(null).SingleOrDefaultAsync(
            item => item.Slug == slug, cancellationToken);
        return revision is null ? null : new CommerceOfferDetailResult(
            revision.OfferId, revision.Slug, revision.Name, revision.Summary, revision.Markdown,
            revision.Kind, revision.Number, revision.DisplayPrice, revision.ProviderProductCode,
            revision.IsPurchasable, revision.PriceMinorUnits, revision.PublishedAt);
    }

    public async Task<IReadOnlyList<CommerceOfferAdminListResult>> ListAdminAsync(CancellationToken cancellationToken) =>
        await dbContext.CommerceOffers.AsNoTracking().OrderByDescending(offer => offer.UpdatedAt).Take(100)
            .Select(offer => new CommerceOfferAdminListResult(offer.Id, offer.Slug, offer.DraftName,
                offer.DraftKind, offer.State, offer.LatestRevisionNumber, offer.UpdatedAt,
                offer.DraftIsPurchasable, offer.DraftPriceMinorUnits, offer.RowVersion))
            .ToArrayAsync(cancellationToken);

    public async Task<CommerceOfferAdminDraftResult?> FindAdminAsync(Guid id, CancellationToken cancellationToken)
    {
        var offer = await dbContext.CommerceOffers.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return offer is null ? null : new CommerceOfferAdminDraftResult(
            offer.Id, offer.Slug, offer.DraftName, offer.DraftSummary, offer.DraftMarkdown,
            offer.DraftKind, offer.DraftDisplayPrice, offer.DraftProviderProductCode,
            offer.DraftIsPurchasable, offer.DraftPriceMinorUnits, offer.State,
            offer.LatestRevisionNumber, offer.RowVersion);
    }

    public Task<CommerceOffer?> FindForUpdateAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.CommerceOffers.SingleOrDefaultAsync(offer => offer.Id == id, cancellationToken);

    public Task<bool> IsSlugInUseAsync(string slug, Guid? excludingId, CancellationToken cancellationToken)
    {
        var query = dbContext.CommerceOffers.AsNoTracking().Where(offer => offer.Slug == slug);
        if (excludingId.HasValue) query = query.Where(offer => offer.Id != excludingId.Value);
        return query.AnyAsync(cancellationToken);
    }

    public Task<bool> IsPublishedSlugInUseAsync(string slug, Guid excludingId, CancellationToken cancellationToken) =>
        dbContext.CommerceOfferRevisions.AsNoTracking().AnyAsync(revision => revision.Slug == slug
            && dbContext.CommerceOffers.Any(offer => offer.Id == revision.OfferId && offer.Id != excludingId
                && offer.State == CommerceOfferState.Published && offer.PublishedRevisionId == revision.Id),
            cancellationToken);

    public void Add(CommerceOffer offer) => dbContext.CommerceOffers.Add(offer);
    public void AddRevision(CommerceOfferRevision revision) => dbContext.CommerceOfferRevisions.Add(revision);

    public void AddAudit(Guid? actorId, string action, Guid id, object? details = null)
    {
        if (actorId is null) return;
        var json = JsonSerializer.Serialize(details ?? new { });
        dbContext.AuditEvents.Add(new WikiAuditEvent
        {
            ActorUserId = actorId.Value, Action = action, EntityType = "CommerceOffer", EntityId = id,
            DetailsJson = json.Length <= 4000 ? json : json[..4000], OccurredAt = DateTimeOffset.UtcNow
        });
    }

    private IQueryable<CommerceOfferRevision> PublishedRevisions(string? search)
    {
        var query = dbContext.CommerceOfferRevisions.AsNoTracking().Where(revision =>
            dbContext.CommerceOffers.Any(offer => offer.Id == revision.OfferId
                && offer.State == CommerceOfferState.Published && offer.PublishedRevisionId == revision.Id));
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(revision => revision.Name.Contains(search) || revision.Summary.Contains(search));
        return query;
    }
}
