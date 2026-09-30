namespace NovaHaven.Api.Contracts.Catalog;

public sealed record CatalogRecipeComponentRequest(Guid ItemId, int Quantity);

public sealed record CatalogRecipeRequest(
    string? Name,
    string? Slug,
    string? Summary,
    string? Markdown,
    IReadOnlyList<CatalogRecipeComponentRequest>? Ingredients,
    IReadOnlyList<CatalogRecipeComponentRequest>? Outputs);
