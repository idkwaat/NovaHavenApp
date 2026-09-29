using NovaHaven.Application.Wiki;
using NovaHaven.Domain.Wiki;
using NovaHaven.Domain.Wiki.Entities;
using Xunit;

namespace NovaHaven.Domain.Tests;

public sealed class WikiTagTests
{
    private static WikiDraftInput Input(Guid[]? ids = null) =>
        new("Fishing", "fishing-guide", "Guide", "# Fishing", Guid.NewGuid(), ids);

    [Fact]
    public void OptionalTagsAreValid() => Assert.Empty(WikiDraftValidator.Validate(Input()));

    [Fact]
    public void DuplicateTagIdsAreRejected()
    {
        var id = Guid.NewGuid();
        Assert.Contains("tagIds", WikiDraftValidator.Validate(Input([id, id])).Keys);
    }

    [Fact]
    public void MoreThanTenTagsAreRejected() =>
        Assert.Contains("tagIds", WikiDraftValidator.Validate(Input(Enumerable.Range(0, 11).Select(_ => Guid.NewGuid()).ToArray())).Keys);

    [Fact]
    public void EmptyIdIsRejected() =>
        Assert.Contains("tagIds", WikiDraftValidator.Validate(Input([Guid.Empty])).Keys);

    [Fact]
    public void ReferencedTagCannotBeDeactivatedOrDeleted()
    {
        var current = new WikiTag { Name = "Fishing", Slug = "fishing", IsActive = true };
        Assert.NotNull(WikiTagPolicy.CheckUpdate(current, new("Fishing", "fishing", false), true, false));
        Assert.NotNull(WikiTagPolicy.CheckDelete(false, true));
    }

    [Fact]
    public void ReferencedHistoricalTagCannotBeRenamed() =>
        Assert.NotNull(WikiTagPolicy.CheckUpdate(new WikiTag { Name = "Fishing", Slug = "fishing" },
            new("Fishery", "fishing", true), false, true));
}
