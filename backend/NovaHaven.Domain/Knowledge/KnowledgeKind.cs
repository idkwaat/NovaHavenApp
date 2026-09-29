namespace NovaHaven.Domain.Knowledge;

public enum KnowledgeKind
{
    Npc,
    Quest,
    Location,
    Season
}

public enum KnowledgeState
{
    Draft,
    Published,
    Unpublished
}

public enum KnowledgeLinkType
{
    Related,
    GivesQuest,
    LocatedAt,
    UsesItem,
    PartOfSeason
}
