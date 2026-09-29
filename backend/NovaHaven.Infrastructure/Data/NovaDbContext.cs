using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NovaHaven.Domain.Wiki.Entities;
using NovaHaven.Domain.News.Entities;
using NovaHaven.Domain.Catalog.Entities;
using NovaHaven.Domain.Knowledge.Entities;
using NovaHaven.Domain.Community.Entities;
using NovaHaven.Domain.Integration.Entities;
using NovaHaven.Domain.Rewards.Entities;
using NovaHaven.Domain.Commerce.Entities;
using NovaHaven.Domain.Notifications.Entities;
using NovaHaven.Infrastructure.Identity;

namespace NovaHaven.Infrastructure.Data;

public sealed class NovaDbContext(DbContextOptions<NovaDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<WikiArticle> Articles => Set<WikiArticle>();
    public DbSet<WikiArticleRevision> Revisions => Set<WikiArticleRevision>();
    public DbSet<WikiCategory> Categories => Set<WikiCategory>();
    public DbSet<WikiTag> Tags => Set<WikiTag>();
    public DbSet<WikiDraftTag> DraftTags => Set<WikiDraftTag>();
    public DbSet<WikiRevisionTag> RevisionTags => Set<WikiRevisionTag>();
    public DbSet<WikiMedia> Media => Set<WikiMedia>();
    public DbSet<WikiDraftMedia> DraftMedia => Set<WikiDraftMedia>();
    public DbSet<WikiRevisionMedia> RevisionMedia => Set<WikiRevisionMedia>();
    public DbSet<WikiAuditEvent> AuditEvents => Set<WikiAuditEvent>();
    public DbSet<NewsPost> NewsPosts => Set<NewsPost>();
    public DbSet<GameCatalogItem> CatalogItems => Set<GameCatalogItem>();
    public DbSet<GameCatalogItemRevision> CatalogItemRevisions => Set<GameCatalogItemRevision>();
    public DbSet<GameRecipe> Recipes => Set<GameRecipe>();
    public DbSet<GameRecipeDraftComponent> RecipeDraftComponents => Set<GameRecipeDraftComponent>();
    public DbSet<GameRecipeRevision> RecipeRevisions => Set<GameRecipeRevision>();
    public DbSet<GameRecipeRevisionComponent> RecipeRevisionComponents => Set<GameRecipeRevisionComponent>();
    public DbSet<GameKnowledgeEntry> KnowledgeEntries => Set<GameKnowledgeEntry>();
    public DbSet<GameKnowledgeRevision> KnowledgeRevisions => Set<GameKnowledgeRevision>();
    public DbSet<NpcProfile> NpcProfiles => Set<NpcProfile>();
    public DbSet<NpcProfileRevision> NpcProfileRevisions => Set<NpcProfileRevision>();
    public DbSet<QuestDefinition> QuestDefinitions => Set<QuestDefinition>();
    public DbSet<QuestDefinitionRevision> QuestDefinitionRevisions => Set<QuestDefinitionRevision>();
    public DbSet<QuestStep> QuestSteps => Set<QuestStep>();
    public DbSet<QuestStepRevision> QuestStepRevisions => Set<QuestStepRevision>();
    public DbSet<WorldLocation> WorldLocations => Set<WorldLocation>();
    public DbSet<WorldLocationRevision> WorldLocationRevisions => Set<WorldLocationRevision>();
    public DbSet<SeasonDefinition> SeasonDefinitions => Set<SeasonDefinition>();
    public DbSet<SeasonDefinitionRevision> SeasonDefinitionRevisions => Set<SeasonDefinitionRevision>();
    public DbSet<GameKnowledgeDraftLink> KnowledgeDraftLinks => Set<GameKnowledgeDraftLink>();
    public DbSet<GameKnowledgeRevisionLink> KnowledgeRevisionLinks => Set<GameKnowledgeRevisionLink>();
    public DbSet<CommunityRecord> CommunityRecords => Set<CommunityRecord>();
    public DbSet<CommunityRecordRevision> CommunityRecordRevisions => Set<CommunityRecordRevision>();
    public DbSet<CommunityLeaderboardDraftRow> CommunityLeaderboardDraftRows => Set<CommunityLeaderboardDraftRow>();
    public DbSet<CommunityLeaderboardRevisionRow> CommunityLeaderboardRevisionRows => Set<CommunityLeaderboardRevisionRow>();
    public DbSet<CommunityEventRegistration> CommunityEventRegistrations => Set<CommunityEventRegistration>();
    public DbSet<IntegrationCapability> IntegrationCapabilities => Set<IntegrationCapability>();
    public DbSet<RewardDefinition> RewardDefinitions => Set<RewardDefinition>();
    public DbSet<RewardDefinitionRevision> RewardDefinitionRevisions => Set<RewardDefinitionRevision>();
    public DbSet<CommerceOffer> CommerceOffers => Set<CommerceOffer>();
    public DbSet<CommerceOfferRevision> CommerceOfferRevisions => Set<CommerceOfferRevision>();
    public DbSet<CommerceOrder> CommerceOrders => Set<CommerceOrder>();
    public DbSet<CommerceOrderLine> CommerceOrderLines => Set<CommerceOrderLine>();
    public DbSet<CommercePayment> CommercePayments => Set<CommercePayment>();
    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();
    public DbSet<PushSubscription> PushSubscriptions => Set<PushSubscription>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(NovaDbContext).Assembly);

        builder.Entity<WikiDraftTag>(entity =>
        {
            entity.HasKey(x => new { x.ArticleId, x.TagId });
            entity.HasOne<WikiArticle>().WithMany().HasForeignKey(x => x.ArticleId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<WikiTag>().WithMany().HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<WikiRevisionTag>(entity =>
        {
            entity.HasKey(x => new { x.RevisionId, x.TagId });
            entity.HasOne<WikiArticleRevision>().WithMany().HasForeignKey(x => x.RevisionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<WikiTag>().WithMany().HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
