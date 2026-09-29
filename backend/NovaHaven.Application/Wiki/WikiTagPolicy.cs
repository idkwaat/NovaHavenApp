using NovaHaven.Domain.Wiki;
using NovaHaven.Domain.Wiki.Entities;

namespace NovaHaven.Application.Wiki;

public sealed record WikiTagInput(string Name, string Slug);
public sealed record WikiTagUpdateInput(string Name, string Slug, bool IsActive);

public static class WikiTagPolicy
{
    public static string NormalizeName(string name) => WikiCategoryPolicy.NormalizeName(name);

    public static string? CheckUpdate(WikiTag tag, WikiTagUpdateInput input,
        bool hasDraftReferences, bool hasRevisionReferences)
    {
        if (hasRevisionReferences && (tag.Name != input.Name.Trim() || tag.Slug != input.Slug))
            return "A tag referenced by historical revisions cannot be renamed or have its slug changed.";
        if (tag.IsActive && !input.IsActive && (hasDraftReferences || hasRevisionReferences))
            return "A tag referenced by drafts or revisions cannot be deactivated.";
        return null;
    }

    public static string? CheckDelete(bool hasDraftReferences, bool hasRevisionReferences) =>
        hasDraftReferences || hasRevisionReferences
            ? "A tag referenced by drafts or revisions cannot be deleted."
            : null;
}
