namespace NovaHaven.Domain.Wiki.Entities;

public sealed class WikiMedia
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string StorageKey { get; set; } = "";
    public string OriginalFileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long Length { get; set; }
    public string Sha256 { get; set; } = "";
    public int Width { get; set; }
    public int Height { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
