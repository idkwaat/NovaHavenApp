using NovaHaven.Application.Wiki;
using NovaHaven.Domain.Wiki;
using NovaHaven.Domain.Wiki.Entities;
using Xunit;

namespace NovaHaven.Domain.Tests;

public sealed class WikiRulesTests
{
    private static WikiDraftInput Input(string slug = "getting-started") =>
        new("Getting Started", slug, "Guide", "# First steps", Guid.NewGuid());

    [Fact]
    public void ValidDraftHasNoValidationErrors() => Assert.Empty(WikiDraftValidator.Validate(Input()));

    [Theory]
    [InlineData("A-B")]
    [InlineData("double--hyphen")]
    [InlineData("bad-")]
    [InlineData("hi")]
    public void RejectsInvalidSlug(string slug) => Assert.Contains("slug", WikiDraftValidator.Validate(Input(slug)).Keys);

    [Fact]
    public void ARevisionIsSnapshotNotReferenceToEditableDraft()
    {
        var article = new WikiArticle { DraftTitle = "Before", DraftMarkdown = "Old", DraftCategoryId = Guid.NewGuid() };
        var snapshot = WikiArticleRevision.FromDraft(article, Guid.NewGuid(), DateTimeOffset.UtcNow);
        article.DraftTitle = "After";
        article.DraftMarkdown = "New";
        Assert.Equal("Before", snapshot.Title);
        Assert.Equal("Old", snapshot.Markdown);
        Assert.Equal(1, snapshot.Number);
    }
}
