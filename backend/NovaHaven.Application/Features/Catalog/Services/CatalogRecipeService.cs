using NovaHaven.Application.Common.Concurrency;
using NovaHaven.Application.Common.Results;
using NovaHaven.Application.Common.Transactions;
using NovaHaven.Application.Features.Catalog.Commands;
using NovaHaven.Application.Features.Catalog.Queries;
using NovaHaven.Application.Features.Catalog.Repositories;
using NovaHaven.Application.Features.Catalog.Results;
using NovaHaven.Application.Features.Catalog.Validators;
using NovaHaven.Application.Features.Notifications;
using NovaHaven.Domain.Catalog.Entities;

namespace NovaHaven.Application.Features.Catalog.Services;

public sealed class CatalogRecipeService(
    ICatalogRecipeRepository repository,
    IUnitOfWork unitOfWork,
    IUserNotificationPublisher notificationPublisher)
{
    public async Task<ApplicationResult<CatalogRecipePageResult>> ListPublishedAsync(
        CatalogRecipeListQuery query,
        CancellationToken cancellationToken)
    {
        var page = query.Page ?? 1;
        var pageSize = query.PageSize ?? 20;
        if (page < 1 || pageSize is < 1 or > 50 || (query.Search?.Length ?? 0) > 100)
            return RequestFailure<CatalogRecipePageResult>("Invalid recipe pagination or filter.");

        var offset = ((long)page - 1) * pageSize;
        if (offset > int.MaxValue) return RequestFailure<CatalogRecipePageResult>("Page is out of range.");

        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        var total = await repository.CountPublishedAsync(search, cancellationToken);
        var recipes = await repository.ListPublishedAsync(search, (int)offset, pageSize, cancellationToken);
        return Success(new CatalogRecipePageResult(recipes, page, pageSize, total));
    }

    public async Task<ApplicationResult<CatalogRecipeDetailResult>> GetPublishedAsync(
        string slug,
        CancellationToken cancellationToken)
    {
        var recipe = await repository.FindPublishedBySlugAsync(slug, cancellationToken);
        return recipe is null ? NotFound<CatalogRecipeDetailResult>() : Success(recipe);
    }

    public async Task<ApplicationResult<IReadOnlyList<CatalogRecipeAdminListItemResult>>> ListAdminAsync(
        CancellationToken cancellationToken) =>
        Success(await repository.ListAdminAsync(cancellationToken));

    public async Task<ApplicationResult<CatalogRecipeAdminDraftResult>> GetAdminDraftAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var recipe = await repository.FindAdminDraftAsync(id, cancellationToken);
        return recipe is null ? NotFound<CatalogRecipeAdminDraftResult>() : Success(recipe);
    }

    public async Task<ApplicationResult<CatalogRecipeWriteResult>> CreateAsync(
        CatalogRecipeInput input,
        Guid? actorId,
        CancellationToken cancellationToken)
    {
        var errors = CatalogRecipeValidator.Validate(input);
        if (errors.Count > 0) return ValidationFailure<CatalogRecipeWriteResult>(errors);
        if (!await AllItemsExistAsync(input, cancellationToken))
            return ComponentFailure<CatalogRecipeWriteResult>("Every component must reference an existing Catalog item.");
        if (await repository.IsSlugInUseAsync(input.Slug, null, cancellationToken))
            return Conflict<CatalogRecipeWriteResult>("Recipe slug already exists.");

        var now = DateTimeOffset.UtcNow;
        var recipe = new GameRecipe
        {
            Slug = input.Slug,
            DraftName = input.Name.Trim(),
            DraftSummary = input.Summary,
            DraftMarkdown = input.Markdown,
            CreatedAt = now,
            UpdatedAt = now
        };
        repository.Add(recipe);
        repository.AddDraftComponents(recipe.Id, input);
        repository.AddAudit(actorId, "catalog_recipe.created", recipe.Id, new { recipe.Slug });
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Success(new CatalogRecipeWriteResult(recipe.Id, recipe.Slug, recipe.RowVersion));
    }

    public async Task<ApplicationResult<CatalogRecipeWriteResult>> UpdateAsync(
        Guid id,
        CatalogRecipeInput input,
        byte[]? expectedVersion,
        Guid? actorId,
        CancellationToken cancellationToken)
    {
        var recipe = await repository.FindForUpdateAsync(id, cancellationToken);
        if (recipe is null) return NotFound<CatalogRecipeWriteResult>();
        var precondition = CheckPrecondition(recipe.RowVersion, expectedVersion);
        if (precondition is not null) return ApplicationResult<CatalogRecipeWriteResult>.Failure(precondition);

        var errors = CatalogRecipeValidator.Validate(input);
        if (errors.Count > 0) return ValidationFailure<CatalogRecipeWriteResult>(errors);
        if (!await AllItemsExistAsync(input, cancellationToken))
            return ComponentFailure<CatalogRecipeWriteResult>("Every component must reference an existing Catalog item.");
        if (input.Slug != recipe.Slug && await repository.IsSlugInUseAsync(input.Slug, id, cancellationToken))
            return Conflict<CatalogRecipeWriteResult>("Recipe slug already exists.");

        recipe.Slug = input.Slug;
        recipe.DraftName = input.Name.Trim();
        recipe.DraftSummary = input.Summary;
        recipe.DraftMarkdown = input.Markdown;
        recipe.UpdatedAt = DateTimeOffset.UtcNow;
        await repository.ReplaceDraftComponentsAsync(id, input, cancellationToken);
        repository.AddAudit(actorId, "catalog_recipe.edited", recipe.Id, new { recipe.Slug });
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Success(new CatalogRecipeWriteResult(recipe.Id, recipe.Slug, recipe.RowVersion));
    }

    public async Task<ApplicationResult<CatalogRecipePublishResult>> PublishAsync(
        Guid id,
        byte[]? expectedVersion,
        Guid publisherId,
        Guid? actorId,
        CancellationToken cancellationToken)
    {
        var recipe = await repository.FindForUpdateAsync(id, cancellationToken);
        if (recipe is null) return NotFound<CatalogRecipePublishResult>();
        var precondition = CheckPrecondition(recipe.RowVersion, expectedVersion);
        if (precondition is not null) return ApplicationResult<CatalogRecipePublishResult>.Failure(precondition);

        var components = await repository.ListDraftComponentsAsync(id, cancellationToken);
        var input = new CatalogRecipeInput(
            recipe.DraftName, recipe.Slug, recipe.DraftSummary, recipe.DraftMarkdown,
            components.Where(x => x.Role == RecipeComponentRole.Ingredient)
                .Select(x => new CatalogRecipeComponentInput(x.CatalogItemId, x.Quantity)).ToArray(),
            components.Where(x => x.Role == RecipeComponentRole.Output)
                .Select(x => new CatalogRecipeComponentInput(x.CatalogItemId, x.Quantity)).ToArray());
        var errors = CatalogRecipeValidator.Validate(input);
        if (errors.Count > 0) return ValidationFailure<CatalogRecipePublishResult>(errors);

        var itemIds = components.Select(x => x.CatalogItemId).Distinct().ToArray();
        var publishedItems = await repository.FindCatalogItemPublicationReferencesAsync(itemIds, cancellationToken);
        if (publishedItems.Count != itemIds.Length
            || publishedItems.Any(item => item.State != CatalogItemState.Published || item.PublishedRevisionId is null))
            return ComponentFailure<CatalogRecipePublishResult>(
                "Every recipe ingredient and output must reference a published Catalog item.");

        var publishedRevisionByItemId = publishedItems.ToDictionary(item => item.ItemId, item => item.PublishedRevisionId!.Value);
        var now = DateTimeOffset.UtcNow;
        var revision = GameRecipeRevision.FromDraft(recipe, publisherId, now);
        recipe.State = CatalogItemState.Published;
        recipe.WasPublished = true;
        recipe.PublishedRevisionId = revision.Id;
        recipe.LatestRevisionNumber = revision.Number;
        recipe.UpdatedAt = now;
        repository.AddRevision(revision);
        repository.AddRevisionComponents(components.Select(component => new GameRecipeRevisionComponent
        {
            RecipeRevisionId = revision.Id,
            CatalogItemRevisionId = publishedRevisionByItemId[component.CatalogItemId],
            Role = component.Role,
            Quantity = component.Quantity
        }).ToArray());
        var notification = await notificationPublisher.StageForAllUsersAsync(
            $"Công thức mới: {recipe.DraftName}", recipe.DraftSummary, $"/catalog/recipes/{recipe.Slug}", cancellationToken);
        repository.AddAudit(actorId, "catalog_recipe.published", recipe.Id, new { revision = revision.Number });
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await notificationPublisher.DeliverPushAsync(notification, cancellationToken);
        return Success(new CatalogRecipePublishResult(
            recipe.Id, revision.Id, revision.Number, revision.PublishedAt, recipe.RowVersion));
    }

    public async Task<ApplicationResult<uint>> UnpublishAsync(
        Guid id,
        byte[]? expectedVersion,
        Guid? actorId,
        CancellationToken cancellationToken)
    {
        var recipe = await repository.FindForUpdateAsync(id, cancellationToken);
        if (recipe is null) return NotFound<uint>();
        var precondition = CheckPrecondition(recipe.RowVersion, expectedVersion);
        if (precondition is not null) return ApplicationResult<uint>.Failure(precondition);
        if (recipe.State != CatalogItemState.Published)
            return Conflict<uint>("Recipe is not currently published.");

        recipe.State = CatalogItemState.Unpublished;
        recipe.UpdatedAt = DateTimeOffset.UtcNow;
        repository.AddAudit(actorId, "catalog_recipe.unpublished", recipe.Id);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Success(recipe.RowVersion);
    }

    private async Task<bool> AllItemsExistAsync(CatalogRecipeInput input, CancellationToken cancellationToken)
    {
        var ids = input.Ingredients.Concat(input.Outputs).Select(component => component.ItemId).Distinct().ToArray();
        return await repository.CountExistingCatalogItemsAsync(ids, cancellationToken) == ids.Length;
    }

    private static ApplicationError? CheckPrecondition(uint rowVersion, byte[]? expectedVersion)
    {
        if (expectedVersion is null)
            return new ApplicationError("http.precondition-required", "If-Match is required.");
        if (!ConcurrencyVersion.Matches(rowVersion, expectedVersion))
            return new ApplicationError("http.precondition-failed", "Recipe changed; reload before editing.");
        return null;
    }

    private static ApplicationResult<T> Success<T>(T value) => ApplicationResult<T>.Success(value);
    private static ApplicationResult<T> NotFound<T>() => Failure<T>("catalog.recipe.not-found", "The requested Recipe was not found.");
    private static ApplicationResult<T> Conflict<T>(string message) => Failure<T>("catalog.recipe.conflict", message);
    private static ApplicationResult<T> RequestFailure<T>(string message) => Failure<T>("validation.failed", message);
    private static ApplicationResult<T> ComponentFailure<T>(string message) =>
        ApplicationResult<T>.Failure(new ApplicationError("validation.failed", "One or more validation errors occurred.",
            new Dictionary<string, string[]> { ["components"] = [message] }));
    private static ApplicationResult<T> ValidationFailure<T>(Dictionary<string, string[]> errors) =>
        ApplicationResult<T>.Failure(new ApplicationError("validation.failed", "One or more validation errors occurred.", errors));
    private static ApplicationResult<T> Failure<T>(string code, string message) =>
        ApplicationResult<T>.Failure(new ApplicationError(code, message));
}
