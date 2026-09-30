using System.Text.RegularExpressions;
using NovaHaven.Application.Features.Catalog.Commands;

namespace NovaHaven.Application.Features.Catalog.Validators;

public static partial class CatalogRecipeValidator
{
    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex SlugPattern();

    public static Dictionary<string, string[]> Validate(CatalogRecipeInput input)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Trim().Length > 160)
            errors["name"] = ["Name must contain 1–160 characters."];
        if (string.IsNullOrEmpty(input.Slug) || input.Slug.Length is < 3 or > 120 || !SlugPattern().IsMatch(input.Slug))
            errors["slug"] = ["Slug must be 3–120 lowercase letters, digits or single hyphens."];
        if (input.Summary is null || input.Summary.Length > 500)
            errors["summary"] = ["Summary must not exceed 500 characters."];
        if (string.IsNullOrWhiteSpace(input.Markdown) || input.Markdown.Length > 50_000)
            errors["markdown"] = ["Markdown must contain 1–50,000 characters."];
        ValidateComponents(input.Ingredients, "ingredients", 1, 16, errors);
        ValidateComponents(input.Outputs, "outputs", 1, 8, errors);
        return errors;
    }

    private static void ValidateComponents(
        IReadOnlyList<CatalogRecipeComponentInput>? components,
        string field,
        int minimum,
        int maximum,
        Dictionary<string, string[]> errors)
    {
        if (components is null || components.Count < minimum || components.Count > maximum)
        {
            errors[field] = [$"{field} must contain {minimum}–{maximum} components."];
            return;
        }

        if (components.Any(component => component.ItemId == Guid.Empty || component.Quantity is < 1 or > 999))
            errors[field] = ["Every component needs a valid item and quantity from 1 to 999."];
        else if (components.Select(component => component.ItemId).Distinct().Count() != components.Count)
            errors[field] = ["Components in one role must reference distinct items."];
    }
}
