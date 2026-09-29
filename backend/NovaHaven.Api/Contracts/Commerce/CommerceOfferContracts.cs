using NovaHaven.Domain.Commerce.Entities;

namespace NovaHaven.Api.Contracts.Commerce;

public sealed record CommerceOfferRequest(
    string? Name, string? Slug, string? Summary, string? Markdown, string? Kind,
    string? DisplayPrice, string? ProviderProductCode, bool IsPurchasable = false, long? PriceMinorUnits = null);

public sealed record CommerceOfferSummaryResponse(
    Guid Id, string Slug, string Name, string Summary, string Kind, int Revision,
    bool DefinitionOnly, bool IsPurchasable, long? PriceMinorUnits, string CurrencyCode,
    string? CheckoutMode, DateTimeOffset UpdatedAt);

public sealed record CommerceOfferPageResponse(
    IReadOnlyList<CommerceOfferSummaryResponse> Items, int Page, int PageSize, int Total);

public sealed record CommerceOfferDetailResponse(
    Guid Id, string Slug, string Name, string Summary, string Markdown, string Kind,
    int Revision, string DisplayPrice, string? ProviderProductCode, bool IsPurchasable,
    long? PriceMinorUnits, string CurrencyCode, string? CheckoutMode, bool DefinitionOnly,
    DateTimeOffset PublishedAt, DateTimeOffset UpdatedAt);

public sealed record CommerceOfferAdminListResponse(
    Guid Id, string Slug, string Name, string Kind, CommerceOfferState State,
    int LatestRevisionNumber, DateTimeOffset UpdatedAt, bool DefinitionOnly,
    bool IsPurchasable, long? PriceMinorUnits, string Etag);

public sealed record CommerceOfferAdminDraftResponse(
    Guid Id, string Slug, string Name, string Summary, string Markdown, string Kind,
    string DisplayPrice, string? ProviderProductCode, bool IsPurchasable, long? PriceMinorUnits,
    string CurrencyCode, bool DefinitionOnly, CommerceOfferState State, int LatestRevisionNumber);

public sealed record CommerceOfferCreateResponse(Guid Id, string Slug, bool DefinitionOnly, string Etag);
public sealed record CommerceOfferWriteResponse(Guid Id, bool DefinitionOnly, string Etag);
public sealed record CommerceOfferPublishResponse(Guid Id, int Revision, bool DefinitionOnly, string Etag);
