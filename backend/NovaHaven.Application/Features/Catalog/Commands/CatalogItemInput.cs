using NovaHaven.Domain.Catalog.Entities;

namespace NovaHaven.Application.Features.Catalog.Commands;

public sealed record CatalogItemInput(
    string Name,
    string Slug,
    string Summary,
    string Markdown,
    CatalogItemKind Kind);
