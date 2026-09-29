using System.Security.Claims;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NovaHaven.Api.Infrastructure;
using NovaHaven.Application.Catalog;
using NovaHaven.Domain.Catalog;
using NovaHaven.Domain.Catalog.Entities;
using NovaHaven.Infrastructure.Data;

namespace NovaHaven.Api.Endpoints;

public static class CatalogRecipeEndpoints
{
    public static void MapCatalogRecipeEndpoints(this WebApplication app)
    {
        var admin = app.MapGroup("/api/v1/admin/catalog/recipes").RequireAuthorization("AdminOnly");
        admin.AddEndpointFilter(async (context, next) =>
        {
            if (HttpMethods.IsGet(context.HttpContext.Request.Method)) return await next(context);
            var antiforgery = context.HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Antiforgery.IAntiforgery>();
            return await AuthEndpoints.ValidateCsrf(context.HttpContext, antiforgery)
                ? await next(context)
                : Results.Problem(statusCode: 400, title: "Invalid anti-forgery token.");
        });
        admin.MapGet("", AdminListAsync);
        admin.MapPost("", CreateAsync);
        admin.MapGet("/{id:guid}", AdminDetailAsync);
        admin.MapPatch("/{id:guid}", UpdateAsync);
        admin.MapPost("/{id:guid}/publish", PublishAsync);
        admin.MapPost("/{id:guid}/unpublish", UnpublishAsync);

        var publicGroup = app.MapGroup("/api/v1/catalog/recipes");
        publicGroup.MapGet("", ListAsync);
        publicGroup.MapGet("/{slug}", DetailAsync);
    }

    private static async Task<IResult> ListAsync(NovaDbContext db, string? q, int? page, int? pageSize, CancellationToken ct)
    {
        var currentPage = page ?? 1;
        var size = pageSize ?? 20;
        if (currentPage < 1 || size is < 1 or > 50 || (q?.Length ?? 0) > 100)
            return Results.Problem(statusCode: 400, title: "Invalid recipe pagination or filter.");
        var query = from recipe in db.Recipes.AsNoTracking()
                    join revision in db.RecipeRevisions.AsNoTracking()
                        on recipe.PublishedRevisionId equals (Guid?)revision.Id
                    where recipe.State == CatalogItemState.Published
                    select new { Recipe = recipe, Revision = revision };
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(x => x.Revision.Name.Contains(term) || x.Revision.Summary.Contains(term));
        }
        var total = await query.CountAsync(ct);
        var offset = ((long)currentPage - 1) * size;
        if (offset > int.MaxValue) return Results.Problem(statusCode: 400, title: "Page is out of range.");
        var rows = await query.OrderBy(x => x.Revision.Name).ThenBy(x => x.Recipe.Id)
            .Skip((int)offset).Take(size)
            .Select(x => new
            {
                id = x.Recipe.Id, slug = x.Recipe.Slug, name = x.Revision.Name,
                summary = x.Revision.Summary, revision = x.Revision.Number,
                publishedAt = x.Revision.PublishedAt, updatedAt = x.Recipe.UpdatedAt
            }).ToListAsync(ct);
        return Results.Ok(new { items = rows, page = currentPage, pageSize = size, total });
    }

    private static async Task<IResult> DetailAsync(NovaDbContext db, string slug, CancellationToken ct)
    {
        var row = await (from recipe in db.Recipes.AsNoTracking()
                         join revision in db.RecipeRevisions.AsNoTracking()
                             on recipe.PublishedRevisionId equals (Guid?)revision.Id
                         where recipe.State == CatalogItemState.Published && recipe.Slug == slug
                         select new { Recipe = recipe, Revision = revision }).SingleOrDefaultAsync(ct);
        if (row is null) return Results.NotFound();
        var components = await (from component in db.RecipeRevisionComponents.AsNoTracking()
                                join itemRevision in db.CatalogItemRevisions.AsNoTracking()
                                    on component.CatalogItemRevisionId equals itemRevision.Id
                                where component.RecipeRevisionId == row.Revision.Id
                                select new
                                {
                                    role = component.Role,
                                    itemId = itemRevision.ItemId,
                                    itemSlug = itemRevision.Slug,
                                    itemName = itemRevision.Name,
                                    quantity = component.Quantity
                                }).ToListAsync(ct);
        return Results.Ok(new
        {
            id = row.Recipe.Id, slug = row.Recipe.Slug, name = row.Revision.Name,
            summary = row.Revision.Summary, markdown = row.Revision.Markdown,
            revision = row.Revision.Number, publishedAt = row.Revision.PublishedAt,
            ingredients = components.Where(x => x.role == RecipeComponentRole.Ingredient)
                .Select(x => new { x.itemId, x.itemSlug, x.itemName, x.quantity }),
            outputs = components.Where(x => x.role == RecipeComponentRole.Output)
                .Select(x => new { x.itemId, x.itemSlug, x.itemName, x.quantity })
        });
    }

    private static async Task<IResult> AdminListAsync(NovaDbContext db, CancellationToken ct)
    {
        var rows = await db.Recipes.AsNoTracking().OrderByDescending(x => x.UpdatedAt).Take(100).ToListAsync(ct);
        return Results.Ok(rows.Select(x => new
        {
            x.Id, x.Slug, name = x.DraftName, state = x.State.ToString().ToLowerInvariant(),
            x.LatestRevisionNumber, x.UpdatedAt, etag = ETag(x)
        }));
    }

    private static async Task<IResult> CreateAsync(NovaDbContext db, HttpContext http, CatalogRecipeRequest request, CancellationToken ct)
    {
        if (!TryInput(request, out var input, out var parseError)) return parseError!;
        var errors = CatalogRecipeValidator.Validate(input);
        if (errors.Count > 0) return Results.ValidationProblem(errors);
        var missing = await MissingDraftItemsAsync(db, input, ct);
        if (missing is not null) return missing;
        if (await db.Recipes.AnyAsync(x => x.Slug == input.Slug, ct)) return Conflict("Recipe slug already exists.");
        var recipe = new GameRecipe
        {
            Slug = input.Slug, DraftName = input.Name.Trim(), DraftSummary = input.Summary,
            DraftMarkdown = input.Markdown, UpdatedAt = DateTimeOffset.UtcNow
        };
        db.Recipes.Add(recipe);
        AddDraftComponents(db, recipe.Id, input);
        AuditWriter.TryAdd(db, http, "catalog_recipe.created", "GameRecipe", recipe.Id, new { recipe.Slug });
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException e) when (UniqueViolation(e)) { return Conflict("Recipe slug already exists."); }
        return Results.Created($"/api/v1/admin/catalog/recipes/{recipe.Id}", new { recipe.Id, recipe.Slug, etag = ETag(recipe) });
    }

    private static async Task<IResult> AdminDetailAsync(Guid id, NovaDbContext db, HttpContext http, CancellationToken ct)
    {
        var recipe = await db.Recipes.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (recipe is null) return Results.NotFound();
        var components = await (from component in db.RecipeDraftComponents.AsNoTracking()
                                join item in db.CatalogItems.AsNoTracking() on component.CatalogItemId equals item.Id
                                where component.RecipeId == id
                                select new { component.Role, component.CatalogItemId, itemName = item.DraftName, component.Quantity }).ToListAsync(ct);
        http.Response.Headers.ETag = ETag(recipe);
        http.Response.Headers.CacheControl = "no-store";
        return Results.Ok(new
        {
            recipe.Id, recipe.Slug, name = recipe.DraftName, summary = recipe.DraftSummary,
            markdown = recipe.DraftMarkdown, state = recipe.State.ToString().ToLowerInvariant(),
            recipe.LatestRevisionNumber,
            ingredients = components.Where(x => x.Role == RecipeComponentRole.Ingredient)
                .Select(x => new { itemId = x.CatalogItemId, x.itemName, quantity = x.Quantity }),
            outputs = components.Where(x => x.Role == RecipeComponentRole.Output)
                .Select(x => new { itemId = x.CatalogItemId, x.itemName, quantity = x.Quantity })
        });
    }

    private static async Task<IResult> UpdateAsync(Guid id, NovaDbContext db, HttpContext http, CatalogRecipeRequest request, CancellationToken ct)
    {
        var recipe = await db.Recipes.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (recipe is null) return Results.NotFound();
        var precondition = CheckPrecondition(http, recipe);
        if (precondition is not null) return precondition;
        if (!TryInput(request, out var input, out var parseError)) return parseError!;
        var errors = CatalogRecipeValidator.Validate(input);
        if (errors.Count > 0) return Results.ValidationProblem(errors);
        var missing = await MissingDraftItemsAsync(db, input, ct);
        if (missing is not null) return missing;
        if (input.Slug != recipe.Slug && await db.Recipes.AnyAsync(x => x.Slug == input.Slug && x.Id != id, ct))
            return Conflict("Recipe slug already exists.");
        recipe.Slug = input.Slug; recipe.DraftName = input.Name.Trim(); recipe.DraftSummary = input.Summary;
        recipe.DraftMarkdown = input.Markdown; recipe.UpdatedAt = DateTimeOffset.UtcNow;
        var oldComponents = await db.RecipeDraftComponents.Where(x => x.RecipeId == id).ToListAsync(ct);
        db.RecipeDraftComponents.RemoveRange(oldComponents);
        AddDraftComponents(db, recipe.Id, input);
        AuditWriter.TryAdd(db, http, "catalog_recipe.edited", "GameRecipe", recipe.Id, new { recipe.Slug });
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return Stale(); }
        catch (DbUpdateException e) when (UniqueViolation(e)) { return Conflict("Recipe slug already exists."); }
        http.Response.Headers.ETag = ETag(recipe);
        return Results.Ok(new { recipe.Id, etag = ETag(recipe) });
    }

    private static async Task<IResult> PublishAsync(Guid id, NovaDbContext db, HttpContext http, CancellationToken ct)
    {
        var recipe = await db.Recipes.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (recipe is null) return Results.NotFound();
        var precondition = CheckPrecondition(http, recipe);
        if (precondition is not null) return precondition;
        var draftComponents = await db.RecipeDraftComponents.Where(x => x.RecipeId == id).ToListAsync(ct);
        var input = new CatalogRecipeInput(recipe.DraftName, recipe.Slug, recipe.DraftSummary, recipe.DraftMarkdown,
            draftComponents.Where(x => x.Role == RecipeComponentRole.Ingredient).Select(x => new CatalogRecipeComponentInput(x.CatalogItemId, x.Quantity)).ToArray(),
            draftComponents.Where(x => x.Role == RecipeComponentRole.Output).Select(x => new CatalogRecipeComponentInput(x.CatalogItemId, x.Quantity)).ToArray());
        var errors = CatalogRecipeValidator.Validate(input);
        if (errors.Count > 0) return Results.ValidationProblem(errors);
        var itemIds = draftComponents.Select(x => x.CatalogItemId).Distinct().ToArray();
        var items = await db.CatalogItems.Where(x => itemIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        if (items.Count != itemIds.Length || items.Values.Any(x => x.State != CatalogItemState.Published || x.PublishedRevisionId is null))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["components"] = ["Every recipe ingredient and output must reference a published Catalog item."] });
        var actor = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(actor, out var actorId)) return Results.Unauthorized();
        var now = DateTimeOffset.UtcNow;
        var revision = GameRecipeRevision.FromDraft(recipe, actorId, now);
        recipe.State = CatalogItemState.Published; recipe.WasPublished = true; recipe.PublishedRevisionId = revision.Id;
        recipe.LatestRevisionNumber = revision.Number; recipe.UpdatedAt = now;
        db.RecipeRevisions.Add(revision);
        db.RecipeRevisionComponents.AddRange(draftComponents.Select(x => new GameRecipeRevisionComponent
        {
            RecipeRevisionId = revision.Id,
            CatalogItemRevisionId = items[x.CatalogItemId].PublishedRevisionId!.Value,
            Role = x.Role,
            Quantity = x.Quantity
        }));
        AuditWriter.TryAdd(db, http, "catalog_recipe.published", "GameRecipe", recipe.Id, new { revision = revision.Number });
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return Stale(); }
        http.Response.Headers.ETag = ETag(recipe);
        return Results.Ok(new { recipe.Id, revisionId = revision.Id, revision.Number, publishedAt = revision.PublishedAt, etag = ETag(recipe) });
    }

    private static async Task<IResult> UnpublishAsync(Guid id, NovaDbContext db, HttpContext http, CancellationToken ct)
    {
        var recipe = await db.Recipes.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (recipe is null) return Results.NotFound();
        var precondition = CheckPrecondition(http, recipe);
        if (precondition is not null) return precondition;
        if (recipe.State != CatalogItemState.Published) return Conflict("Recipe is not currently published.");
        recipe.State = CatalogItemState.Unpublished; recipe.UpdatedAt = DateTimeOffset.UtcNow;
        AuditWriter.TryAdd(db, http, "catalog_recipe.unpublished", "GameRecipe", recipe.Id);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return Stale(); }
        http.Response.Headers.ETag = ETag(recipe);
        return Results.NoContent();
    }

    private static async Task<IResult?> MissingDraftItemsAsync(NovaDbContext db, CatalogRecipeInput input, CancellationToken ct)
    {
        var ids = input.Ingredients.Concat(input.Outputs).Select(x => x.ItemId).Distinct().ToArray();
        var count = await db.CatalogItems.CountAsync(x => ids.Contains(x.Id), ct);
        return count == ids.Length ? null : Results.ValidationProblem(new Dictionary<string, string[]> { ["components"] = ["Every component must reference an existing Catalog item."] });
    }

    private static void AddDraftComponents(NovaDbContext db, Guid recipeId, CatalogRecipeInput input)
    {
        db.RecipeDraftComponents.AddRange(
            input.Ingredients.Select(x => new GameRecipeDraftComponent { RecipeId = recipeId, CatalogItemId = x.ItemId, Role = RecipeComponentRole.Ingredient, Quantity = x.Quantity })
                .Concat(input.Outputs.Select(x => new GameRecipeDraftComponent { RecipeId = recipeId, CatalogItemId = x.ItemId, Role = RecipeComponentRole.Output, Quantity = x.Quantity })));
    }

    private static bool TryInput(CatalogRecipeRequest request, out CatalogRecipeInput input, out IResult? error)
    {
        input = new CatalogRecipeInput(request.Name ?? "", request.Slug ?? "", request.Summary ?? "", request.Markdown ?? "",
            request.Ingredients ?? [], request.Outputs ?? []);
        error = null;
        return true;
    }

    private static string ETag(GameRecipe recipe) => $"\"{Convert.ToBase64String(recipe.RowVersion)}\"";
    private static IResult? CheckPrecondition(HttpContext http, GameRecipe recipe)
    {
        if (!http.Request.Headers.TryGetValue("If-Match", out var value) || string.IsNullOrWhiteSpace(value.ToString()))
            return Results.Problem(statusCode: 428, title: "If-Match is required.");
        return string.Equals(value.ToString(), ETag(recipe), StringComparison.Ordinal) ? null : Stale();
    }
    private static IResult Stale() => Results.Problem(statusCode: 412, title: "Recipe changed; reload before editing.");
    private static IResult Conflict(string title) => Results.Problem(statusCode: 409, title: title);
    private static bool UniqueViolation(DbUpdateException exception) => exception.InnerException is SqlException { Number: 2601 or 2627 };
}

public sealed record CatalogRecipeRequest(
    string? Name,
    string? Slug,
    string? Summary,
    string? Markdown,
    IReadOnlyList<CatalogRecipeComponentInput>? Ingredients,
    IReadOnlyList<CatalogRecipeComponentInput>? Outputs);
