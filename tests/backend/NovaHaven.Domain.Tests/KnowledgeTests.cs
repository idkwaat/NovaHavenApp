using NovaHaven.Application.Knowledge;
using NovaHaven.Domain.Knowledge;
using NovaHaven.Domain.Knowledge.Entities;
using Xunit;

namespace NovaHaven.Domain.Tests;

public sealed class KnowledgeTests
{
    [Fact]
    public void Location_validator_rejects_coordinates_outside_world_bounds()
    {
        var errors = KnowledgeValidator.ValidateLocation(new LocationInput(
            "Central", "City", 91.0m, 181.0m, null));

        Assert.Contains("latitude", errors.Keys);
        Assert.Contains("longitude", errors.Keys);
    }

    [Fact]
    public void Season_validator_rejects_end_before_start()
    {
        var errors = KnowledgeValidator.ValidateSeason(new SeasonInput(
            DateTimeOffset.Parse("2026-12-31T00:00:00Z"),
            DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            "Winter", null));

        Assert.Contains("endsAt", errors.Keys);
    }

    [Fact]
    public void Link_validator_rejects_duplicate_or_multiple_targets()
    {
        var target = Guid.NewGuid();
        var errors = KnowledgeValidator.ValidateLinks([
            new KnowledgeLinkInput(KnowledgeLinkType.Related, target, null, 0),
            new KnowledgeLinkInput(KnowledgeLinkType.Related, target, null, 1),
            new KnowledgeLinkInput(KnowledgeLinkType.UsesItem, Guid.NewGuid(), Guid.NewGuid(), 2)
        ]);

        Assert.Contains("links", errors.Keys);
    }

    [Fact]
    public void Revision_snapshot_copies_draft_and_increments_number()
    {
        var entry = new GameKnowledgeEntry
        {
            Slug = "warden-lyra",
            DraftName = "Warden Lyra",
            DraftSummary = "A harbor warden.",
            DraftMarkdown = "# Warden Lyra",
            Kind = KnowledgeKind.Npc,
            LatestRevisionNumber = 2
        };

        var revision = GameKnowledgeRevision.FromDraft(entry, Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Equal(3, revision.Number);
        Assert.Equal(entry.Slug, revision.Slug);
        Assert.Equal(entry.DraftName, revision.Name);
        Assert.Equal(entry.Kind, revision.Kind);
    }
}
