namespace NovaHaven.Application.Features.Commerce.Results;

public sealed record CommerceOrderLineResult(
    string Slug, string Name, int Revision, int Quantity,
    long UnitPriceMinorUnits, long LineTotalMinorUnits);

public sealed record CommerceOrderReceiptResult(
    string OrderNumber, DateTimeOffset CreatedAt, long TotalMinorUnits,
    string CurrencyCode, IReadOnlyList<CommerceOrderLineResult> Items, bool IdempotentReplay);

public sealed record CommerceOrderHistoryItemResult(
    string OrderNumber, DateTimeOffset CreatedAt, long TotalMinorUnits,
    string CurrencyCode, IReadOnlyList<CommerceOrderLineResult> Items);

public sealed record CommerceOrderHistoryPageResult(
    IReadOnlyList<CommerceOrderHistoryItemResult> Items, int Page, int PageSize, int Total);

public sealed record CommercePublishedOfferForCheckoutResult(
    Guid OfferId, string Slug, Guid RevisionId, int Revision, string Name,
    bool IsPurchasable, long? PriceMinorUnits);
