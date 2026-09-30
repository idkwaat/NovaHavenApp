namespace NovaHaven.Api.Contracts.Rewards;

public sealed record RewardListItemResponse(
    Guid Id, string Slug, string Name, string Summary, string Kind,
    int Revision, bool ExternalAcknowledgementRequired, DateTimeOffset UpdatedAt);
public sealed record RewardPageResponse(IReadOnlyList<RewardListItemResponse> Items, int Page, int PageSize, int Total);
public sealed record RewardDetailResponse(
    Guid Id, string Slug, string Name, string Summary, string Markdown, string Kind, int Number,
    string DeliveryDescription, bool ExternalAcknowledgementRequired, DateTimeOffset PublishedAt, DateTimeOffset UpdatedAt);
public sealed record RewardAdminListItemResponse(
    Guid Id, string Slug, string Name, string Kind, int State, int LatestRevisionNumber, DateTimeOffset UpdatedAt, string Etag);
public sealed record RewardAdminDraftResponse(
    Guid Id, string Slug, string Name, string Summary, string Markdown, string Kind,
    string DeliveryDescription, bool ExternalAcknowledgementRequired, int State, int LatestRevisionNumber);
public sealed record RewardCreateResponse(Guid Id, string Slug, string Etag, bool ExternalAcknowledgementRequired);
public sealed record RewardWriteResponse(Guid Id, string Etag);
public sealed record RewardPublishResponse(Guid Id, int Revision, bool ExternalAcknowledgementRequired, string Etag);
