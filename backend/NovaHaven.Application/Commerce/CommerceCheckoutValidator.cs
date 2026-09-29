using System.Text.RegularExpressions;

namespace NovaHaven.Application.Commerce;

public sealed record CommerceCheckoutLineInput(string Slug, int Quantity);
public sealed record CommerceCheckoutInput(IReadOnlyList<CommerceCheckoutLineInput>? Items);

public static class CommerceCheckoutValidator
{
    private static readonly Regex Slug = new("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static Dictionary<string, string[]> Validate(CommerceCheckoutInput input)
    {
        var errors = new Dictionary<string, string[]>();
        var items = input.Items;
        if (items is null || items.Count is < 1 or > 20)
        {
            errors["items"] = ["Cart must contain between 1 and 20 offers."];
            return errors;
        }

        if (items.Any(item => item is null || string.IsNullOrWhiteSpace(item.Slug)) ||
            items.Select(item => item.Slug).Distinct(StringComparer.OrdinalIgnoreCase).Count() != items.Count)
            errors["items"] = ["Cart offers must be present and have distinct slugs."];

        if (items.Any(item => item is not null && (item.Slug.Length is < 3 or > 120 || !Slug.IsMatch(item.Slug))))
            errors["slug"] = ["Offer slugs must be 3–120 lowercase letters, digits or single hyphens."];

        if (items.Any(item => item is not null && item.Quantity is < 1 or > 99))
            errors["quantity"] = ["Each quantity must be between 1 and 99."];

        return errors;
    }
}
