using NovaHaven.Domain.Rewards.Entities;

namespace NovaHaven.Application.Features.Rewards.Results;

public sealed record RewardListItemResult(
    Guid Id, string Slug, string Name, string Summary, RewardDefinitionKind Kind,
    int Revision, bool ExternalAcknowledgementRequired, DateTimeOffset UpdatedAt);
public sealed record RewardPageResult(IReadOnlyList<RewardListItemResult> Items, int Page, int PageSize, int Total);
public sealed record RewardDetailResult(
    Guid Id, string Slug, string Name, string Summary, string Markdown, RewardDefinitionKind Kind,
    int Number, string DeliveryDescription, bool ExternalAcknowledgementRequired,
    DateTimeOffset PublishedAt, DateTimeOffset UpdatedAt);
public sealed record RewardAdminListItemResult(
    Guid Id, string Slug, string Name, RewardDefinitionKind Kind, RewardDefinitionState State,
    int LatestRevisionNumber, DateTimeOffset UpdatedAt, uint RowVersion);
public sealed record RewardAdminDraftResult(
    Guid Id, string Slug, string Name, string Summary, string Markdown, RewardDefinitionKind Kind,
    string DeliveryDescription, RewardDefinitionState State, int LatestRevisionNumber, uint RowVersion);
public sealed record RewardWriteResult(Guid Id, string Slug, uint RowVersion);
public sealed record RewardPublishResult(Guid Id, int Revision, uint RowVersion);
