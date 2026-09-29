namespace NovaHaven.Api.Contracts.Commerce;

public sealed record CommerceCheckoutLineRequest(string Slug, int Quantity);
public sealed record CommerceCheckoutRequest(IReadOnlyList<CommerceCheckoutLineRequest>? Items);

public sealed record CommerceOrderLineResponse(
    string Slug, string Name, int Revision, int Quantity,
    long UnitPriceMinorUnits, long LineTotalMinorUnits);

public sealed record CommerceOrderReceiptResponse(
    string OrderNumber, DateTimeOffset CreatedAt, string Status, long TotalMinorUnits,
    string CurrencyCode, IReadOnlyList<CommerceOrderLineResponse> Items,
    string PaymentStatus, string PaymentMethod, bool RealCharge, string Fulfilment,
    bool IdempotentReplay, string Message);

public sealed record CommerceOrderHistoryItemResponse(
    string OrderNumber, DateTimeOffset CreatedAt, string Status, long TotalMinorUnits,
    string CurrencyCode, string PaymentStatus, string PaymentMethod, bool RealCharge,
    string Fulfilment, IReadOnlyList<CommerceOrderLineResponse> Items);

public sealed record CommerceOrderHistoryPageResponse(
    IReadOnlyList<CommerceOrderHistoryItemResponse> Items, int Page, int PageSize, int Total);
