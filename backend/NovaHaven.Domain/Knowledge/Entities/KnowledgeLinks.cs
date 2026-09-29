using NovaHaven.Domain.Knowledge;

namespace NovaHaven.Domain.Knowledge.Entities;

public sealed class GameKnowledgeDraftLink
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EntryId { get; set; }
    public KnowledgeLinkType LinkType { get; set; }
    public Guid? TargetEntryId { get; set; }
    public Guid? TargetCatalogItemId { get; set; }
    public int SortOrder { get; set; }
}

public sealed class GameKnowledgeRevisionLink
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RevisionId { get; set; }
    public KnowledgeLinkType LinkType { get; set; }
    public Guid? TargetRevisionId { get; set; }
    public Guid? TargetCatalogItemRevisionId { get; set; }
    public string TargetSlug { get; set; } = "";
    public string TargetName { get; set; } = "";
    public string TargetType { get; set; } = "";
    public int SortOrder { get; set; }
}
