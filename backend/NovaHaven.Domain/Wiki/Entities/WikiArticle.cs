namespace NovaHaven.Domain.Wiki.Entities;

public enum ArticleState { Draft, Published, Unpublished }

public sealed class WikiArticle
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Slug { get; set; } = "";
    public string DraftTitle { get; set; } = "";
    public string DraftSummary { get; set; } = "";
    public string DraftMarkdown { get; set; } = "";
    public Guid DraftCategoryId { get; set; }
    public ArticleState State { get; set; } = ArticleState.Draft;
    public Guid? PublishedRevisionId { get; set; }
    public bool WasPublished { get; set; }
    public int LatestRevisionNumber { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
