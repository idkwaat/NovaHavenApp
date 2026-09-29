using System.Text;
using NovaHaven.Domain.Wiki;
using NovaHaven.Domain.Wiki.Entities;

namespace NovaHaven.Application.Wiki;

/// <summary>Editorial classification rules; database constraints still guard concurrent deletes.</summary>
public static class WikiCategoryPolicy
{
    public static string NormalizeName(string name) => name.Trim().Normalize(NormalizationForm.FormKC).ToUpperInvariant();

    public static string? CheckUpdate(WikiCategory current, WikiCategoryUpdateInput input,
        bool hasArticleReferences, bool hasRevisionReferences)
    {
        if (hasRevisionReferences &&
            (!string.Equals(current.Name, input.Name.Trim(), StringComparison.Ordinal) ||
             !string.Equals(current.Slug, input.Slug, StringComparison.Ordinal)))
            return "A category referenced by revisions cannot be renamed or have its slug changed.";
        if (current.IsActive && !input.IsActive && (hasArticleReferences || hasRevisionReferences))
            return "Move all article drafts and historical references before deactivating this category.";
        return null;
    }

    public static string? CheckDelete(bool hasArticleReferences, bool hasRevisionReferences) =>
        hasArticleReferences || hasRevisionReferences
            ? "A category referenced by article drafts or revisions cannot be deleted."
            : null;
}
