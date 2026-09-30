using NovaHaven.Application.Features.Catalog.Results;
using NovaHaven.Domain.Catalog.Entities;

namespace NovaHaven.Application.Features.Catalog.Repositories;

public interface ICatalogItemRepository
{
    Task<int> CountPublishedAsync(string? search, CatalogItemKind? kind, CancellationToken cancellationToken);

    Task<IReadOnlyList<CatalogItemListItemResult>> ListPublishedAsync(
        string? search,
        CatalogItemKind? kind,
        int offset,
        int pageSize,
        CancellationToken cancellationToken);

    Task<CatalogItemDetailResult?> FindPublishedBySlugAsync(string slug, CancellationToken cancellationToken);

    Task<IReadOnlyList<CatalogItemAdminListItemResult>> ListAdminAsync(CancellationToken cancellationToken);

    Task<CatalogItemAdminDraftResult?> FindAdminDraftAsync(Guid id, CancellationToken cancellationToken);

    Task<GameCatalogItem?> FindForUpdateAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> IsSlugInUseAsync(string slug, Guid? excludingId, CancellationToken cancellationToken);

    void Add(GameCatalogItem item);

    void AddRevision(GameCatalogItemRevision revision);

    void AddAudit(Guid? actorId, string action, Guid itemId, object? details = null);
}
