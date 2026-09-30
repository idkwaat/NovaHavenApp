using NovaHaven.Application.Community;

namespace NovaHaven.Application.Features.Community.Commands;

public sealed record CommunityDraftCommand(
    string? Name, string? Slug, string? Summary, string? Markdown, string? BodyKind,
    DateTimeOffset? StartsAt, DateTimeOffset? EndsAt, Guid? LocationEntryId, int? Capacity, bool RegistrationOpen,
    string? Motto, string? DiscordUrl, string? Handle, string? Bio, string? AvatarUrl,
    string? OwnerDisplayName, string? GalleryMarkdown, string? LeaderboardCategory, Guid? SeasonEntryId,
    IReadOnlyList<LeaderboardRowInput>? Rows);

public sealed record CommunityRegistrationCommand(string? DisplayName, string? Contact);
public sealed record CommunityRegistrationUpdateCommand(string? State);
