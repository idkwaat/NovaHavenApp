namespace NovaHaven.Domain.Wiki.Entities;

// Revision rows are append-only by application policy; restoring copies into an editable draft.
public sealed class WikiArticleRevision
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ArticleId { get; set; }
    public int Number { get; set; }
    public string Title { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Markdown { get; set; } = "";
    public Guid CategoryId { get; set; }
    public DateTimeOffset PublishedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid PublishedBy { get; set; }

    public static WikiArticleRevision FromDraft(WikiArticle article, Guid publisherId, DateTimeOffset now) => new()
    {
        ArticleId = article.Id,
        Number = checked(article.LatestRevisionNumber + 1),
        Title = article.DraftTitle,
        Summary = article.DraftSummary,
        Markdown = article.DraftMarkdown,
        CategoryId = article.DraftCategoryId,
        PublishedAt = now,
        PublishedBy = publisherId
    };
}
