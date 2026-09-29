using NovaHaven.Application.Catalog;
using Xunit;

namespace NovaHaven.Domain.Tests;

public sealed class CatalogRecipeTests
{
    [Fact]
    public void Recipe_validator_accepts_distinct_components()
    {
        var errors = CatalogRecipeValidator.Validate(new CatalogRecipeInput(
            "Moonsteel blade recipe", "moonsteel-blade", "Forge recipe.", "# Recipe",
            [new CatalogRecipeComponentInput(Guid.NewGuid(), 2)],
            [new CatalogRecipeComponentInput(Guid.NewGuid(), 1)]));

        Assert.Empty(errors);
    }

    [Fact]
    public void Recipe_validator_rejects_duplicate_or_empty_components()
    {
        var itemId = Guid.NewGuid();
        var errors = CatalogRecipeValidator.Validate(new CatalogRecipeInput(
            "Recipe", "recipe", "", "body",
            [new CatalogRecipeComponentInput(itemId, 1), new CatalogRecipeComponentInput(itemId, 2)],
            []));

        Assert.Contains("ingredients", errors.Keys);
        Assert.Contains("outputs", errors.Keys);
    }
}
