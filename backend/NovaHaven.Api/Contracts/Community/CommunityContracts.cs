namespace NovaHaven.Api.Contracts.Community;

public sealed record CommunityRequest(
    string? Name, string? Slug, string? Summary, string? Markdown, string? Kind,
    DateTimeOffset? StartsAt, DateTimeOffset? EndsAt, Guid? LocationEntryId, int? Capacity, bool RegistrationOpen,
    string? Motto, string? DiscordUrl, string? Handle, string? Bio, string? AvatarUrl,
    string? OwnerDisplayName, string? GalleryMarkdown, string? LeaderboardCategory, Guid? SeasonEntryId,
    IReadOnlyList<CommunityLeaderboardRowRequest>? Rows);

public sealed record CommunityLeaderboardRowRequest(int Rank, string? ParticipantName, decimal Score, string? Note);
public sealed record CommunityRegistrationRequest(string? DisplayName, string? Contact);
public sealed record CommunityRegistrationUpdateRequest(string? State);

public sealed record CommunitySummaryResponse(
    Guid Id, string Slug, string Name, string Summary, string Kind, int Revision, DateTimeOffset PublishedAt);

public sealed record CommunityPageResponse(
    IReadOnlyList<CommunitySummaryResponse> Items, int Page, int PageSize, int Total);

public sealed record CommunityDetailResponse(
    Guid Id, string Slug, string Name, string Summary, string Markdown,
    string Kind, int Revision, DateTimeOffset PublishedAt, object Metadata);

public sealed record CommunityAdminListItemResponse(
    Guid Id, string Slug, string Name, string Kind, string State,
    int LatestRevisionNumber, DateTimeOffset UpdatedAt, string Etag);

public sealed record CommunityAdminDraftResponse(
    Guid Id, string Slug, string Name, string Summary, string Markdown,
    string Kind, string State, int LatestRevisionNumber, object Metadata);

public sealed record CommunityCreateResponse(Guid Id, string Slug, string Etag);
public sealed record CommunityWriteResponse(Guid Id, string Etag);
public sealed record CommunityPublishResponse(Guid Id, Guid RevisionId, int Revision, string Etag);

public sealed record CommunityRegistrationResponse(
    Guid Id, string DisplayName, string Contact, string State, DateTimeOffset CreatedAt, string Etag);

public sealed record CommunityRegistrationCreateResponse(
    Guid Id, string DisplayName, string Contact, string State, string Etag);

public sealed record CommunityRegistrationUpdateResponse(Guid Id, string State, string Etag);
