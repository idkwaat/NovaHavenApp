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
[Route("api/v1/admin/catalog/items")]
public sealed class AdminCatalogItemsController(CatalogItemService catalogItemService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CatalogItemAdminListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await catalogItemService.ListAdminAsync(cancellationToken);
        if (!result.IsSuccess) return ApplicationProblemDetailsMapper.ToActionResult(result.Error!);
        return Ok(result.Value!.Select(item => new CatalogItemAdminListItemResponse(
            item.Id, item.Slug, item.Name, KindName(item.Kind), StateName(item.State),
            item.LatestRevisionNumber, item.UpdatedAt, RowVersionEtag.Format(item.RowVersion))).ToArray());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(CatalogItemCreateResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CatalogItemRequest request, CancellationToken cancellationToken)
    {
        var result = await catalogItemService.CreateAsync(ToInput(request), GetActorId(), cancellationToken);
        if (!result.IsSuccess) return ApplicationProblemDetailsMapper.ToActionResult(result.Error!);
        var item = result.Value!;
        return Created($"/api/v1/admin/catalog/items/{item.Id}",
            new CatalogItemCreateResponse(item.Id, item.Slug, RowVersionEtag.Format(item.RowVersion)));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CatalogItemAdminDraftResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await catalogItemService.GetAdminDraftAsync(id, cancellationToken);
        if (!result.IsSuccess)
            return result.Error!.Code == "catalog.item.not-found"
                ? NotFound()
                : ApplicationProblemDetailsMapper.ToActionResult(result.Error);

        var item = result.Value!;
        Response.Headers.ETag = RowVersionEtag.Format(item.RowVersion);
        Response.Headers.CacheControl = "no-store";
        return Ok(new CatalogItemAdminDraftResponse(
            item.Id, item.Slug, item.Name, item.Summary, item.Markdown,
            KindName(item.Kind), StateName(item.State), item.LatestRevisionNumber,
            item.PublishedRevision is null ? null : new CatalogItemRevisionResponse(
                item.PublishedRevision.Id, item.PublishedRevision.Number, item.PublishedRevision.Name,
                item.PublishedRevision.Summary, item.PublishedRevision.Markdown,
                KindName(item.PublishedRevision.Kind), item.PublishedRevision.PublishedAt),
            RowVersionEtag.Format(item.RowVersion)));
    }

    [HttpPatch("{id:guid}")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(CatalogItemWriteResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] CatalogItemRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken)
    {
        var result = await catalogItemService.UpdateAsync(
            id, ToInput(request), RowVersionEtag.ParseExpectedVersion(ifMatch), GetActorId(), cancellationToken);
        if (!result.IsSuccess) return MapFailure(result.Error!);
        var item = result.Value!;
        var etag = RowVersionEtag.Format(item.RowVersion);
        Response.Headers.ETag = etag;
        return Ok(new CatalogItemWriteResponse(item.Id, etag));
    }

    [HttpPost("{id:guid}/publish")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(CatalogItemPublishResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Publish(
        Guid id,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken)
    {
        var publisherId = GetActorId();
        if (publisherId is null) return Unauthorized();
        var result = await catalogItemService.PublishAsync(
            id, RowVersionEtag.ParseExpectedVersion(ifMatch), publisherId.Value, publisherId, cancellationToken);
        if (!result.IsSuccess) return MapFailure(result.Error!);
        var published = result.Value!;
        var etag = RowVersionEtag.Format(published.RowVersion);
        Response.Headers.ETag = etag;
        return Ok(new CatalogItemPublishResponse(
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
        var result = await catalogItemService.UnpublishAsync(
            id, RowVersionEtag.ParseExpectedVersion(ifMatch), GetActorId(), cancellationToken);
        if (!result.IsSuccess) return MapFailure(result.Error!);
        Response.Headers.ETag = RowVersionEtag.Format(result.Value);
        return NoContent();
    }

    private IActionResult MapFailure(Application.Common.Results.ApplicationError error) =>
        error.Code == "catalog.item.not-found"
            ? NotFound()
            : ApplicationProblemDetailsMapper.ToActionResult(error);

    private Guid? GetActorId() =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId) ? actorId : null;

    private static CatalogItemDraftInput ToInput(CatalogItemRequest request) =>
        new(request.Name, request.Slug, request.Summary, request.Markdown, request.Kind);

    private static string KindName(CatalogItemKind kind) => kind.ToString().ToLowerInvariant();
    private static string StateName(CatalogItemState state) => state.ToString().ToLowerInvariant();
}
