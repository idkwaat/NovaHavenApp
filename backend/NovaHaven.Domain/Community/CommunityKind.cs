namespace NovaHaven.Domain.Community;

public enum CommunityKind
{
    Event,
    Guild,
    Player,
    Housing,
    Leaderboard
}

public enum CommunityState
{
    Draft,
    Published,
    Unpublished
}

public enum EventRegistrationState
{
    Pending,
    Approved,
    Rejected,
    Cancelled
}
