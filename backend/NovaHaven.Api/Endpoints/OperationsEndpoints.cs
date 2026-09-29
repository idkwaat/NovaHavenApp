using Microsoft.EntityFrameworkCore;
using NovaHaven.Domain.Catalog;
using NovaHaven.Domain.Catalog.Entities;
using NovaHaven.Domain.Commerce;
using NovaHaven.Domain.Commerce.Entities;
using NovaHaven.Domain.Community;
using NovaHaven.Domain.Community.Entities;
using NovaHaven.Domain.Integration;
using NovaHaven.Domain.Integration.Entities;
using NovaHaven.Domain.Knowledge;
using NovaHaven.Domain.Knowledge.Entities;
using NovaHaven.Domain.Rewards;
using NovaHaven.Domain.Rewards.Entities;
using NovaHaven.Domain.Wiki;
using NovaHaven.Domain.Wiki.Entities;
using NovaHaven.Infrastructure.Data;

namespace NovaHaven.Api.Endpoints;

public static class OperationsEndpoints
{
    public static void MapOperationsEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v1/admin/operations/diagnostics", async (NovaDbContext db, CancellationToken ct) =>
        {
            var canConnect = await db.Database.CanConnectAsync(ct);
            var appliedMigrations = (await db.Database.GetAppliedMigrationsAsync(ct)).ToArray();
            var pendingMigrations = (await db.Database.GetPendingMigrationsAsync(ct)).ToArray();
            var persistedIntegrations = await db.IntegrationCapabilities.AsNoTracking()
                .OrderBy(x => x.CapabilityKey)
                .ToListAsync(ct);

            var content = new
            {
                wikiArticles = await db.Articles.CountAsync(ct),
                publishedWikiArticles = await db.Articles.CountAsync(x => x.State == ArticleState.Published, ct),
                knowledgeEntries = await db.KnowledgeEntries.CountAsync(ct),
                publishedKnowledgeEntries = await db.KnowledgeEntries.CountAsync(x => x.State == KnowledgeState.Published, ct),
                communityRecords = await db.CommunityRecords.CountAsync(ct),
                publishedCommunityRecords = await db.CommunityRecords.CountAsync(x => x.State == CommunityState.Published, ct),
                catalogItems = await db.CatalogItems.CountAsync(ct),
                publishedCatalogItems = await db.CatalogItems.CountAsync(x => x.State == CatalogItemState.Published, ct),
                rewards = await db.RewardDefinitions.CountAsync(ct),
                publishedRewards = await db.RewardDefinitions.CountAsync(x => x.State == RewardDefinitionState.Published, ct),
                commerceOffers = await db.CommerceOffers.CountAsync(ct),
                publishedCommerceOffers = await db.CommerceOffers.CountAsync(x => x.State == CommerceOfferState.Published, ct),
                auditEvents = await db.AuditEvents.CountAsync(ct)
            };

            var integrations = persistedIntegrations.Select(x => new
            {
                capabilityKey = x.CapabilityKey,
                status = x.Status.ToString().ToLowerInvariant(),
                updatedAt = x.UpdatedAt
            }).ToArray();

            return Results.Ok(new
            {
                generatedAt = DateTimeOffset.UtcNow,
                database = new
                {
                    provider = db.Database.ProviderName ?? "unknown",
                    canConnect,
                    appliedMigrations = appliedMigrations.Length,
                    pendingMigrations = pendingMigrations.Length,
                    pendingMigrationNames = pendingMigrations.Take(20).ToArray()
                },
                content,
                integrations
            });
        }).RequireAuthorization("AdminOnly");
    }
}
