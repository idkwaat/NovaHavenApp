using NovaHaven.Domain.Knowledge;
using NovaHaven.Domain.Knowledge.Entities;
using Xunit;

namespace NovaHaven.Domain.Tests;

public sealed class DomainEntityNamespaceTests
{
    [Theory]
    [InlineData("NovaHaven.Domain.Wiki", "WikiArticle")]
    [InlineData("NovaHaven.Domain.News", "NewsPost")]
    [InlineData("NovaHaven.Domain.Catalog", "GameCatalogItem")]
    [InlineData("NovaHaven.Domain.Knowledge", "GameKnowledgeEntry")]
    [InlineData("NovaHaven.Domain.Community", "CommunityRecord")]
    [InlineData("NovaHaven.Domain.Integration", "IntegrationCapability")]
    [InlineData("NovaHaven.Domain.Rewards", "RewardDefinition")]
    [InlineData("NovaHaven.Domain.Commerce", "CommerceOffer")]
    public void Entity_types_live_in_their_feature_entities_namespace(string featureNamespace, string entityName)
    {
        var entityType = typeof(KnowledgeKind).Assembly.GetType($"{featureNamespace}.Entities.{entityName}");

        Assert.NotNull(entityType);
    }
}
