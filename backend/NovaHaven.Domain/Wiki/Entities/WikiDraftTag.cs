namespace NovaHaven.Domain.Wiki.Entities;

// Editing a draft never modifies a previously published revision's tags.
public sealed class WikiDraftTag
{
    public Guid ArticleId { get; set; }
    public Guid TagId { get; set; }
}
