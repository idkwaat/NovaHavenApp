using NovaHaven.Domain.Knowledge;

namespace NovaHaven.Domain.Knowledge.Entities;

public sealed class GameKnowledgeEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Slug { get; set; } = "";
    public string DraftName { get; set; } = "";
    public string DraftSummary { get; set; } = "";
    public string DraftMarkdown { get; set; } = "";
    public KnowledgeKind Kind { get; set; }
    public KnowledgeState State { get; set; } = KnowledgeState.Draft;
    public Guid? PublishedRevisionId { get; set; }
    public bool WasPublished { get; set; }
    public int LatestRevisionNumber { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class GameKnowledgeRevision
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EntryId { get; set; }
    public int Number { get; set; }
    public KnowledgeKind Kind { get; set; }
    public string Slug { get; set; } = "";
    public string Name { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Markdown { get; set; } = "";
    public DateTimeOffset PublishedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid PublishedBy { get; set; }

    public static GameKnowledgeRevision FromDraft(GameKnowledgeEntry entry, Guid publisherId, DateTimeOffset now) => new()
    {
        EntryId = entry.Id,
        Number = checked(entry.LatestRevisionNumber + 1),
        Kind = entry.Kind,
        Slug = entry.Slug,
        Name = entry.DraftName,
        Summary = entry.DraftSummary,
        Markdown = entry.DraftMarkdown,
        PublishedAt = now,
        PublishedBy = publisherId
    };
}
