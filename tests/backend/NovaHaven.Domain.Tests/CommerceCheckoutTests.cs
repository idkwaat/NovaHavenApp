using NovaHaven.Application.Commerce;
using NovaHaven.Domain.Commerce;
using NovaHaven.Domain.Commerce.Entities;
using Xunit;

namespace NovaHaven.Domain.Tests;

public sealed class CommerceCheckoutTests
{
    [Fact]
    public void Commerce_offer_is_not_purchasable_until_explicitly_enabled()
    {
        var offer = new CommerceOffer { Slug = "local-demo", DraftName = "Local demo", DraftMarkdown = "# Demo" };

        var revision = CommerceOfferRevision.FromDraft(offer, Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.False(revision.IsPurchasable);
        Assert.Null(revision.PriceMinorUnits);
    }

    [Fact]
    public void Commerce_revision_snapshots_enabled_purchase_price()
    {
        var offer = new CommerceOffer
        {
            Slug = "supporter-demo", DraftName = "Supporter demo", DraftMarkdown = "# Demo",
            DraftIsPurchasable = true, DraftPriceMinorUnits = 49_000, LatestRevisionNumber = 2
        };

        var revision = CommerceOfferRevision.FromDraft(offer, Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Equal(3, revision.Number);
        Assert.True(revision.IsPurchasable);
        Assert.Equal(49_000, revision.PriceMinorUnits);
    }

    [Fact]
    public void Checkout_validator_accepts_a_bounded_distinct_cart()
    {
        var errors = CommerceCheckoutValidator.Validate(new CommerceCheckoutInput(
        [new("supporter-demo", 2), new("event-pass-demo", 1)]));

        Assert.Empty(errors);
    }

    [Theory]
    [MemberData(nameof(InvalidCarts))]
    public void Checkout_validator_rejects_empty_duplicate_oversized_or_invalid_quantity_carts(
        IReadOnlyList<CommerceCheckoutLineInput> items, string expectedError)
    {
        var errors = CommerceCheckoutValidator.Validate(new CommerceCheckoutInput(items));

        Assert.Contains(expectedError, errors.Keys);
    }

    public static TheoryData<IReadOnlyList<CommerceCheckoutLineInput>, string> InvalidCarts => new()
    {
        { [], "items" },
        { [new("supporter-demo", 1), new("supporter-demo", 2)], "items" },
        { Enumerable.Range(1, 21).Select(i => new CommerceCheckoutLineInput($"offer-{i}", 1)).ToArray(), "items" },
        { [new("supporter-demo", 0)], "quantity" },
        { [new("supporter-demo", 100)], "quantity" },
        { [new("Not-A-Slug", 1)], "slug" }
    };

    [Fact]
    public void Purchasable_commerce_offer_requires_a_positive_bounded_vnd_price()
    {
        var missing = CommerceValidator.Validate(new CommerceInput(
            "Supporter", "supporter", "", "# Supporter", "donation", "", null,
            IsPurchasable: true, PriceMinorUnits: null));
        var zero = CommerceValidator.Validate(new CommerceInput(
            "Supporter", "supporter", "", "# Supporter", "donation", "", null,
            IsPurchasable: true, PriceMinorUnits: 0));
        var tooHigh = CommerceValidator.Validate(new CommerceInput(
            "Supporter", "supporter", "", "# Supporter", "donation", "", null,
            IsPurchasable: true, PriceMinorUnits: 1_000_000_000_001));

        Assert.Contains("priceMinorUnits", missing.Keys);
        Assert.Contains("priceMinorUnits", zero.Keys);
        Assert.Contains("priceMinorUnits", tooHigh.Keys);
    }
}
