namespace NovaHaven.Application.Features.Catalog.Queries;

public sealed record CatalogItemListQuery(string? Search, string? Kind, int? Page, int? PageSize);
