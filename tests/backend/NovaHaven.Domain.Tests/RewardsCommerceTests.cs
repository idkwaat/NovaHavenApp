using NovaHaven.Application.Commerce;
using NovaHaven.Application.Features.Rewards.Commands;
using NovaHaven.Application.Features.Rewards.Validators;
using NovaHaven.Domain.Commerce;
using NovaHaven.Domain.Commerce.Entities;
using NovaHaven.Domain.Rewards.Entities;
using Xunit;

namespace NovaHaven.Domain.Tests;

public sealed class RewardsCommerceTests
{
    [Fact]
    public void Reward_validator_requires_external_acknowledgement_and_valid_kind()
    {
        var errors = RewardValidator.Validate(new RewardInput("Starter", "starter", "", "# Starter", "not-a-kind", "Give one item"));

        Assert.Contains("kind", errors.Keys);
        Assert.DoesNotContain("externalAcknowledgementRequired", errors.Keys);
    }

    [Fact]
    public void Reward_revision_preserves_definition_without_granting()
    {
        var definition = new RewardDefinition { Slug = "starter", DraftName = "Starter", DraftMarkdown = "# Starter", LatestRevisionNumber = 1, ExternalAcknowledgementRequired = true };

        var revision = RewardDefinitionRevision.FromDraft(definition, Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Equal(2, revision.Number);
        Assert.True(revision.ExternalAcknowledgementRequired);
    }

    [Fact]
    public void Commerce_validator_rejects_checkout_provider_reference()
    {
        var errors = CommerceValidator.Validate(new CommerceInput("Supporter", "supporter", "", "# Supporter", "donation", "5 USD", "provider-secret"));

        Assert.Contains("providerProductCode", errors.Keys);
    }

    [Fact]
    public void Commerce_revision_is_definition_only()
    {
        var offer = new CommerceOffer { Slug = "supporter", DraftName = "Supporter", DraftMarkdown = "# Supporter", LatestRevisionNumber = 4 };

        var revision = CommerceOfferRevision.FromDraft(offer, Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Equal(5, revision.Number);
        Assert.True(revision.DefinitionOnly);
    }
}
