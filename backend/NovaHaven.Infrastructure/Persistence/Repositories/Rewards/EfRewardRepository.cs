using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NovaHaven.Application.Features.Rewards.Repositories;
using NovaHaven.Application.Features.Rewards.Results;
using NovaHaven.Domain.Rewards.Entities;
using NovaHaven.Domain.Wiki.Entities;
using NovaHaven.Infrastructure.Data;

namespace NovaHaven.Infrastructure.Persistence.Repositories.Rewards;

public sealed class EfRewardRepository(NovaDbContext dbContext) : IRewardRepository
{
    public Task<int> CountPublishedAsync(string? search, CancellationToken cancellationToken)
    {
        var query = from definition in dbContext.RewardDefinitions.AsNoTracking()
                    join revision in dbContext.RewardDefinitionRevisions.AsNoTracking()
                        on definition.PublishedRevisionId equals (Guid?)revision.Id
                    where definition.State == RewardDefinitionState.Published
                    select new { Definition = definition, Revision = revision };
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(row => row.Revision.Name.Contains(search) || row.Revision.Summary.Contains(search));
        return query.CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RewardListItemResult>> ListPublishedAsync(
        string? search, int offset, int pageSize, CancellationToken cancellationToken)
    {
        var query = from definition in dbContext.RewardDefinitions.AsNoTracking()
                    join revision in dbContext.RewardDefinitionRevisions.AsNoTracking()
                        on definition.PublishedRevisionId equals (Guid?)revision.Id
                    where definition.State == RewardDefinitionState.Published
                    select new { Definition = definition, Revision = revision };
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(row => row.Revision.Name.Contains(search) || row.Revision.Summary.Contains(search));
        return await query.OrderByDescending(row => row.Revision.PublishedAt)
            .ThenByDescending(row => row.Definition.Id)
            .Skip(offset)
            .Take(pageSize)
            .Select(row => new RewardListItemResult(
                row.Definition.Id, row.Revision.Slug, row.Revision.Name, row.Revision.Summary,
                row.Revision.Kind, row.Revision.Number, row.Revision.ExternalAcknowledgementRequired,
                row.Revision.PublishedAt))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<RewardDetailResult?> FindPublishedBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        var row = await (from definition in dbContext.RewardDefinitions.AsNoTracking()
            join revision in dbContext.RewardDefinitionRevisions.AsNoTracking()
                on definition.PublishedRevisionId equals (Guid?)revision.Id
            where definition.State == RewardDefinitionState.Published
            select new { Definition = definition, Revision = revision })
            .Where(candidate => candidate.Revision.Slug == slug)
            .Select(candidate => new
            {
                DefinitionId = candidate.Definition.Id,
                candidate.Revision.Slug,
                candidate.Revision.Name,
                candidate.Revision.Summary,
                candidate.Revision.Markdown,
                candidate.Revision.Kind,
                candidate.Revision.Number,
                candidate.Revision.DeliveryDescription,
                candidate.Revision.ExternalAcknowledgementRequired,
                candidate.Revision.PublishedAt
            })
            .SingleOrDefaultAsync(cancellationToken);
        return row is null ? null : new RewardDetailResult(
            row.DefinitionId, row.Slug, row.Name, row.Summary, row.Markdown, row.Kind,
            row.Number, row.DeliveryDescription, row.ExternalAcknowledgementRequired,
            row.PublishedAt, row.PublishedAt);
    }

    public async Task<IReadOnlyList<RewardAdminListItemResult>> ListAdminAsync(CancellationToken cancellationToken) =>
        await dbContext.RewardDefinitions.AsNoTracking()
            .OrderByDescending(definition => definition.UpdatedAt)
            .Take(100)
            .Select(definition => new RewardAdminListItemResult(
                definition.Id, definition.Slug, definition.DraftName, definition.DraftKind,
                definition.State, definition.LatestRevisionNumber, definition.UpdatedAt, definition.RowVersion))
            .ToArrayAsync(cancellationToken);

    public async Task<RewardAdminDraftResult?> FindAdminDraftAsync(Guid id, CancellationToken cancellationToken)
    {
        var definition = await dbContext.RewardDefinitions.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        return definition is null ? null : new RewardAdminDraftResult(
            definition.Id, definition.Slug, definition.DraftName, definition.DraftSummary,
            definition.DraftMarkdown, definition.DraftKind, definition.DraftDeliveryDescription,
            definition.State, definition.LatestRevisionNumber, definition.RowVersion);
    }

    public Task<RewardDefinition?> FindForUpdateAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.RewardDefinitions.SingleOrDefaultAsync(definition => definition.Id == id, cancellationToken);

    public Task<bool> IsSlugInUseAsync(string slug, Guid? excludingId, CancellationToken cancellationToken)
    {
        var query = dbContext.RewardDefinitions.AsNoTracking().Where(definition => definition.Slug == slug);
        if (excludingId.HasValue) query = query.Where(definition => definition.Id != excludingId.Value);
        return query.AnyAsync(cancellationToken);
    }

    public void Add(RewardDefinition definition) => dbContext.RewardDefinitions.Add(definition);
    public void AddRevision(RewardDefinitionRevision revision) => dbContext.RewardDefinitionRevisions.Add(revision);

    public void AddAudit(Guid? actorId, string action, Guid id, object? details = null)
    {
        if (actorId is null) return;
        var serialized = JsonSerializer.Serialize(details ?? new { });
        dbContext.AuditEvents.Add(new WikiAuditEvent
        {
            ActorUserId = actorId.Value,
            Action = action,
            EntityType = "RewardDefinition",
            EntityId = id,
            DetailsJson = serialized.Length <= 4000 ? serialized : serialized[..4000],
            OccurredAt = DateTimeOffset.UtcNow
        });
    }

}
