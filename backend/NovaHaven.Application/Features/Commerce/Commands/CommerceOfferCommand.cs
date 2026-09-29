namespace NovaHaven.Application.Features.Commerce.Commands;

public sealed record CommerceOfferCommand(
    string? Name, string? Slug, string? Summary, string? Markdown, string? Kind,
    string? DisplayPrice, string? ProviderProductCode, bool IsPurchasable, long? PriceMinorUnits);
