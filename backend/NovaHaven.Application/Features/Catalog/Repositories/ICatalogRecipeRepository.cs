using NovaHaven.Application.Features.Catalog.Commands;
using NovaHaven.Application.Features.Catalog.Results;
using NovaHaven.Domain.Catalog.Entities;

namespace NovaHaven.Application.Features.Catalog.Repositories;

public interface ICatalogRecipeRepository
{
    Task<int> CountPublishedAsync(string? search, CancellationToken cancellationToken);

    Task<IReadOnlyList<CatalogRecipeListItemResult>> ListPublishedAsync(
        string? search,
        int offset,
        int pageSize,
        CancellationToken cancellationToken);

    Task<CatalogRecipeDetailResult?> FindPublishedBySlugAsync(string slug, CancellationToken cancellationToken);

    Task<IReadOnlyList<CatalogRecipeAdminListItemResult>> ListAdminAsync(CancellationToken cancellationToken);

    Task<CatalogRecipeAdminDraftResult?> FindAdminDraftAsync(Guid id, CancellationToken cancellationToken);

    Task<GameRecipe?> FindForUpdateAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> IsSlugInUseAsync(string slug, Guid? excludingId, CancellationToken cancellationToken);

    Task<int> CountExistingCatalogItemsAsync(IReadOnlyCollection<Guid> itemIds, CancellationToken cancellationToken);

    Task<IReadOnlyList<GameRecipeDraftComponent>> ListDraftComponentsAsync(Guid recipeId, CancellationToken cancellationToken);

    Task<IReadOnlyList<CatalogItemPublicationReferenceResult>> FindCatalogItemPublicationReferencesAsync(
        IReadOnlyCollection<Guid> itemIds,
        CancellationToken cancellationToken);

    void Add(GameRecipe recipe);

    void AddDraftComponents(Guid recipeId, CatalogRecipeInput input);

    Task ReplaceDraftComponentsAsync(Guid recipeId, CatalogRecipeInput input, CancellationToken cancellationToken);

    void AddRevision(GameRecipeRevision revision);

    void AddRevisionComponents(IReadOnlyCollection<GameRecipeRevisionComponent> components);

    void AddAudit(Guid? actorId, string action, Guid recipeId, object? details = null);
}
