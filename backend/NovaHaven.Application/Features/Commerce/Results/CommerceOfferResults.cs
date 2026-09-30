using NovaHaven.Domain.Commerce.Entities;

namespace NovaHaven.Application.Features.Commerce.Results;

public sealed record CommerceOfferPageResult(
    IReadOnlyList<CommerceOfferSummaryResult> Items, int Page, int PageSize, int Total);

public sealed record CommerceOfferSummaryResult(
    Guid Id, string Slug, string Name, string Summary, CommerceOfferKind Kind, int Revision,
    bool IsPurchasable, long? PriceMinorUnits, DateTimeOffset PublishedAt);

public sealed record CommerceOfferDetailResult(
    Guid Id, string Slug, string Name, string Summary, string Markdown, CommerceOfferKind Kind,
    int Revision, string DisplayPrice, string? ProviderProductCode, bool IsPurchasable,
    long? PriceMinorUnits, DateTimeOffset PublishedAt);

public sealed record CommerceOfferAdminListResult(
    Guid Id, string Slug, string Name, CommerceOfferKind Kind, CommerceOfferState State,
    int LatestRevisionNumber, DateTimeOffset UpdatedAt, bool IsPurchasable, long? PriceMinorUnits, uint RowVersion);

public sealed record CommerceOfferAdminDraftResult(
    Guid Id, string Slug, string Name, string Summary, string Markdown, CommerceOfferKind Kind,
    string DisplayPrice, string? ProviderProductCode, bool IsPurchasable, long? PriceMinorUnits,
    CommerceOfferState State, int LatestRevisionNumber, uint RowVersion);

public sealed record CommerceOfferWriteResult(Guid Id, string Slug, int Revision, uint RowVersion);
