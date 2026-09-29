using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NovaHaven.Api.Contracts.Commerce;
using NovaHaven.Api.Errors;
using NovaHaven.Application.Commerce;
using NovaHaven.Application.Features.Commerce.Commands;
using NovaHaven.Application.Features.Commerce.Services;

namespace NovaHaven.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/commerce/orders")]
public sealed class CommerceOrdersController(CommerceOrderService service) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(CommerceOrderReceiptResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(CommerceOrderReceiptResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Checkout(
        [FromBody] CommerceCheckoutRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await service.CheckoutAsync(idempotencyKey,
            new CommerceCheckoutCommand(request.Items?.Select(item =>
                new CommerceCheckoutLineInput(item.Slug, item.Quantity)).ToArray()),
            cancellationToken);
        if (!result.IsSuccess) return ApplicationProblemDetailsMapper.ToActionResult(result.Error!);
        var receipt = result.Value!;
        return StatusCode(receipt.IdempotentReplay ? StatusCodes.Status200OK : StatusCodes.Status201Created,
            new CommerceOrderReceiptResponse(receipt.OrderNumber, receipt.CreatedAt, "demoCompleted",
                receipt.TotalMinorUnits, receipt.CurrencyCode, receipt.Items.Select(ToResponse).ToArray(),
                "simulated", "localDemo", false, "none", receipt.IdempotentReplay,
                "Mô phỏng local — không thu tiền thật và không giao vật phẩm hay quyền lợi trong game."));
    }

    private static CommerceOrderLineResponse ToResponse(Application.Features.Commerce.Results.CommerceOrderLineResult item) =>
        new(item.Slug, item.Name, item.Revision, item.Quantity, item.UnitPriceMinorUnits, item.LineTotalMinorUnits);
}
