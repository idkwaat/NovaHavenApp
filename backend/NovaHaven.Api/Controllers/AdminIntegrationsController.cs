using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NovaHaven.Api.Contracts.Integration;
using NovaHaven.Api.Filters;
using NovaHaven.Api.Errors;
using NovaHaven.Api.Http;
using NovaHaven.Application.Features.Integration.Services;
using NovaHaven.Domain.Integration.Entities;

namespace NovaHaven.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[PersistenceConflictExceptionFilter]
[Route("api/v1/admin/integrations")]
public sealed class AdminIntegrationsController(IntegrationCapabilityService service) : ControllerBase
{
    [HttpGet("capabilities")]
    [ProducesResponseType(typeof(IReadOnlyList<AdminIntegrationCapabilityResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var results = await service.ListAdminAsync(cancellationToken);
        return Ok(results.Select(result => new AdminIntegrationCapabilityResponse(
            result.Id, result.CapabilityKey, result.DisplayName, result.Owner, StatusName(result.Status),
            result.LastCheckedAt, result.LastSuccessAt, result.SafeMessage, result.UpdatedAt,
            RowVersionEtag.Format(result.RowVersion))).ToArray());
    }

    [HttpPatch("capabilities/{key}")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(IntegrationCapabilityUpdateResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(
        string key, [FromBody] IntegrationCapabilityUpdateRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch, CancellationToken cancellationToken)
    {
        var result = await service.UpdateAsync(key, request.Status, request.SafeMessage,
            RowVersionEtag.ParseExpectedVersion(ifMatch), GetActorId(), cancellationToken);
        if (!result.IsSuccess) return ApplicationProblemDetailsMapper.ToActionResult(result.Error!);
        var capability = result.Value!;
        var etag = RowVersionEtag.Format(capability.RowVersion);
        Response.Headers.ETag = etag;
        return Ok(new IntegrationCapabilityUpdateResponse(
            capability.CapabilityKey, StatusName(capability.Status), capability.SafeMessage, etag));
    }

    private Guid? GetActorId() =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId) ? actorId : null;

    private static string StatusName(IntegrationStatus status) => status.ToString().ToLowerInvariant();
}
