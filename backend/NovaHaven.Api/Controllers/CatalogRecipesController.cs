using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NovaHaven.Api.Contracts.Catalog;
using NovaHaven.Api.Errors;
using NovaHaven.Application.Features.Catalog.Queries;
using NovaHaven.Application.Features.Catalog.Services;

namespace NovaHaven.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/catalog/recipes")]
public sealed class CatalogRecipesController(CatalogRecipeService catalogRecipeService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(CatalogRecipePageResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery(Name = "q")] string? search,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var result = await catalogRecipeService.ListPublishedAsync(
            new CatalogRecipeListQuery(search, page, pageSize), cancellationToken);
        if (!result.IsSuccess) return ApplicationProblemDetailsMapper.ToActionResult(result.Error!);

        var pageResult = result.Value!;
        return Ok(new CatalogRecipePageResponse(
            pageResult.Items.Select(recipe => new CatalogRecipeListItemResponse(
                recipe.Id, recipe.Slug, recipe.Name, recipe.Summary,
                recipe.Revision, recipe.PublishedAt, recipe.UpdatedAt)).ToArray(),
            pageResult.Page, pageResult.PageSize, pageResult.Total));
    }

    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(CatalogRecipeDetailResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(string slug, CancellationToken cancellationToken)
    {
        var result = await catalogRecipeService.GetPublishedAsync(slug, cancellationToken);
        if (!result.IsSuccess)
            return result.Error!.Code == "catalog.recipe.not-found"
                ? NotFound()
                : ApplicationProblemDetailsMapper.ToActionResult(result.Error);

        var recipe = result.Value!;
        return Ok(new CatalogRecipeDetailResponse(
            recipe.Id, recipe.Slug, recipe.Name, recipe.Summary, recipe.Markdown,
            recipe.Revision, recipe.PublishedAt,
            recipe.Ingredients.Select(component => new CatalogRecipeComponentResponse(
                component.ItemId, component.ItemSlug, component.ItemName, component.Quantity)).ToArray(),
            recipe.Outputs.Select(component => new CatalogRecipeComponentResponse(
                component.ItemId, component.ItemSlug, component.ItemName, component.Quantity)).ToArray()));
    }
}
