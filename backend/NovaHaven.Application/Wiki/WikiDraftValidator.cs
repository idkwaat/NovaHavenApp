using System.Text.RegularExpressions;

namespace NovaHaven.Application.Wiki;

public sealed record WikiDraftInput(string Title, string Slug, string Summary, string Markdown, Guid CategoryId,
    Guid[]? TagIds = null, Guid[]? MediaIds = null);
public sealed record WikiCategoryInput(string Name, string Slug, int DisplayOrder);
public sealed record WikiCategoryUpdateInput(string Name, string Slug, int DisplayOrder, bool IsActive);

public static class WikiDraftValidator
{
    private static readonly Regex SlugPattern = new(@"^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static Dictionary<string, string[]> Validate(WikiDraftInput input)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(input.Title) || input.Title.Trim().Length > 120)
            errors["title"] = ["Title must contain 1–120 characters."];
        if (string.IsNullOrEmpty(input.Slug) || input.Slug.Length is < 3 or > 120 || !SlugPattern.IsMatch(input.Slug))
            errors["slug"] = ["Slug must be 3–120 lowercase letters, digits or single hyphens."];
        if (input.Summary is null || input.Summary.Length > 300)
            errors["summary"] = ["Summary must not exceed 300 characters."];
        if (string.IsNullOrWhiteSpace(input.Markdown) || input.Markdown.Length > 50_000)
            errors["markdown"] = ["Markdown must contain 1–50,000 characters."];
        if (input.CategoryId == Guid.Empty)
            errors["categoryId"] = ["A category is required."];
        if (input.TagIds is { } tags &&
            (tags.Length > 10 || tags.Any(id => id == Guid.Empty) || tags.Distinct().Count() != tags.Length))
            errors["tagIds"] = ["Choose at most 10 distinct valid tags."];
        if (input.MediaIds is { } media &&
            (media.Length > 20 || media.Any(id => id == Guid.Empty) || media.Distinct().Count() != media.Length))
            errors["mediaIds"] = ["Choose at most 20 distinct valid media files."];
        return errors;
    }

    public static Dictionary<string, string[]> ValidateTag(WikiTagInput input)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Trim().Length > 80)
            errors["name"] = ["Tag name must contain 1–80 characters."];
        if (string.IsNullOrEmpty(input.Slug) || input.Slug.Length is < 3 or > 80 || !SlugPattern.IsMatch(input.Slug))
            errors["slug"] = ["Tag slug must be 3–80 lowercase letters, digits or single hyphens."];
        return errors;
    }

    public static Dictionary<string, string[]> ValidateTag(WikiTagUpdateInput input) =>
        ValidateTag(new WikiTagInput(input.Name, input.Slug));

    public static Dictionary<string, string[]> ValidateCategory(WikiCategoryInput input)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Trim().Length > 80)
            errors["name"] = ["Category name must contain 1–80 characters."];
        if (string.IsNullOrEmpty(input.Slug) || input.Slug.Length is < 3 or > 80 || !SlugPattern.IsMatch(input.Slug))
            errors["slug"] = ["Category slug must be 3–80 lowercase letters, digits or hyphens."];
        if (input.DisplayOrder is < 0 or > 10_000)
            errors["displayOrder"] = ["Display order must be 0–10000."];
        return errors;
    }

    public static Dictionary<string, string[]> ValidateCategory(WikiCategoryUpdateInput input) =>
        ValidateCategory(new WikiCategoryInput(input.Name, input.Slug, input.DisplayOrder));
}
