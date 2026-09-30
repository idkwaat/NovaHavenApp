namespace NovaHaven.Application.Features.Catalog.Commands;

public sealed record CatalogRecipeComponentInput(Guid ItemId, int Quantity);

public sealed record CatalogRecipeInput(
    string Name,
    string Slug,
    string Summary,
    string Markdown,
    IReadOnlyList<CatalogRecipeComponentInput> Ingredients,
    IReadOnlyList<CatalogRecipeComponentInput> Outputs);
