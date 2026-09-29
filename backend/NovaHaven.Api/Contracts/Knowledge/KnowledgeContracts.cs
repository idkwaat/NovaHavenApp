using NovaHaven.Domain.Knowledge;

namespace NovaHaven.Api.Contracts.Knowledge;

public sealed record KnowledgeRequest(
    string? Name, string? Slug, string? Summary, string? Markdown, string? Kind,
    string? Role, Guid? LocationEntryId, string? PortraitUrl,
    int? Difficulty, Guid? GiverNpcEntryId, string? RewardDescription, List<KnowledgeStepRequest>? Steps,
    string? Region, string? LocationType, decimal? Latitude, decimal? Longitude, string? MapImageUrl,
    DateTimeOffset? StartsAt, DateTimeOffset? EndsAt, string? Theme, string? EventDescription,
    List<KnowledgeLinkRequest>? Links);

public sealed record KnowledgeStepRequest(int Position, string? Title, string? Description);

public sealed record KnowledgeLinkRequest(
    KnowledgeLinkType LinkType, Guid? TargetEntryId, Guid? TargetCatalogItemId, int SortOrder);

public sealed record KnowledgePageResponse(
    IReadOnlyList<KnowledgeSummaryResponse> Items, int Page, int PageSize, int Total);

public sealed record KnowledgeSummaryResponse(
    Guid Id, string Slug, string Name, string Summary, string Kind,
    int Revision, DateTimeOffset PublishedAt, string? PreviewImageUrl);

public sealed record KnowledgeDetailResponse(
    Guid Id, string Slug, string Name, string Summary, string Markdown, string Kind,
    int Revision, DateTimeOffset PublishedAt, object? Metadata,
    IReadOnlyList<KnowledgePublishedLinkResponse> Links);

public sealed record KnowledgePublishedLinkResponse(
    string LinkType, string Slug, string Name, string Type, int SortOrder);

public sealed record KnowledgeAdminListItemResponse(
    Guid Id, string Slug, string Name, string Kind, string State,
    int LatestRevisionNumber, DateTimeOffset UpdatedAt, string Etag);

public sealed record KnowledgeAdminDraftResponse(
    Guid Id, string Slug, string Name, string Summary, string Markdown, string Kind, string State,
    int LatestRevisionNumber, object? Metadata, IReadOnlyList<KnowledgeDraftLinkResponse> Links);

public sealed record KnowledgeDraftLinkResponse(
    string LinkType, Guid? TargetEntryId, Guid? TargetCatalogItemId, int SortOrder);

public sealed record KnowledgeCreateResponse(Guid Id, string Slug, string Etag);

public sealed record KnowledgeWriteResponse(Guid Id, string Etag);

public sealed record KnowledgePublishResponse(Guid Id, Guid RevisionId, int Revision, string Etag);
