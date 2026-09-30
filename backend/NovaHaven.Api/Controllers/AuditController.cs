using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NovaHaven.Api.Contracts.Audit;
using NovaHaven.Api.Errors;
using NovaHaven.Application.Features.Audit.Queries;
using NovaHaven.Application.Features.Audit.Services;

namespace NovaHaven.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/v1/admin/audit")]
public sealed class AuditController(AuditService auditService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(AuditPageResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] string? entityType,
        [FromQuery] Guid? entityId,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var result = await auditService.ListAsync(
            new AuditListQuery(entityType, entityId, page ?? 1, pageSize ?? 50), cancellationToken);
        if (!result.IsSuccess) return ApplicationProblemDetailsMapper.ToActionResult(result.Error!);

        var value = result.Value!;
        return Ok(new AuditPageResponse(
            value.Items.Select(item => new AuditEventResponse(
                item.Id,
                item.ActorUserId,
                item.Action,
                item.EntityType,
                item.EntityId,
                item.DetailsJson,
                item.OccurredAt)).ToArray(),
            value.Page,
            value.PageSize,
            value.Total));
    }
}
