namespace NovaHaven.Api.Contracts.Rewards;

public sealed record RewardRequest(
    string? Name, string? Slug, string? Summary, string? Markdown, string? Kind, string? DeliveryDescription);
