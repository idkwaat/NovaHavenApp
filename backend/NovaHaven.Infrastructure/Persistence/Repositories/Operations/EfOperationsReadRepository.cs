using Microsoft.EntityFrameworkCore;
using NovaHaven.Application.Features.Operations.Repositories;
using NovaHaven.Application.Features.Operations.Results;
using NovaHaven.Domain.Catalog;
using NovaHaven.Domain.Catalog.Entities;
using NovaHaven.Domain.Commerce;
using NovaHaven.Domain.Commerce.Entities;
using NovaHaven.Domain.Community;
using NovaHaven.Domain.Community.Entities;
using NovaHaven.Domain.Integration.Entities;
using NovaHaven.Domain.Knowledge;
using NovaHaven.Domain.Knowledge.Entities;
using NovaHaven.Domain.Rewards.Entities;
using NovaHaven.Domain.Wiki;
using NovaHaven.Domain.Wiki.Entities;
using NovaHaven.Infrastructure.Data;

namespace NovaHaven.Infrastructure.Persistence.Repositories.Operations;

public sealed class EfOperationsReadRepository(NovaDbContext dbContext) : IOperationsReadRepository
{
    public async Task<OperationsDataResult> GetDiagnosticsAsync(CancellationToken cancellationToken)
    {
        var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
        var appliedMigrations = (await dbContext.Database.GetAppliedMigrationsAsync(cancellationToken)).Count();
        var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToArray();
        var persistedIntegrations = await dbContext.IntegrationCapabilities.AsNoTracking()
            .OrderBy(capability => capability.CapabilityKey)
            .Select(capability => new IntegrationDiagnosticsResult(
                capability.CapabilityKey,
                capability.Status.ToString().ToLowerInvariant(),
                capability.UpdatedAt))
            .ToArrayAsync(cancellationToken);

        var database = new DatabaseDiagnosticsResult(
            dbContext.Database.ProviderName ?? "unknown",
            canConnect,
            appliedMigrations,
            pendingMigrations.Length,
            pendingMigrations.Take(20).ToArray());
        var content = new ContentDiagnosticsResult(
            await dbContext.Articles.CountAsync(cancellationToken),
            await dbContext.Articles.CountAsync(article => article.State == ArticleState.Published, cancellationToken),
            await dbContext.KnowledgeEntries.CountAsync(cancellationToken),
            await dbContext.KnowledgeEntries.CountAsync(entry => entry.State == KnowledgeState.Published, cancellationToken),
            await dbContext.CommunityRecords.CountAsync(cancellationToken),
            await dbContext.CommunityRecords.CountAsync(record => record.State == CommunityState.Published, cancellationToken),
            await dbContext.CatalogItems.CountAsync(cancellationToken),
            await dbContext.CatalogItems.CountAsync(item => item.State == CatalogItemState.Published, cancellationToken),
            await dbContext.RewardDefinitions.CountAsync(cancellationToken),
            await dbContext.RewardDefinitions.CountAsync(reward => reward.State == RewardDefinitionState.Published, cancellationToken),
            await dbContext.CommerceOffers.CountAsync(cancellationToken),
            await dbContext.CommerceOffers.CountAsync(offer => offer.State == CommerceOfferState.Published, cancellationToken),
            await dbContext.AuditEvents.CountAsync(cancellationToken));

        return new OperationsDataResult(database, content, persistedIntegrations);
    }
}
