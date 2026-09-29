using System.Text.RegularExpressions;
using NovaHaven.Domain.Catalog;
using NovaHaven.Domain.Catalog.Entities;

namespace NovaHaven.Application.Catalog;

public sealed record CatalogItemInput(
    string Name,
    string Slug,
    string Summary,
    string Markdown,
    CatalogItemKind Kind);

public static class CatalogItemValidator
{
    private static readonly Regex SlugPattern = new(
        "^[a-z0-9]+(?:-[a-z0-9]+)*$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static Dictionary<string, string[]> Validate(CatalogItemInput input)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Trim().Length > 160)
            errors["name"] = ["Name must contain 1–160 characters."];
        if (string.IsNullOrEmpty(input.Slug) || input.Slug.Length is < 3 or > 120 || !SlugPattern.IsMatch(input.Slug))
            errors["slug"] = ["Slug must be 3–120 lowercase letters, digits or single hyphens."];
        if (input.Summary is null || input.Summary.Length > 500)
            errors["summary"] = ["Summary must not exceed 500 characters."];
        if (string.IsNullOrWhiteSpace(input.Markdown) || input.Markdown.Length > 50_000)
            errors["markdown"] = ["Markdown must contain 1–50,000 characters."];
        if (!Enum.IsDefined(input.Kind))
            errors["kind"] = ["Kind is not supported."];
        return errors;
    }
}
