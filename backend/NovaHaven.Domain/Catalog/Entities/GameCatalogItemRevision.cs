namespace NovaHaven.Domain.Catalog.Entities;

// Revision rows are append-only by application policy.
public sealed class GameCatalogItemRevision
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ItemId { get; set; }
    public int Number { get; set; }
    public string Slug { get; set; } = "";
    public string Name { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Markdown { get; set; } = "";
    public CatalogItemKind Kind { get; set; }
    public DateTimeOffset PublishedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid PublishedBy { get; set; }

    public static GameCatalogItemRevision FromDraft(
        GameCatalogItem item,
        Guid publisherId,
        DateTimeOffset now) => new()
    {
        ItemId = item.Id,
        Number = checked(item.LatestRevisionNumber + 1),
        Slug = item.Slug,
        Name = item.DraftName,
        Summary = item.DraftSummary,
        Markdown = item.DraftMarkdown,
        Kind = item.DraftKind,
        PublishedAt = now,
        PublishedBy = publisherId
    };
}
