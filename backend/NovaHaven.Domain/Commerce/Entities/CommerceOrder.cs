namespace NovaHaven.Domain.Commerce.Entities;

public enum CommerceOrderStatus { DemoCompleted }
public enum CommercePaymentStatus { Simulated }
public enum CommercePaymentMethod { LocalDemo }

public sealed class CommerceOrder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OrderNumber { get; set; } = "";
    public Guid IdempotencyKey { get; set; }
    public string RequestFingerprint { get; set; } = "";
    public long TotalMinorUnits { get; set; }
    public string CurrencyCode { get; set; } = "VND";
    public CommerceOrderStatus Status { get; set; } = CommerceOrderStatus.DemoCompleted;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class CommerceOrderLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Guid OfferId { get; set; }
    public Guid OfferRevisionId { get; set; }
    public int RevisionNumber { get; set; }
    public string OfferSlug { get; set; } = "";
    public string OfferName { get; set; } = "";
    public int Quantity { get; set; }
    public long UnitPriceMinorUnits { get; set; }
    public long LineTotalMinorUnits { get; set; }
}

public sealed class CommercePayment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public CommercePaymentMethod Method { get; set; } = CommercePaymentMethod.LocalDemo;
    public CommercePaymentStatus Status { get; set; } = CommercePaymentStatus.Simulated;
    public long AmountMinorUnits { get; set; }
    public string CurrencyCode { get; set; } = "VND";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
