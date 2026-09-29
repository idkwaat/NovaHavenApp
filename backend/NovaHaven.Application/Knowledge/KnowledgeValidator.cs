using NovaHaven.Domain.Knowledge;
using NovaHaven.Domain.Knowledge.Entities;

namespace NovaHaven.Application.Knowledge;

public sealed record KnowledgeCommonInput(string Name, string Slug, string Summary, string Markdown, KnowledgeKind Kind);
public sealed record LocationInput(string Region, string LocationType, decimal? Latitude, decimal? Longitude, string? MapImageUrl);
public sealed record SeasonInput(DateTimeOffset StartsAt, DateTimeOffset EndsAt, string Theme, string? EventDescription);
public sealed record NpcInput(string Role, Guid? LocationEntryId, string? PortraitUrl);
public sealed record QuestStepInput(int Position, string Title, string Description);
public sealed record QuestInput(int Difficulty, Guid? GiverNpcEntryId, Guid? LocationEntryId, string RewardDescription, IReadOnlyList<QuestStepInput> Steps);
public sealed record KnowledgeLinkInput(KnowledgeLinkType LinkType, Guid? TargetEntryId, Guid? TargetCatalogItemId, int SortOrder);

public static class KnowledgeValidator
{
    public static Dictionary<string, string[]> ValidateCommon(KnowledgeCommonInput input)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Trim().Length is < 2 or > 160)
            errors["name"] = ["Name must be between 2 and 160 characters."];
        if (string.IsNullOrWhiteSpace(input.Slug) || input.Slug.Trim().Length is < 2 or > 160 || input.Slug.Any(char.IsUpper) || input.Slug.Contains(' '))
            errors["slug"] = ["Slug must be lowercase, non-empty and contain no spaces."];
        if ((input.Summary?.Length ?? 0) > 500) errors["summary"] = ["Summary cannot exceed 500 characters."];
        if (string.IsNullOrWhiteSpace(input.Markdown) || input.Markdown.Length > 100_000)
            errors["markdown"] = ["Markdown is required and must be at most 100000 characters."];
        return errors;
    }

    public static Dictionary<string, string[]> ValidateNpc(NpcInput input)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(input.Role) || input.Role.Trim().Length > 120)
            errors["role"] = ["Role is required and must be at most 120 characters."];
        return errors;
    }

    public static Dictionary<string, string[]> ValidateQuest(QuestInput input)
    {
        var errors = new Dictionary<string, string[]>();
        if (input.Difficulty is < 1 or > 5) errors["difficulty"] = ["Difficulty must be between 1 and 5."];
        if (string.IsNullOrWhiteSpace(input.RewardDescription) || input.RewardDescription.Length > 500)
            errors["rewardDescription"] = ["Reward description is required and must be at most 500 characters."];
        if (input.Steps.Count is < 1 or > 50 || input.Steps.Select(x => x.Position).Distinct().Count() != input.Steps.Count)
            errors["steps"] = ["Quest steps must contain 1 to 50 unique positions."];
        if (input.Steps.Any(x => x.Position < 1 || string.IsNullOrWhiteSpace(x.Title) || x.Title.Length > 160 || string.IsNullOrWhiteSpace(x.Description) || x.Description.Length > 2_000))
            errors["steps"] = ["Every quest step needs a positive position, title and description within the size limits."];
        return errors;
    }

    public static Dictionary<string, string[]> ValidateLocation(LocationInput input)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(input.Region) || input.Region.Length > 120) errors["region"] = ["Region is required and must be at most 120 characters."];
        if (string.IsNullOrWhiteSpace(input.LocationType) || input.LocationType.Length > 80) errors["locationType"] = ["Location type is required and must be at most 80 characters."];
        if ((input.Latitude.HasValue != input.Longitude.HasValue) || input.Latitude is < -90 or > 90) errors["latitude"] = ["Latitude and longitude must be supplied together and latitude must be between -90 and 90."];
        if (input.Latitude.HasValue && input.Longitude is < -180 or > 180) errors["longitude"] = ["Longitude must be between -180 and 180."];
        if ((input.MapImageUrl?.Length ?? 0) > 500) errors["mapImageUrl"] = ["Map image URL cannot exceed 500 characters."];
        return errors;
    }

    public static Dictionary<string, string[]> ValidateSeason(SeasonInput input)
    {
        var errors = new Dictionary<string, string[]>();
        if (input.EndsAt <= input.StartsAt) errors["endsAt"] = ["Season end must be after its start."];
        if (string.IsNullOrWhiteSpace(input.Theme) || input.Theme.Length > 160) errors["theme"] = ["Theme is required and must be at most 160 characters."];
        if ((input.EventDescription?.Length ?? 0) > 2_000) errors["eventDescription"] = ["Event description cannot exceed 2000 characters."];
        return errors;
    }

    public static Dictionary<string, string[]> ValidateLinks(IReadOnlyList<KnowledgeLinkInput> links)
    {
        var errors = new Dictionary<string, string[]>();
        if (links.Count > 50) errors["links"] = ["At most 50 links are allowed."];
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var link in links)
        {
            var targetCount = (link.TargetEntryId.HasValue ? 1 : 0) + (link.TargetCatalogItemId.HasValue ? 1 : 0);
            var key = $"{link.LinkType}:{link.TargetEntryId}:{link.TargetCatalogItemId}";
            if (targetCount != 1 || link.SortOrder < 0 || !keys.Add(key)) errors["links"] = ["Each link must have one target, a non-negative order and no duplicate target/type pair."];
        }
        return errors;
    }
}
