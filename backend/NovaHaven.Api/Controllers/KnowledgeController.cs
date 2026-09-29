using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NovaHaven.Api.Contracts.Knowledge;
using NovaHaven.Api.Errors;
using NovaHaven.Application.Features.Knowledge.Services;
using NovaHaven.Domain.Knowledge;

namespace NovaHaven.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/knowledge")]
public sealed class KnowledgeController(KnowledgeService knowledgeService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(KnowledgePageResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] string? kind,
        [FromQuery] string? q,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var result = await knowledgeService.ListPublishedAsync(kind, q, page, pageSize, cancellationToken);
        if (!result.IsSuccess) return MapFailure(result.Error!);
        var pageResult = result.Value!;
        return Ok(new KnowledgePageResponse(pageResult.Items.Select(item => new KnowledgeSummaryResponse(
            item.Id, item.Slug, item.Name, item.Summary, KindName(item.Kind),
            item.Revision, item.PublishedAt, item.PreviewImageUrl)).ToArray(),
            pageResult.Page, pageResult.PageSize, pageResult.Total));
    }

    [HttpGet("{kind}")]
    [ProducesResponseType(typeof(KnowledgePageResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListByKind(
        string kind,
        [FromQuery] string? q,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var result = await knowledgeService.ListPublishedAsync(kind, q, page, pageSize, cancellationToken);
        if (!result.IsSuccess) return MapFailure(result.Error!);
        var pageResult = result.Value!;
        return Ok(new KnowledgePageResponse(pageResult.Items.Select(item => new KnowledgeSummaryResponse(
            item.Id, item.Slug, item.Name, item.Summary, KindName(item.Kind),
            item.Revision, item.PublishedAt, item.PreviewImageUrl)).ToArray(),
            pageResult.Page, pageResult.PageSize, pageResult.Total));
    }

    [HttpGet("{kind}/{slug}")]
    [ProducesResponseType(typeof(KnowledgeDetailResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(string kind, string slug, CancellationToken cancellationToken)
    {
        var result = await knowledgeService.GetPublishedAsync(kind, slug, cancellationToken);
        if (!result.IsSuccess) return MapFailure(result.Error!);
        var entry = result.Value!;
        return Ok(new KnowledgeDetailResponse(
            entry.Id, entry.Slug, entry.Name, entry.Summary, entry.Markdown, KindName(entry.Kind),
            entry.Revision, entry.PublishedAt, entry.Metadata,
            entry.Links.Select(link => new KnowledgePublishedLinkResponse(
                LinkName(link.LinkType), link.Slug, link.Name, link.Type, link.SortOrder)).ToArray()));
    }

    private IActionResult MapFailure(Application.Common.Results.ApplicationError error) =>
        error.Code == "knowledge.kind.invalid"
            ? Problem(statusCode: StatusCodes.Status400BadRequest, title: error.Message)
            : error.Code == "knowledge.entry.not-found"
                ? NotFound()
                : ApplicationProblemDetailsMapper.ToActionResult(error);

    private static string KindName(KnowledgeKind kind) => kind.ToString().ToLowerInvariant();
    private static string LinkName(KnowledgeLinkType kind) => kind.ToString().ToLowerInvariant();
}
