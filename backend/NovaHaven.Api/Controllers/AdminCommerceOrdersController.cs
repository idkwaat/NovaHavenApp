using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NovaHaven.Api.Contracts.Commerce;
using NovaHaven.Api.Errors;
using NovaHaven.Application.Features.Commerce.Services;

namespace NovaHaven.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/v1/admin/commerce/orders")]
public sealed class AdminCommerceOrdersController(CommerceOrderService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int? page, [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var result = await service.ListAdminAsync(page, pageSize, cancellationToken);
        if (!result.IsSuccess) return ApplicationProblemDetailsMapper.ToActionResult(result.Error!);
        var history = result.Value!;
        return Ok(new CommerceOrderHistoryPageResponse(history.Items.Select(order =>
            new CommerceOrderHistoryItemResponse(order.OrderNumber, order.CreatedAt, "demoCompleted",
                order.TotalMinorUnits, order.CurrencyCode, "simulated", "localDemo", false, "none",
                order.Items.Select(item => new CommerceOrderLineResponse(item.Slug, item.Name,
                    item.Revision, item.Quantity, item.UnitPriceMinorUnits, item.LineTotalMinorUnits)).ToArray()))
            .ToArray(), history.Page, history.PageSize, history.Total));
    }
}
