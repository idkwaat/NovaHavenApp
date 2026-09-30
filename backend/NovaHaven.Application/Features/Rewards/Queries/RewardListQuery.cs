namespace NovaHaven.Application.Features.Rewards.Queries;

public sealed record RewardListQuery(string? Search, int? Page, int? PageSize);
