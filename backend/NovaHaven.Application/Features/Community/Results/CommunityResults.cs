using NovaHaven.Domain.Community;

namespace NovaHaven.Application.Features.Community.Results;

public sealed record CommunityPageResult(
    IReadOnlyList<CommunitySummaryResult> Items, int Page, int PageSize, int Total);

public sealed record CommunitySummaryResult(
    Guid Id, string Slug, string Name, string Summary, CommunityKind Kind,
    int Revision, DateTimeOffset PublishedAt);

public sealed record CommunityPublicDetailResult(
    Guid Id, string Slug, string Name, string Summary, string Markdown,
    CommunityKind Kind, int Revision, DateTimeOffset PublishedAt, object Metadata);

public sealed record CommunityAdminListItemResult(
    Guid Id, string Slug, string Name, CommunityKind Kind, CommunityState State,
    int LatestRevisionNumber, DateTimeOffset UpdatedAt, uint RowVersion);

public sealed record CommunityAdminDraftResult(
    Guid Id, string Slug, string Name, string Summary, string Markdown, CommunityKind Kind,
    CommunityState State, int LatestRevisionNumber, object Metadata, uint RowVersion);

public sealed record CommunityWriteResult(Guid Id, string Slug, uint RowVersion);
public sealed record CommunityPublishResult(Guid Id, Guid RevisionId, int Revision, uint RowVersion);

public sealed record CommunityRegistrationResult(
    Guid Id, string DisplayName, string Contact, EventRegistrationState State,
    DateTimeOffset CreatedAt, uint RowVersion);

public sealed record CommunityRegistrationWriteResult(
    Guid Id, string DisplayName, string Contact, EventRegistrationState State, uint RowVersion);
