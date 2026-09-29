using NovaHaven.Domain.Knowledge;

namespace NovaHaven.Application.Features.Knowledge.Results;

public sealed record KnowledgePageResult(
    IReadOnlyList<KnowledgeSummaryResult> Items,
    int Page,
    int PageSize,
    int Total);

public sealed record KnowledgeSummaryResult(
    Guid Id,
    string Slug,
    string Name,
    string Summary,
    KnowledgeKind Kind,
    int Revision,
    DateTimeOffset PublishedAt,
    string? PreviewImageUrl);

public sealed record KnowledgePublicDetailResult(
    Guid Id,
    string Slug,
    string Name,
    string Summary,
    string Markdown,
    KnowledgeKind Kind,
    int Revision,
    DateTimeOffset PublishedAt,
    object? Metadata,
    IReadOnlyList<KnowledgePublishedLinkResult> Links);

public sealed record KnowledgePublishedLinkResult(
    KnowledgeLinkType LinkType,
    string Slug,
    string Name,
    string Type,
    int SortOrder);

public sealed record KnowledgeAdminListItemResult(
    Guid Id,
    string Slug,
    string Name,
    KnowledgeKind Kind,
    KnowledgeState State,
    int LatestRevisionNumber,
    DateTimeOffset UpdatedAt,
    uint RowVersion);

public sealed record KnowledgeAdminDraftResult(
    Guid Id,
    string Slug,
    string Name,
    string Summary,
    string Markdown,
    KnowledgeKind Kind,
    KnowledgeState State,
    int LatestRevisionNumber,
    object? Metadata,
    IReadOnlyList<KnowledgeDraftLinkResult> Links,
    uint RowVersion);

public sealed record KnowledgeDraftLinkResult(
    KnowledgeLinkType LinkType,
    Guid? TargetEntryId,
    Guid? TargetCatalogItemId,
    int SortOrder);

public sealed record KnowledgeWriteResult(Guid Id, string Slug, uint RowVersion);

public sealed record KnowledgePublishResult(Guid Id, Guid RevisionId, int Revision, uint RowVersion);
