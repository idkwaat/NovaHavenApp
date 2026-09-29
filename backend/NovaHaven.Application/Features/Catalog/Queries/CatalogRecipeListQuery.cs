namespace NovaHaven.Application.Features.Catalog.Queries;

public sealed record CatalogRecipeListQuery(string? Search, int? Page, int? PageSize);
