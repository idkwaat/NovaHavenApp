namespace NovaHaven.Domain.Wiki.Entities;

public sealed class WikiTag
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string NormalizedName { get; set; } = "";
    public string Slug { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public uint RowVersion { get; set; }
}
