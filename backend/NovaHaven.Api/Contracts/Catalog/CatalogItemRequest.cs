namespace NovaHaven.Api.Contracts.Catalog;

public sealed record CatalogItemRequest(
    string? Name,
    string? Slug,
    string? Summary,
    string? Markdown,
    string? Kind);
