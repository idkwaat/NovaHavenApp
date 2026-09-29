namespace NovaHaven.Domain.Wiki.Entities;

// Append-only revision membership; never cascade-delete historical references.
public sealed class WikiRevisionTag
{
    public Guid RevisionId { get; set; }
    public Guid TagId { get; set; }
}
