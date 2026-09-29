using NovaHaven.Application.Community;
using NovaHaven.Domain.Community;
using NovaHaven.Domain.Community.Entities;
using Xunit;

namespace NovaHaven.Domain.Tests;

public sealed class CommunityTests
{
    [Fact]
    public void Event_validator_rejects_invalid_window_and_capacity()
    {
        var errors = CommunityValidator.ValidateEvent(new CommunityEventInput(
            DateTimeOffset.Parse("2026-12-31T00:00:00Z"), DateTimeOffset.Parse("2026-01-01T00:00:00Z"), null, 0, false));

        Assert.Contains("endsAt", errors.Keys);
        Assert.Contains("capacity", errors.Keys);
    }

    [Fact]
    public void Housing_validator_requires_owner_and_gallery()
    {
        var errors = CommunityValidator.ValidateHousing(new HousingInput("", ""));

        Assert.Contains("ownerDisplayName", errors.Keys);
        Assert.Contains("galleryMarkdown", errors.Keys);
    }

    [Fact]
    public void Leaderboard_validator_rejects_duplicate_ranks()
    {
        var errors = CommunityValidator.ValidateLeaderboard(new LeaderboardInput("Fishing", [
            new LeaderboardRowInput(1, "Lyra", 100, ""),
            new LeaderboardRowInput(1, "Mira", 90, "")
        ]));

        Assert.Contains("rows", errors.Keys);
    }

    [Fact]
    public void Community_revision_copies_common_draft_fields()
    {
        var record = new CommunityRecord
        {
            Kind = CommunityKind.Event,
            Slug = "harbor-festival",
            DraftName = "Harbor Festival",
            DraftSummary = "A local event.",
            DraftMarkdown = "# Harbor Festival",
            LatestRevisionNumber = 4
        };

        var revision = CommunityRecordRevision.FromDraft(record, Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Equal(5, revision.Number);
        Assert.Equal(record.Slug, revision.Slug);
        Assert.Equal(record.Kind, revision.Kind);
    }
}
