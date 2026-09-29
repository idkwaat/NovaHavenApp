namespace NovaHaven.Application.Features.Catalog.Commands;

public sealed record CatalogItemDraftInput(
    string? Name,
    string? Slug,
    string? Summary,
    string? Markdown,
    string? Kind);
