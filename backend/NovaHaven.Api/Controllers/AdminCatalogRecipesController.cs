using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NovaHaven.Api.Contracts.Catalog;
using NovaHaven.Api.Errors;
using NovaHaven.Api.Filters;
using NovaHaven.Api.Http;
using NovaHaven.Application.Features.Catalog.Commands;
using NovaHaven.Application.Features.Catalog.Services;
using NovaHaven.Domain.Catalog.Entities;

namespace NovaHaven.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[PersistenceConflictExceptionFilter]
[Route("api/v1/admin/catalog/recipes")]
public sealed class AdminCatalogRecipesController(CatalogRecipeService catalogRecipeService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CatalogRecipeAdminListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await catalogRecipeService.ListAdminAsync(cancellationToken);
        if (!result.IsSuccess) return ApplicationProblemDetailsMapper.ToActionResult(result.Error!);
        return Ok(result.Value!.Select(recipe => new CatalogRecipeAdminListItemResponse(
            recipe.Id, recipe.Slug, recipe.Name, StateName(recipe.State), recipe.LatestRevisionNumber,
            recipe.UpdatedAt, RowVersionEtag.Format(recipe.RowVersion))).ToArray());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(CatalogRecipeCreateResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CatalogRecipeRequest request, CancellationToken cancellationToken)
    {
        var result = await catalogRecipeService.CreateAsync(ToInput(request), GetActorId(), cancellationToken);
        if (!result.IsSuccess) return ApplicationProblemDetailsMapper.ToActionResult(result.Error!);
        var recipe = result.Value!;
        return Created($"/api/v1/admin/catalog/recipes/{recipe.Id}",
            new CatalogRecipeCreateResponse(recipe.Id, recipe.Slug, RowVersionEtag.Format(recipe.RowVersion)));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CatalogRecipeAdminDraftResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await catalogRecipeService.GetAdminDraftAsync(id, cancellationToken);
        if (!result.IsSuccess)
            return result.Error!.Code == "catalog.recipe.not-found"
                ? NotFound()
                : ApplicationProblemDetailsMapper.ToActionResult(result.Error);

        var recipe = result.Value!;
        Response.Headers.ETag = RowVersionEtag.Format(recipe.RowVersion);
        Response.Headers.CacheControl = "no-store";
        return Ok(ToResponse(recipe));
    }

    [HttpPatch("{id:guid}")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(CatalogRecipeWriteResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] CatalogRecipeRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken)
    {
        var result = await catalogRecipeService.UpdateAsync(
            id, ToInput(request), RowVersionEtag.ParseExpectedVersion(ifMatch), GetActorId(), cancellationToken);
        if (!result.IsSuccess) return MapFailure(result.Error!);
        var recipe = result.Value!;
        var etag = RowVersionEtag.Format(recipe.RowVersion);
        Response.Headers.ETag = etag;
        return Ok(new CatalogRecipeWriteResponse(recipe.Id, etag));
    }

    [HttpPost("{id:guid}/publish")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(CatalogRecipePublishResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Publish(
        Guid id,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken)
    {
        var publisherId = GetActorId();
        if (publisherId is null) return Unauthorized();
        var result = await catalogRecipeService.PublishAsync(
            id, RowVersionEtag.ParseExpectedVersion(ifMatch), publisherId.Value, publisherId, cancellationToken);
        if (!result.IsSuccess) return MapFailure(result.Error!);
        var published = result.Value!;
        var etag = RowVersionEtag.Format(published.RowVersion);
        Response.Headers.ETag = etag;
        return Ok(new CatalogRecipePublishResponse(
            published.Id, published.RevisionId, published.Revision, published.PublishedAt, etag));
    }

    [HttpPost("{id:guid}/unpublish")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Unpublish(
        Guid id,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken)
    {
        var result = await catalogRecipeService.UnpublishAsync(
            id, RowVersionEtag.ParseExpectedVersion(ifMatch), GetActorId(), cancellationToken);
        if (!result.IsSuccess) return MapFailure(result.Error!);
        Response.Headers.ETag = RowVersionEtag.Format(result.Value);
        return NoContent();
    }

    private IActionResult MapFailure(Application.Common.Results.ApplicationError error) =>
        error.Code == "catalog.recipe.not-found"
            ? NotFound()
            : ApplicationProblemDetailsMapper.ToActionResult(error);

    private Guid? GetActorId() =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId) ? actorId : null;

    private static CatalogRecipeInput ToInput(CatalogRecipeRequest request) =>
        new(request.Name ?? "", request.Slug ?? "", request.Summary ?? "", request.Markdown ?? "",
            request.Ingredients?.Select(component => new CatalogRecipeComponentInput(component.ItemId, component.Quantity)).ToArray() ?? [],
            request.Outputs?.Select(component => new CatalogRecipeComponentInput(component.ItemId, component.Quantity)).ToArray() ?? []);

    private static CatalogRecipeAdminDraftResponse ToResponse(
        Application.Features.Catalog.Results.CatalogRecipeAdminDraftResult recipe) => new(
        recipe.Id, recipe.Slug, recipe.Name, recipe.Summary, recipe.Markdown, StateName(recipe.State),
        recipe.LatestRevisionNumber,
        recipe.Ingredients.Select(component => new CatalogRecipeDraftComponentResponse(
            component.ItemId, component.ItemName, component.Quantity)).ToArray(),
        recipe.Outputs.Select(component => new CatalogRecipeDraftComponentResponse(
            component.ItemId, component.ItemName, component.Quantity)).ToArray(),
        RowVersionEtag.Format(recipe.RowVersion));

    private static string StateName(CatalogItemState state) => state.ToString().ToLowerInvariant();
}
