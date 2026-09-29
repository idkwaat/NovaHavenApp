namespace NovaHaven.Domain.Commerce.Entities;

public enum CommerceOfferKind { Cosmetic, Membership, Bundle, Donation, Other }
public enum CommerceOfferState { Draft, Published, Unpublished }

public sealed class CommerceOffer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Slug { get; set; } = "";
    public string DraftName { get; set; } = "";
    public string DraftSummary { get; set; } = "";
    public string DraftMarkdown { get; set; } = "";
    public CommerceOfferKind DraftKind { get; set; }
    public string DraftDisplayPrice { get; set; } = "";
    public string? DraftProviderProductCode { get; set; }
    public bool DraftIsPurchasable { get; set; }
    public long? DraftPriceMinorUnits { get; set; }
    public CommerceOfferState State { get; set; } = CommerceOfferState.Draft;
    public Guid? PublishedRevisionId { get; set; }
    public bool WasPublished { get; set; }
    public int LatestRevisionNumber { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class CommerceOfferRevision
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OfferId { get; set; }
    public int Number { get; set; }
    public string Slug { get; set; } = "";
    public string Name { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Markdown { get; set; } = "";
    public CommerceOfferKind Kind { get; set; }
    public string DisplayPrice { get; set; } = "";
    public string? ProviderProductCode { get; set; }
    public bool IsPurchasable { get; set; }
    public long? PriceMinorUnits { get; set; }
    public bool DefinitionOnly { get; set; } = true;
    public DateTimeOffset PublishedAt { get; set; }
    public Guid PublishedBy { get; set; }

    public static CommerceOfferRevision FromDraft(CommerceOffer draft, Guid actorId, DateTimeOffset now) => new()
    {
        OfferId = draft.Id, Number = draft.LatestRevisionNumber + 1, Slug = draft.Slug,
        Name = draft.DraftName, Summary = draft.DraftSummary, Markdown = draft.DraftMarkdown,
        Kind = draft.DraftKind, DisplayPrice = draft.DraftDisplayPrice,
        ProviderProductCode = draft.DraftProviderProductCode,
        IsPurchasable = draft.DraftIsPurchasable, PriceMinorUnits = draft.DraftPriceMinorUnits,
        DefinitionOnly = true,
        PublishedAt = now, PublishedBy = actorId
    };
}
