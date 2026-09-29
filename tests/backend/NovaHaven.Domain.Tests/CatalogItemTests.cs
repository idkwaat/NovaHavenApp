using NovaHaven.Application.Catalog;
using NovaHaven.Domain.Catalog;
using NovaHaven.Domain.Catalog.Entities;
using Xunit;

namespace NovaHaven.Domain.Tests;

public sealed class CatalogItemTests
{
    [Fact]
    public void Validator_accepts_supported_typed_item()
    {
        var errors = CatalogItemValidator.Validate(new CatalogItemInput(
            "Moonsteel Sword", "moonsteel-sword", "A forged blade.", "# Moonsteel Sword", CatalogItemKind.Weapon));

        Assert.Empty(errors);
    }

    [Fact]
    public void Validator_rejects_invalid_slug_and_kind()
    {
        var errors = CatalogItemValidator.Validate(new CatalogItemInput(
            "Name", "Moonsteel Sword", "", "body", (CatalogItemKind)999));

        Assert.Contains("slug", errors.Keys);
        Assert.Contains("kind", errors.Keys);
    }

    [Fact]
    public void Revision_copies_draft_without_mutating_item()
    {
        var item = new GameCatalogItem
        {
            DraftName = "Moonsteel Sword",
            DraftSummary = "A forged blade.",
            DraftMarkdown = "# Details",
            DraftKind = CatalogItemKind.Weapon,
            LatestRevisionNumber = 2
        };

        var revision = GameCatalogItemRevision.FromDraft(item, Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Equal(3, revision.Number);
        Assert.Equal(item.DraftName, revision.Name);
        Assert.Equal(item.DraftKind, revision.Kind);
        Assert.Equal(2, item.LatestRevisionNumber);
    }
}
