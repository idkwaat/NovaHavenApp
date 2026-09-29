using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NovaHaven.Api.Contracts.Integration;
using NovaHaven.Application.Features.Integration.Services;
using NovaHaven.Domain.Integration.Entities;

namespace NovaHaven.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/integrations/status")]
public sealed class IntegrationsController(IntegrationCapabilityService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<IntegrationCapabilityResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var results = await service.ListPublicAsync(cancellationToken);
        return Ok(results.Select(result => new IntegrationCapabilityResponse(
            result.CapabilityKey, result.DisplayName, result.Owner, StatusName(result.Status),
            result.LastCheckedAt, result.LastSuccessAt, result.SafeMessage)).ToArray());
    }

    private static string StatusName(IntegrationStatus status) => status.ToString().ToLowerInvariant();
}
