using System.Text.RegularExpressions;
using NovaHaven.Domain.Commerce;
using NovaHaven.Domain.Commerce.Entities;

namespace NovaHaven.Application.Commerce;

public sealed record CommerceInput(
    string Name, string Slug, string Summary, string Markdown, string Kind, string DisplayPrice,
    string? ProviderProductCode, bool IsPurchasable = false, long? PriceMinorUnits = null);

public static class CommerceValidator
{
    private static readonly Regex Slug = new("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static Dictionary<string, string[]> Validate(CommerceInput input)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Trim().Length > 160) errors["name"] = ["Name must contain 1–160 characters."];
        if (input.Slug.Length is < 3 or > 120 || !Slug.IsMatch(input.Slug)) errors["slug"] = ["Slug must be 3–120 lowercase letters, digits or single hyphens."];
        if (input.Summary.Length > 500) errors["summary"] = ["Summary cannot exceed 500 characters."];
        if (string.IsNullOrWhiteSpace(input.Markdown) || input.Markdown.Length > 50_000) errors["markdown"] = ["Markdown must contain 1–50,000 characters."];
        if (!Enum.TryParse<CommerceOfferKind>(input.Kind, true, out var kind) || !Enum.IsDefined(kind)) errors["kind"] = ["Kind is not supported."];
        if (string.IsNullOrWhiteSpace(input.DisplayPrice) || input.DisplayPrice.Length > 80) errors["displayPrice"] = ["Display price is required and cannot exceed 80 characters."];
        if ((input.IsPurchasable && (input.PriceMinorUnits is null or < 1 or > 1_000_000_000_000)) ||
            (!input.IsPurchasable && input.PriceMinorUnits is < 0 or > 1_000_000_000_000))
            errors["priceMinorUnits"] = ["Purchasable offers require a VND price from 1 to 1,000,000,000,000."];
        if (!string.IsNullOrWhiteSpace(input.ProviderProductCode) && (input.ProviderProductCode.Length > 120 || input.ProviderProductCode.Contains("secret", StringComparison.OrdinalIgnoreCase) || input.ProviderProductCode.Contains("token", StringComparison.OrdinalIgnoreCase))) errors["providerProductCode"] = ["Provider product code is a public reference only; secrets are not accepted."];
        return errors;
    }
}
