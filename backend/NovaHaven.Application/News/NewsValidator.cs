using System.Text.RegularExpressions;

namespace NovaHaven.Application.News;

public sealed record NewsPostInput(string Title, string Slug, string Summary, string Markdown);

public static class NewsValidator
{
    private static readonly Regex SlugPattern = new("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static Dictionary<string, string[]> Validate(NewsPostInput input)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(input.Title) || input.Title.Trim().Length > 160)
            errors["title"] = ["Title must contain 1–160 characters."];
        if (string.IsNullOrEmpty(input.Slug) || input.Slug.Length is < 3 or > 120 || !SlugPattern.IsMatch(input.Slug))
            errors["slug"] = ["Slug must be 3–120 lowercase letters, digits or single hyphens."];
        if (input.Summary is null || input.Summary.Length > 500)
            errors["summary"] = ["Summary must not exceed 500 characters."];
        if (string.IsNullOrWhiteSpace(input.Markdown) || input.Markdown.Length > 50_000)
            errors["markdown"] = ["Markdown must contain 1–50,000 characters."];
        return errors;
    }
}
