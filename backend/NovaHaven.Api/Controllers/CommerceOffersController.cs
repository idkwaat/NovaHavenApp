using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NovaHaven.Api.Contracts.Commerce;
using NovaHaven.Api.Errors;
using NovaHaven.Application.Features.Commerce.Services;
using NovaHaven.Domain.Commerce.Entities;

namespace NovaHaven.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/commerce/offers")]
public sealed class CommerceOffersController(CommerceOfferService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery(Name = "q")] string? search, [FromQuery] int? page,
        [FromQuery] int? pageSize, CancellationToken cancellationToken)
    {
        var result = await service.ListPublishedAsync(search, page, pageSize, cancellationToken);
        if (!result.IsSuccess) return ApplicationProblemDetailsMapper.ToActionResult(result.Error!);
        var value = result.Value!;
        return Ok(new CommerceOfferPageResponse(value.Items.Select(item => new CommerceOfferSummaryResponse(
            item.Id, item.Slug, item.Name, item.Summary, KindName(item.Kind), item.Revision,
            true, item.IsPurchasable, item.PriceMinorUnits, "VND", item.IsPurchasable ? "localDemo" : null,
            item.PublishedAt)).ToArray(), value.Page, value.PageSize, value.Total));
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Get(string slug, CancellationToken cancellationToken)
    {
        var result = await service.GetPublishedAsync(slug, cancellationToken);
        if (!result.IsSuccess) return result.Error!.Code == "commerce.offer.not-found"
            ? NotFound() : ApplicationProblemDetailsMapper.ToActionResult(result.Error);
        var item = result.Value!;
        return Ok(new CommerceOfferDetailResponse(item.Id, item.Slug, item.Name, item.Summary,
            item.Markdown, KindName(item.Kind), item.Revision, item.DisplayPrice, item.ProviderProductCode,
            item.IsPurchasable, item.PriceMinorUnits, "VND", item.IsPurchasable ? "localDemo" : null,
            true, item.PublishedAt, item.PublishedAt));
    }

    private static string KindName(CommerceOfferKind kind) => kind.ToString().ToLowerInvariant();
}
