using NovaHaven.Application.Knowledge;

namespace NovaHaven.Application.Features.Knowledge.Commands;

public sealed record KnowledgeDraftCommand(
    string? Name,
    string? Slug,
    string? Summary,
    string? Markdown,
    string? BodyKind,
    string? Role,
    Guid? LocationEntryId,
    string? PortraitUrl,
    int? Difficulty,
    Guid? GiverNpcEntryId,
    string? RewardDescription,
    IReadOnlyList<QuestStepInput>? Steps,
    string? Region,
    string? LocationType,
    decimal? Latitude,
    decimal? Longitude,
    string? MapImageUrl,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt,
    string? Theme,
    string? EventDescription,
    IReadOnlyList<KnowledgeLinkInput>? Links);
