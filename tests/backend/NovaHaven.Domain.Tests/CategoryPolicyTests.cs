using NovaHaven.Application.Wiki;
using NovaHaven.Domain.Wiki;
using NovaHaven.Domain.Wiki.Entities;
using Xunit;

namespace NovaHaven.Domain.Tests;

public sealed class CategoryPolicyTests
{
    private readonly WikiCategory _category = new() { Name = "Fishing", Slug = "fishing", IsActive = true };
    private static WikiCategoryUpdateInput Update(string name = "Fishing", string slug = "fishing", bool active = true) =>
        new(name, slug, 2, active);

    [Fact]
    public void ReorderReferencedCategoryIsAllowed() =>
        Assert.Null(WikiCategoryPolicy.CheckUpdate(_category, Update(), hasArticleReferences: true, hasRevisionReferences: true));

    [Fact]
    public void PublishedRevisionPreventsRename() =>
        Assert.NotNull(WikiCategoryPolicy.CheckUpdate(_category, Update(name: "Fishery"), false, true));

    [Fact]
    public void PublishedRevisionPreventsSlugChange() =>
        Assert.NotNull(WikiCategoryPolicy.CheckUpdate(_category, Update(slug: "fishery"), false, true));

    [Fact]
    public void ReferencedCategoryCannotBeDeactivated() =>
        Assert.NotNull(WikiCategoryPolicy.CheckUpdate(_category, Update(active: false), true, false));

    [Fact]
    public void UnreferencedCategoryCanBeDeactivated() =>
        Assert.Null(WikiCategoryPolicy.CheckUpdate(_category, Update(active: false), false, false));

    [Fact]
    public void HistoricalRevisionPreventsDeletion() =>
        Assert.NotNull(WikiCategoryPolicy.CheckDelete(false, true));

    [Fact]
    public void DraftPreventsDeletion() =>
        Assert.NotNull(WikiCategoryPolicy.CheckDelete(true, false));

    [Fact]
    public void UnreferencedCategoryCanBeDeleted() =>
        Assert.Null(WikiCategoryPolicy.CheckDelete(false, false));
}
