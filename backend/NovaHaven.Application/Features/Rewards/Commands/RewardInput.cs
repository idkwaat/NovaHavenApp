namespace NovaHaven.Application.Features.Rewards.Commands;

public sealed record RewardInput(
    string Name,
    string Slug,
    string Summary,
    string Markdown,
    string Kind,
    string DeliveryDescription);
