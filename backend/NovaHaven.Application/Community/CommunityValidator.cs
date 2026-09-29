using NovaHaven.Domain.Community;
using NovaHaven.Domain.Community.Entities;

namespace NovaHaven.Application.Community;

public sealed record CommunityCommonInput(string Name, string Slug, string Summary, string Markdown, CommunityKind Kind);
public sealed record CommunityEventInput(DateTimeOffset? StartsAt, DateTimeOffset? EndsAt, Guid? LocationEntryId, int? Capacity, bool RegistrationOpen);
public sealed record GuildInput(string Motto, string DiscordUrl);
public sealed record PlayerInput(string Handle, string Bio, string AvatarUrl);
public sealed record HousingInput(string OwnerDisplayName, string GalleryMarkdown, Guid? LocationEntryId = null);
public sealed record LeaderboardRowInput(int Rank, string ParticipantName, decimal Score, string Note);
public sealed record LeaderboardInput(string Category, IReadOnlyList<LeaderboardRowInput> Rows, Guid? SeasonEntryId = null);

public static class CommunityValidator
{
    public static Dictionary<string, string[]> ValidateCommon(CommunityCommonInput input)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Trim().Length is < 2 or > 160) errors["name"] = ["Name must be between 2 and 160 characters."];
        if (string.IsNullOrWhiteSpace(input.Slug) || input.Slug.Trim().Length is < 2 or > 160 || input.Slug.Any(char.IsUpper) || input.Slug.Contains(' ')) errors["slug"] = ["Slug must be lowercase and contain no spaces."];
        if ((input.Summary?.Length ?? 0) > 500) errors["summary"] = ["Summary cannot exceed 500 characters."];
        if (string.IsNullOrWhiteSpace(input.Markdown) || input.Markdown.Length > 100_000) errors["markdown"] = ["Markdown is required and must be at most 100000 characters."];
        return errors;
    }

    public static Dictionary<string, string[]> ValidateEvent(CommunityEventInput input)
    {
        var errors = new Dictionary<string, string[]>();
        if (input.StartsAt is null || input.EndsAt is null || input.EndsAt <= input.StartsAt) errors["endsAt"] = ["Event end must be after its start."];
        if (input.Capacity is < 1 or > 100_000) errors["capacity"] = ["Capacity must be between 1 and 100000."];
        return errors;
    }

    public static Dictionary<string, string[]> ValidateGuild(GuildInput input)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(input.Motto) || input.Motto.Length > 300) errors["motto"] = ["Guild motto is required and must be at most 300 characters."];
        if ((input.DiscordUrl?.Length ?? 0) > 500) errors["discordUrl"] = ["Discord URL cannot exceed 500 characters."];
        return errors;
    }

    public static Dictionary<string, string[]> ValidatePlayer(PlayerInput input)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(input.Handle) || input.Handle.Length is < 2 or > 80) errors["handle"] = ["Player handle must be between 2 and 80 characters."];
        if ((input.Bio?.Length ?? 0) > 2_000) errors["bio"] = ["Bio cannot exceed 2000 characters."];
        if ((input.AvatarUrl?.Length ?? 0) > 500) errors["avatarUrl"] = ["Avatar URL cannot exceed 500 characters."];
        return errors;
    }

    public static Dictionary<string, string[]> ValidateHousing(HousingInput input)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(input.OwnerDisplayName) || input.OwnerDisplayName.Length > 120) errors["ownerDisplayName"] = ["Owner display name is required and must be at most 120 characters."];
        if (string.IsNullOrWhiteSpace(input.GalleryMarkdown) || input.GalleryMarkdown.Length > 10_000) errors["galleryMarkdown"] = ["Gallery Markdown is required and must be at most 10000 characters."];
        return errors;
    }

    public static Dictionary<string, string[]> ValidateLeaderboard(LeaderboardInput input)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(input.Category) || input.Category.Length > 120) errors["category"] = ["Leaderboard category is required and must be at most 120 characters."];
        if (input.Rows.Count is < 1 or > 100 || input.Rows.Select(x => x.Rank).Distinct().Count() != input.Rows.Count || input.Rows.Any(x => x.Rank < 1 || string.IsNullOrWhiteSpace(x.ParticipantName) || x.ParticipantName.Length > 120)) errors["rows"] = ["Leaderboard rows must contain 1 to 100 unique positive ranks and participant names."];
        return errors;
    }
}
