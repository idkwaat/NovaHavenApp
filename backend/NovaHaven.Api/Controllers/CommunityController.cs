using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NovaHaven.Api.Contracts.Community;
using NovaHaven.Api.Errors;
using NovaHaven.Application.Features.Community.Services;
using NovaHaven.Domain.Community;

namespace NovaHaven.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/community")]
public sealed class CommunityController(CommunityService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(CommunityPageResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] string? kind, [FromQuery] string? q, [FromQuery] int? page,
        [FromQuery] int? pageSize, CancellationToken cancellationToken)
    {
        var result = await service.ListPublishedAsync(kind, q, page, pageSize, cancellationToken);
        if (!result.IsSuccess) return MapFailure(result.Error!);
        return Ok(ToPage(result.Value!));
    }

    [HttpGet("{kind}")]
    [ProducesResponseType(typeof(CommunityPageResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListByKind(
        string kind, [FromQuery] string? q, [FromQuery] int? page,
        [FromQuery] int? pageSize, CancellationToken cancellationToken)
    {
        var result = await service.ListPublishedAsync(kind, q, page, pageSize, cancellationToken);
        if (!result.IsSuccess) return MapFailure(result.Error!);
        return Ok(ToPage(result.Value!));
    }

    [HttpGet("{kind}/{slug}")]
    [ProducesResponseType(typeof(CommunityDetailResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(string kind, string slug, CancellationToken cancellationToken)
    {
        var result = await service.GetPublishedAsync(kind, slug, cancellationToken);
        if (!result.IsSuccess) return MapFailure(result.Error!);
        var item = result.Value!;
        return Ok(new CommunityDetailResponse(item.Id, item.Slug, item.Name, item.Summary,
            item.Markdown, KindName(item.Kind), item.Revision, item.PublishedAt, item.Metadata));
    }

    private IActionResult MapFailure(Application.Common.Results.ApplicationError error) =>
        error.Code == "community.kind.invalid"
            ? Problem(statusCode: StatusCodes.Status400BadRequest, title: error.Message)
            : error.Code == "community.record.not-found" ? NotFound()
            : ApplicationProblemDetailsMapper.ToActionResult(error);

    private static CommunityPageResponse ToPage(Application.Features.Community.Results.CommunityPageResult result) =>
        new(result.Items.Select(item => new CommunitySummaryResponse(item.Id, item.Slug, item.Name,
            item.Summary, KindName(item.Kind), item.Revision, item.PublishedAt)).ToArray(),
            result.Page, result.PageSize, result.Total);

    private static string KindName(CommunityKind kind) => kind.ToString().ToLowerInvariant();
}
