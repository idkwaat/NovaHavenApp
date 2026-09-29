using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NovaHaven.Api.Contracts.Catalog;
using NovaHaven.Api.Errors;
using NovaHaven.Application.Features.Catalog.Queries;
using NovaHaven.Application.Features.Catalog.Services;
using NovaHaven.Domain.Catalog.Entities;

namespace NovaHaven.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/catalog/items")]
public sealed class CatalogItemsController(CatalogItemService catalogItemService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(CatalogItemPageResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery(Name = "q")] string? search,
        [FromQuery] string? kind,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var result = await catalogItemService.ListPublishedAsync(
            new CatalogItemListQuery(search, kind, page, pageSize), cancellationToken);
        if (!result.IsSuccess) return ApplicationProblemDetailsMapper.ToActionResult(result.Error!);

        var pageResult = result.Value!;
        return Ok(new CatalogItemPageResponse(
            pageResult.Items.Select(item => new CatalogItemListItemResponse(
                item.Id, item.Slug, item.Name, item.Summary, KindName(item.Kind), item.Revision,
                item.PublishedAt, item.UpdatedAt)).ToArray(),
            pageResult.Page, pageResult.PageSize, pageResult.Total));
    }

    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(CatalogItemDetailResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(string slug, CancellationToken cancellationToken)
    {
        var result = await catalogItemService.GetPublishedAsync(slug, cancellationToken);
        if (!result.IsSuccess)
            return result.Error!.Code == "catalog.item.not-found"
                ? NotFound()
                : ApplicationProblemDetailsMapper.ToActionResult(result.Error);

        var item = result.Value!;
        return Ok(new CatalogItemDetailResponse(
            item.Id, item.Slug, item.Name, item.Summary, item.Markdown, KindName(item.Kind),
            item.Revision, item.PublishedAt, item.UpdatedAt));
    }

    private static string KindName(CatalogItemKind kind) => kind.ToString().ToLowerInvariant();
}
