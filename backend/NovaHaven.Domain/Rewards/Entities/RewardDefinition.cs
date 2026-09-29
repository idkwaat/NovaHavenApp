namespace NovaHaven.Domain.Rewards.Entities;

public enum RewardDefinitionKind { Item, Currency, Permission, Title, Cosmetic, Custom }
public enum RewardDefinitionState { Draft, Published, Unpublished }

public sealed class RewardDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Slug { get; set; } = "";
    public string DraftName { get; set; } = "";
    public string DraftSummary { get; set; } = "";
    public string DraftMarkdown { get; set; } = "";
    public RewardDefinitionKind DraftKind { get; set; }
    public string DraftDeliveryDescription { get; set; } = "";
    public bool ExternalAcknowledgementRequired { get; set; } = true;
    public RewardDefinitionState State { get; set; } = RewardDefinitionState.Draft;
    public Guid? PublishedRevisionId { get; set; }
    public bool WasPublished { get; set; }
    public int LatestRevisionNumber { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class RewardDefinitionRevision
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DefinitionId { get; set; }
    public int Number { get; set; }
    public string Slug { get; set; } = "";
    public string Name { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Markdown { get; set; } = "";
    public RewardDefinitionKind Kind { get; set; }
    public string DeliveryDescription { get; set; } = "";
    public bool ExternalAcknowledgementRequired { get; set; } = true;
    public DateTimeOffset PublishedAt { get; set; }
    public Guid PublishedBy { get; set; }

    public static RewardDefinitionRevision FromDraft(RewardDefinition draft, Guid actorId, DateTimeOffset now) => new()
    {
        DefinitionId = draft.Id, Number = draft.LatestRevisionNumber + 1, Slug = draft.Slug,
        Name = draft.DraftName, Summary = draft.DraftSummary, Markdown = draft.DraftMarkdown,
        Kind = draft.DraftKind, DeliveryDescription = draft.DraftDeliveryDescription,
        ExternalAcknowledgementRequired = true, PublishedAt = now, PublishedBy = actorId
    };
}
