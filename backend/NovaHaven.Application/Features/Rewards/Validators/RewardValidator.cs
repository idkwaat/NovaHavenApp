using System.Text.RegularExpressions;
using NovaHaven.Application.Features.Rewards.Commands;
using NovaHaven.Domain.Rewards.Entities;

namespace NovaHaven.Application.Features.Rewards.Validators;

public static partial class RewardValidator
{
    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex SlugPattern();

    public static Dictionary<string, string[]> Validate(RewardInput input)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Trim().Length > 160)
            errors["name"] = ["Name must contain 1–160 characters."];
        if (input.Slug.Length is < 3 or > 120 || !SlugPattern().IsMatch(input.Slug))
            errors["slug"] = ["Slug must be 3–120 lowercase letters, digits or single hyphens."];
        if (input.Summary.Length > 500) errors["summary"] = ["Summary cannot exceed 500 characters."];
        if (string.IsNullOrWhiteSpace(input.Markdown) || input.Markdown.Length > 50_000)
            errors["markdown"] = ["Markdown must contain 1–50,000 characters."];
        if (!Enum.TryParse<RewardDefinitionKind>(input.Kind, true, out var kind) || !Enum.IsDefined(kind))
            errors["kind"] = ["Kind is not supported."];
        if (string.IsNullOrWhiteSpace(input.DeliveryDescription) || input.DeliveryDescription.Length > 1000)
            errors["deliveryDescription"] = ["Describe the intended delivery without executing a grant."];
        return errors;
    }
}
