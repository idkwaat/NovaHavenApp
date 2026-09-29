namespace NovaHaven.Domain.Wiki.Entities;

public sealed class WikiRevisionMedia
{
    public Guid RevisionId { get; set; }
    public Guid MediaId { get; set; }
    public string AltText { get; set; } = "";
}
