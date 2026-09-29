namespace NovaHaven.Domain.Wiki.Entities;

public sealed class WikiCategory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string NormalizedName { get; set; } = "";
    public string Slug { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
