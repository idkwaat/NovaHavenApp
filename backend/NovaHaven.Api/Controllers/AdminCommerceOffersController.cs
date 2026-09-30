using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NovaHaven.Api.Contracts.Commerce;
using NovaHaven.Api.Errors;
using NovaHaven.Api.Filters;
using NovaHaven.Api.Http;
using NovaHaven.Application.Features.Commerce.Commands;
using NovaHaven.Application.Features.Commerce.Services;
using NovaHaven.Domain.Commerce.Entities;

namespace NovaHaven.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[PersistenceConflictExceptionFilter]
[Route("api/v1/admin/commerce/offers")]
public sealed class AdminCommerceOffersController(CommerceOfferService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var results = await service.ListAdminAsync(cancellationToken);
        return Ok(results.Select(item => new CommerceOfferAdminListResponse(item.Id, item.Slug,
            item.Name, KindName(item.Kind), item.State, item.LatestRevisionNumber, item.UpdatedAt,
            true, item.IsPurchasable, item.PriceMinorUnits, RowVersionEtag.Format(item.RowVersion))).ToArray());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] CommerceOfferRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(ToCommand(request), GetActorId(), cancellationToken);
        if (!result.IsSuccess) return MapFailure(result.Error!);
        var item = result.Value!;
        return Created($"/api/v1/admin/commerce/offers/{item.Id}",
            new CommerceOfferCreateResponse(item.Id, item.Slug, true, RowVersionEtag.Format(item.RowVersion)));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetAdminAsync(id, cancellationToken);
        if (!result.IsSuccess) return MapFailure(result.Error!);
        var item = result.Value!;
        Response.Headers.ETag = RowVersionEtag.Format(item.RowVersion);
        Response.Headers.CacheControl = "no-store";
        return Ok(new CommerceOfferAdminDraftResponse(item.Id, item.Slug, item.Name, item.Summary,
            item.Markdown, KindName(item.Kind), item.DisplayPrice, item.ProviderProductCode,
            item.IsPurchasable, item.PriceMinorUnits, "VND", true, item.State, item.LatestRevisionNumber));
    }

    [HttpPatch("{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(Guid id, [FromBody] CommerceOfferRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch, CancellationToken cancellationToken)
    {
        var result = await service.UpdateAsync(id, ToCommand(request), RowVersionEtag.ParseExpectedVersion(ifMatch),
            GetActorId(), cancellationToken);
        if (!result.IsSuccess) return MapFailure(result.Error!);
        var item = result.Value!;
        var etag = RowVersionEtag.Format(item.RowVersion);
        Response.Headers.ETag = etag;
        return Ok(new CommerceOfferWriteResponse(item.Id, true, etag));
    }

    [HttpPost("{id:guid}/publish")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(Guid id, [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken)
    {
        var actorId = GetActorId();
        if (actorId is null) return Unauthorized();
        var result = await service.PublishAsync(id, RowVersionEtag.ParseExpectedVersion(ifMatch),
            actorId, actorId, cancellationToken);
        if (!result.IsSuccess) return MapFailure(result.Error!);
        var item = result.Value!;
        var etag = RowVersionEtag.Format(item.RowVersion);
        Response.Headers.ETag = etag;
        return Ok(new CommerceOfferPublishResponse(item.Id, item.Revision, true, etag));
    }

    [HttpPost("{id:guid}/unpublish")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unpublish(Guid id, [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken)
    {
        var result = await service.UnpublishAsync(id, RowVersionEtag.ParseExpectedVersion(ifMatch), GetActorId(), cancellationToken);
        if (!result.IsSuccess) return MapFailure(result.Error!);
        Response.Headers.ETag = RowVersionEtag.Format(result.Value);
        return NoContent();
    }

    private IActionResult MapFailure(Application.Common.Results.ApplicationError error) =>
        error.Code == "commerce.offer.not-found" ? NotFound() : ApplicationProblemDetailsMapper.ToActionResult(error);

    private Guid? GetActorId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    private static CommerceOfferCommand ToCommand(CommerceOfferRequest request) => new(
        request.Name, request.Slug, request.Summary, request.Markdown, request.Kind,
        request.DisplayPrice, request.ProviderProductCode, request.IsPurchasable, request.PriceMinorUnits);

    private static string KindName(CommerceOfferKind kind) => kind.ToString().ToLowerInvariant();
}
