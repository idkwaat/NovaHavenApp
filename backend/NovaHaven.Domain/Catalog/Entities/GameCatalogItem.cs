namespace NovaHaven.Domain.Catalog.Entities;

public enum CatalogItemKind
{
    Item,
    Weapon,
    Armor,
    Material,
    Fish,
    Crop
}

public enum CatalogItemState
{
    Draft,
    Published,
    Unpublished
}

public sealed class GameCatalogItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Slug { get; set; } = "";
    public string DraftName { get; set; } = "";
    public string DraftSummary { get; set; } = "";
    public string DraftMarkdown { get; set; } = "";
    public CatalogItemKind DraftKind { get; set; }
    public CatalogItemState State { get; set; } = CatalogItemState.Draft;
    public Guid? PublishedRevisionId { get; set; }
    public bool WasPublished { get; set; }
    public int LatestRevisionNumber { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
