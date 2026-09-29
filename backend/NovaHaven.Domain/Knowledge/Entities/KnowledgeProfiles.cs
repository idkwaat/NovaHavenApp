namespace NovaHaven.Domain.Knowledge.Entities;

public sealed class NpcProfile
{
    public Guid EntryId { get; set; }
    public string Role { get; set; } = "";
    public Guid? LocationEntryId { get; set; }
    public string? PortraitUrl { get; set; }
}

public sealed class NpcProfileRevision
{
    public Guid RevisionId { get; set; }
    public string Role { get; set; } = "";
    public Guid? LocationRevisionId { get; set; }
    public string? PortraitUrl { get; set; }
}

public sealed class QuestDefinition
{
    public Guid EntryId { get; set; }
    public int Difficulty { get; set; }
    public Guid? GiverNpcEntryId { get; set; }
    public Guid? LocationEntryId { get; set; }
    public string RewardDescription { get; set; } = "";
}

public sealed class QuestDefinitionRevision
{
    public Guid RevisionId { get; set; }
    public int Difficulty { get; set; }
    public Guid? GiverNpcRevisionId { get; set; }
    public Guid? LocationRevisionId { get; set; }
    public string RewardDescription { get; set; } = "";
}

public sealed class QuestStep
{
    public Guid QuestEntryId { get; set; }
    public int Position { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
}

public sealed class QuestStepRevision
{
    public Guid QuestRevisionId { get; set; }
    public int Position { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
}

public sealed class WorldLocation
{
    public Guid EntryId { get; set; }
    public string Region { get; set; } = "";
    public string LocationType { get; set; } = "";
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? MapImageUrl { get; set; }
}

public sealed class WorldLocationRevision
{
    public Guid RevisionId { get; set; }
    public string Region { get; set; } = "";
    public string LocationType { get; set; } = "";
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? MapImageUrl { get; set; }
}

public sealed class SeasonDefinition
{
    public Guid EntryId { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public string Theme { get; set; } = "";
    public string? EventDescription { get; set; }
}

public sealed class SeasonDefinitionRevision
{
    public Guid RevisionId { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public string Theme { get; set; } = "";
    public string? EventDescription { get; set; }
}
