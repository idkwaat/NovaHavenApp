using NovaHaven.Domain.Community;

namespace NovaHaven.Domain.Community.Entities;

public sealed class CommunityRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Slug { get; set; } = "";
    public string DraftName { get; set; } = "";
    public string DraftSummary { get; set; } = "";
    public string DraftMarkdown { get; set; } = "";
    public CommunityKind Kind { get; set; }
    public CommunityState State { get; set; } = CommunityState.Draft;
    public Guid? PublishedRevisionId { get; set; }
    public bool WasPublished { get; set; }
    public int LatestRevisionNumber { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public DateTimeOffset? DraftStartsAt { get; set; }
    public DateTimeOffset? DraftEndsAt { get; set; }
    public Guid? DraftLocationEntryId { get; set; }
    public int? DraftCapacity { get; set; }
    public bool DraftRegistrationOpen { get; set; }
    public string DraftMotto { get; set; } = "";
    public string DraftDiscordUrl { get; set; } = "";
    public string DraftHandle { get; set; } = "";
    public string DraftBio { get; set; } = "";
    public string DraftAvatarUrl { get; set; } = "";
    public string DraftOwnerDisplayName { get; set; } = "";
    public string DraftGalleryMarkdown { get; set; } = "";
    public string DraftLeaderboardCategory { get; set; } = "";
    public Guid? DraftSeasonEntryId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class CommunityRecordRevision
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RecordId { get; set; }
    public int Number { get; set; }
    public CommunityKind Kind { get; set; }
    public string Slug { get; set; } = "";
    public string Name { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Markdown { get; set; } = "";
    public DateTimeOffset PublishedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid PublishedBy { get; set; }
    public DateTimeOffset? StartsAt { get; set; }
    public DateTimeOffset? EndsAt { get; set; }
    public Guid? LocationRevisionId { get; set; }
    public int? Capacity { get; set; }
    public bool RegistrationOpen { get; set; }
    public string Motto { get; set; } = "";
    public string DiscordUrl { get; set; } = "";
    public string Handle { get; set; } = "";
    public string Bio { get; set; } = "";
    public string AvatarUrl { get; set; } = "";
    public string OwnerDisplayName { get; set; } = "";
    public string GalleryMarkdown { get; set; } = "";
    public string LeaderboardCategory { get; set; } = "";
    public Guid? SeasonRevisionId { get; set; }

    public static CommunityRecordRevision FromDraft(CommunityRecord record, Guid publisherId, DateTimeOffset now) => new()
    {
        RecordId = record.Id,
        Number = checked(record.LatestRevisionNumber + 1),
        Kind = record.Kind,
        Slug = record.Slug,
        Name = record.DraftName,
        Summary = record.DraftSummary,
        Markdown = record.DraftMarkdown,
        PublishedAt = now,
        PublishedBy = publisherId,
        StartsAt = record.DraftStartsAt,
        EndsAt = record.DraftEndsAt,
        Capacity = record.DraftCapacity,
        RegistrationOpen = record.DraftRegistrationOpen,
        Motto = record.DraftMotto,
        DiscordUrl = record.DraftDiscordUrl,
        Handle = record.DraftHandle,
        Bio = record.DraftBio,
        AvatarUrl = record.DraftAvatarUrl,
        OwnerDisplayName = record.DraftOwnerDisplayName,
        GalleryMarkdown = record.DraftGalleryMarkdown,
        LeaderboardCategory = record.DraftLeaderboardCategory
    };
}

public sealed class CommunityLeaderboardDraftRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RecordId { get; set; }
    public int Rank { get; set; }
    public string ParticipantName { get; set; } = "";
    public decimal Score { get; set; }
    public string Note { get; set; } = "";
}

public sealed class CommunityLeaderboardRevisionRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RevisionId { get; set; }
    public int Rank { get; set; }
    public string ParticipantName { get; set; } = "";
    public decimal Score { get; set; }
    public string Note { get; set; } = "";
}

public sealed class CommunityEventRegistration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EventRecordId { get; set; }
    public string DisplayName { get; set; } = "";
    public string Contact { get; set; } = "";
    public EventRegistrationState State { get; set; } = EventRegistrationState.Pending;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public byte[] RowVersion { get; set; } = [];
}
