using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NovaHaven.Application.Features.Catalog.Commands;
using NovaHaven.Application.Features.Catalog.Repositories;
using NovaHaven.Application.Features.Catalog.Results;
using NovaHaven.Domain.Catalog.Entities;
using NovaHaven.Domain.Wiki.Entities;
using NovaHaven.Infrastructure.Data;

namespace NovaHaven.Infrastructure.Persistence.Repositories.Catalog;

public sealed class EfCatalogRecipeRepository(NovaDbContext dbContext) : ICatalogRecipeRepository
{
    public Task<int> CountPublishedAsync(string? search, CancellationToken cancellationToken)
    {
        var query = from recipe in dbContext.Recipes.AsNoTracking()
                    join revision in dbContext.RecipeRevisions.AsNoTracking()
                        on recipe.PublishedRevisionId equals (Guid?)revision.Id
                    where recipe.State == CatalogItemState.Published
                    select new { Recipe = recipe, Revision = revision };
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(row => row.Revision.Name.Contains(search) || row.Revision.Summary.Contains(search));
        return query.CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CatalogRecipeListItemResult>> ListPublishedAsync(
        string? search,
        int offset,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = from recipe in dbContext.Recipes.AsNoTracking()
                    join revision in dbContext.RecipeRevisions.AsNoTracking()
                        on recipe.PublishedRevisionId equals (Guid?)revision.Id
                    where recipe.State == CatalogItemState.Published
                    select new { Recipe = recipe, Revision = revision };
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(row => row.Revision.Name.Contains(search) || row.Revision.Summary.Contains(search));
        return await query
            .OrderBy(row => row.Revision.Name)
            .ThenBy(row => row.Recipe.Id)
            .Skip(offset)
            .Take(pageSize)
            .Select(row => new CatalogRecipeListItemResult(
                row.Recipe.Id, row.Recipe.Slug, row.Revision.Name, row.Revision.Summary,
                row.Revision.Number, row.Revision.PublishedAt, row.Recipe.UpdatedAt))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<CatalogRecipeDetailResult?> FindPublishedBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        var row = await (from recipe in dbContext.Recipes.AsNoTracking()
            join revision in dbContext.RecipeRevisions.AsNoTracking()
                on recipe.PublishedRevisionId equals (Guid?)revision.Id
            where recipe.State == CatalogItemState.Published && recipe.Slug == slug
            select new { Recipe = recipe, Revision = revision })
            .Select(candidate => new
            {
                RecipeId = candidate.Recipe.Id,
                candidate.Recipe.Slug,
                candidate.Revision.Name,
                candidate.Revision.Summary,
                candidate.Revision.Markdown,
                RevisionId = candidate.Revision.Id,
                candidate.Revision.Number,
                candidate.Revision.PublishedAt
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (row is null) return null;

        var components = await (from component in dbContext.RecipeRevisionComponents.AsNoTracking()
                                join itemRevision in dbContext.CatalogItemRevisions.AsNoTracking()
                                    on component.CatalogItemRevisionId equals itemRevision.Id
                                where component.RecipeRevisionId == row.RevisionId
                                select new
                                {
                                    component.Role,
                                    ItemId = itemRevision.ItemId,
                                    ItemSlug = itemRevision.Slug,
                                    ItemName = itemRevision.Name,
                                    component.Quantity
                                }).ToArrayAsync(cancellationToken);

        return new CatalogRecipeDetailResult(
            row.RecipeId, row.Slug, row.Name, row.Summary, row.Markdown, row.Number, row.PublishedAt,
            components.Where(component => component.Role == RecipeComponentRole.Ingredient)
                .Select(component => new CatalogRecipeComponentResult(
                    component.ItemId, component.ItemSlug, component.ItemName, component.Quantity)).ToArray(),
            components.Where(component => component.Role == RecipeComponentRole.Output)
                .Select(component => new CatalogRecipeComponentResult(
                    component.ItemId, component.ItemSlug, component.ItemName, component.Quantity)).ToArray());
    }

    public async Task<IReadOnlyList<CatalogRecipeAdminListItemResult>> ListAdminAsync(CancellationToken cancellationToken) =>
        await dbContext.Recipes.AsNoTracking()
            .OrderByDescending(recipe => recipe.UpdatedAt)
            .Take(100)
            .Select(recipe => new CatalogRecipeAdminListItemResult(
                recipe.Id, recipe.Slug, recipe.DraftName, recipe.State,
                recipe.LatestRevisionNumber, recipe.UpdatedAt, recipe.RowVersion))
            .ToArrayAsync(cancellationToken);

    public async Task<CatalogRecipeAdminDraftResult?> FindAdminDraftAsync(Guid id, CancellationToken cancellationToken)
    {
        var recipe = await dbContext.Recipes.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (recipe is null) return null;

        var components = await (from component in dbContext.RecipeDraftComponents.AsNoTracking()
                                join item in dbContext.CatalogItems.AsNoTracking()
                                    on component.CatalogItemId equals item.Id
                                where component.RecipeId == id
                                select new
                                {
                                    component.Role,
                                    ItemId = component.CatalogItemId,
                                    ItemName = item.DraftName,
                                    component.Quantity
                                }).ToArrayAsync(cancellationToken);

        return new CatalogRecipeAdminDraftResult(
            recipe.Id, recipe.Slug, recipe.DraftName, recipe.DraftSummary, recipe.DraftMarkdown,
            recipe.State, recipe.LatestRevisionNumber,
            components.Where(component => component.Role == RecipeComponentRole.Ingredient)
                .Select(component => new CatalogRecipeDraftComponentResult(
                    component.ItemId, component.ItemName, component.Quantity)).ToArray(),
            components.Where(component => component.Role == RecipeComponentRole.Output)
                .Select(component => new CatalogRecipeDraftComponentResult(
                    component.ItemId, component.ItemName, component.Quantity)).ToArray(),
            recipe.RowVersion);
    }

    public Task<GameRecipe?> FindForUpdateAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Recipes.SingleOrDefaultAsync(recipe => recipe.Id == id, cancellationToken);

    public Task<bool> IsSlugInUseAsync(string slug, Guid? excludingId, CancellationToken cancellationToken)
    {
        var query = dbContext.Recipes.AsNoTracking().Where(recipe => recipe.Slug == slug);
        if (excludingId.HasValue) query = query.Where(recipe => recipe.Id != excludingId.Value);
        return query.AnyAsync(cancellationToken);
    }

    public Task<int> CountExistingCatalogItemsAsync(IReadOnlyCollection<Guid> itemIds, CancellationToken cancellationToken) =>
        dbContext.CatalogItems.AsNoTracking().CountAsync(item => itemIds.Contains(item.Id), cancellationToken);

    public async Task<IReadOnlyList<GameRecipeDraftComponent>> ListDraftComponentsAsync(
        Guid recipeId,
        CancellationToken cancellationToken) =>
        await dbContext.RecipeDraftComponents.AsNoTracking()
            .Where(component => component.RecipeId == recipeId)
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyList<CatalogItemPublicationReferenceResult>> FindCatalogItemPublicationReferencesAsync(
        IReadOnlyCollection<Guid> itemIds,
        CancellationToken cancellationToken) =>
        await dbContext.CatalogItems.AsNoTracking()
            .Where(item => itemIds.Contains(item.Id))
            .Select(item => new CatalogItemPublicationReferenceResult(
                item.Id, item.State, item.PublishedRevisionId))
            .ToArrayAsync(cancellationToken);

    public void Add(GameRecipe recipe) => dbContext.Recipes.Add(recipe);

    public void AddDraftComponents(Guid recipeId, CatalogRecipeInput input) =>
        dbContext.RecipeDraftComponents.AddRange(CreateDraftComponents(recipeId, input));

    public async Task ReplaceDraftComponentsAsync(
        Guid recipeId,
        CatalogRecipeInput input,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.RecipeDraftComponents
            .Where(component => component.RecipeId == recipeId)
            .ToListAsync(cancellationToken);
        var requested = CreateDraftComponents(recipeId, input)
            .ToDictionary(component => (component.CatalogItemId, component.Role));

        foreach (var current in existing)
        {
            if (!requested.Remove((current.CatalogItemId, current.Role), out var replacement))
                dbContext.RecipeDraftComponents.Remove(current);
            else
                current.Quantity = replacement.Quantity;
        }

        dbContext.RecipeDraftComponents.AddRange(requested.Values);
    }

    public void AddRevision(GameRecipeRevision revision) => dbContext.RecipeRevisions.Add(revision);

    public void AddRevisionComponents(IReadOnlyCollection<GameRecipeRevisionComponent> components) =>
        dbContext.RecipeRevisionComponents.AddRange(components);

    public void AddAudit(Guid? actorId, string action, Guid recipeId, object? details = null)
    {
        if (actorId is null) return;
        var serialized = JsonSerializer.Serialize(details ?? new { });
        dbContext.AuditEvents.Add(new WikiAuditEvent
        {
            ActorUserId = actorId.Value,
            Action = action,
            EntityType = "GameRecipe",
            EntityId = recipeId,
            DetailsJson = serialized.Length <= 4000 ? serialized : serialized[..4000],
            OccurredAt = DateTimeOffset.UtcNow
        });
    }

    private static GameRecipeDraftComponent[] CreateDraftComponents(Guid recipeId, CatalogRecipeInput input) =>
        input.Ingredients.Select(component => new GameRecipeDraftComponent
            {
                RecipeId = recipeId,
                CatalogItemId = component.ItemId,
                Role = RecipeComponentRole.Ingredient,
                Quantity = component.Quantity
            })
            .Concat(input.Outputs.Select(component => new GameRecipeDraftComponent
            {
                RecipeId = recipeId,
                CatalogItemId = component.ItemId,
                Role = RecipeComponentRole.Output,
                Quantity = component.Quantity
            }))
            .ToArray();
}
