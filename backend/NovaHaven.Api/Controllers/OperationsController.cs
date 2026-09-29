using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NovaHaven.Api.Contracts.Operations;
using NovaHaven.Application.Features.Operations.Results;
using NovaHaven.Application.Features.Operations.Services;

namespace NovaHaven.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/v1/admin/operations")]
public sealed class OperationsController(OperationsService operationsService) : ControllerBase
{
    [HttpGet("diagnostics")]
    [ProducesResponseType(typeof(OperationsDiagnosticsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDiagnostics(CancellationToken cancellationToken)
    {
        var result = await operationsService.GetDiagnosticsAsync(cancellationToken);
        return Ok(ToResponse(result));
    }

    private static OperationsDiagnosticsResponse ToResponse(OperationsDiagnosticsResult result) => new(
        result.GeneratedAt,
        new DatabaseDiagnosticsResponse(
            result.Database.Provider,
            result.Database.CanConnect,
            result.Database.AppliedMigrations,
            result.Database.PendingMigrations,
            result.Database.PendingMigrationNames),
        ToResponse(result.Content),
        result.Integrations.Select(integration => new IntegrationDiagnosticsResponse(
            integration.CapabilityKey,
            integration.Status,
            integration.UpdatedAt)).ToArray());

    private static ContentDiagnosticsResponse ToResponse(ContentDiagnosticsResult result) => new(
        result.WikiArticles,
        result.PublishedWikiArticles,
        result.KnowledgeEntries,
        result.PublishedKnowledgeEntries,
        result.CommunityRecords,
        result.PublishedCommunityRecords,
        result.CatalogItems,
        result.PublishedCatalogItems,
        result.Rewards,
        result.PublishedRewards,
        result.CommerceOffers,
        result.PublishedCommerceOffers,
        result.AuditEvents);
}
