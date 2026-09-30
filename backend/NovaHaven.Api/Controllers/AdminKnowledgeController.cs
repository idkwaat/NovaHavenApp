using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NovaHaven.Api.Contracts.Knowledge;
using NovaHaven.Api.Errors;
using NovaHaven.Api.Filters;
using NovaHaven.Api.Http;
using NovaHaven.Application.Features.Knowledge.Commands;
using NovaHaven.Application.Features.Knowledge.Services;
using NovaHaven.Application.Knowledge;
using NovaHaven.Domain.Knowledge;

namespace NovaHaven.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[PersistenceConflictExceptionFilter]
[Route("api/v1/admin/knowledge")]
public sealed class AdminKnowledgeController(KnowledgeService knowledgeService) : ControllerBase
{
    [HttpGet("{kind}")]
    [ProducesResponseType(typeof(IReadOnlyList<KnowledgeAdminListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(string kind, CancellationToken cancellationToken)
    {
        var result = await knowledgeService.ListAdminAsync(kind, cancellationToken);
        if (!result.IsSuccess) return MapFailure(result.Error!);
        return Ok(result.Value!.Select(item => new KnowledgeAdminListItemResponse(
            item.Id, item.Slug, item.Name, KindName(item.Kind), StateName(item.State),
            item.LatestRevisionNumber, item.UpdatedAt, RowVersionEtag.Format(item.RowVersion))).ToArray());
    }

    [HttpPost("{kind}")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(KnowledgeCreateResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        string kind, [FromBody] KnowledgeRequest request, CancellationToken cancellationToken)
    {
        var result = await knowledgeService.CreateAsync(kind, ToCommand(request), GetActorId(), cancellationToken);
        if (!result.IsSuccess) return MapFailure(result.Error!);
        var entry = result.Value!;
        var etag = RowVersionEtag.Format(entry.RowVersion);
        return Created($"/api/v1/admin/knowledge/{kind}/{entry.Id}",
            new KnowledgeCreateResponse(entry.Id, entry.Slug, etag));
    }

    [HttpGet("{kind}/{id:guid}")]
    [ProducesResponseType(typeof(KnowledgeAdminDraftResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(string kind, Guid id, CancellationToken cancellationToken)
    {
        var result = await knowledgeService.GetAdminAsync(kind, id, cancellationToken);
        if (!result.IsSuccess) return MapFailure(result.Error!);
        var entry = result.Value!;
        Response.Headers.ETag = RowVersionEtag.Format(entry.RowVersion);
        Response.Headers.CacheControl = "no-store";
        return Ok(new KnowledgeAdminDraftResponse(
            entry.Id, entry.Slug, entry.Name, entry.Summary, entry.Markdown,
            KindName(entry.Kind), StateName(entry.State), entry.LatestRevisionNumber, entry.Metadata,
            entry.Links.Select(link => new KnowledgeDraftLinkResponse(
                LinkName(link.LinkType), link.TargetEntryId, link.TargetCatalogItemId, link.SortOrder)).ToArray()));
    }

    [HttpPatch("{kind}/{id:guid}")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(KnowledgeWriteResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(
        string kind,
        Guid id,
        [FromBody] KnowledgeRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken)
    {
        var result = await knowledgeService.UpdateAsync(
            kind, id, ToCommand(request), RowVersionEtag.ParseExpectedVersion(ifMatch), GetActorId(), cancellationToken);
        if (!result.IsSuccess) return MapFailure(result.Error!);
        var entry = result.Value!;
        var etag = RowVersionEtag.Format(entry.RowVersion);
        Response.Headers.ETag = etag;
        return Ok(new KnowledgeWriteResponse(entry.Id, etag));
    }

    [HttpPost("{kind}/{id:guid}/publish")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(KnowledgePublishResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Publish(
        string kind,
        Guid id,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken)
    {
        var publisherId = GetActorId();
        if (publisherId is null) return Unauthorized();
        var result = await knowledgeService.PublishAsync(
            kind, id, RowVersionEtag.ParseExpectedVersion(ifMatch), publisherId, publisherId, cancellationToken);
        if (!result.IsSuccess) return MapFailure(result.Error!);
        var entry = result.Value!;
        var etag = RowVersionEtag.Format(entry.RowVersion);
        Response.Headers.ETag = etag;
        return Ok(new KnowledgePublishResponse(entry.Id, entry.RevisionId, entry.Revision, etag));
    }

    [HttpPost("{kind}/{id:guid}/unpublish")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Unpublish(
        string kind,
        Guid id,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken)
    {
        var result = await knowledgeService.UnpublishAsync(
            kind, id, RowVersionEtag.ParseExpectedVersion(ifMatch), GetActorId(), cancellationToken);
        if (!result.IsSuccess) return MapFailure(result.Error!);
        Response.Headers.ETag = RowVersionEtag.Format(result.Value);
        return NoContent();
    }

    private IActionResult MapFailure(Application.Common.Results.ApplicationError error) =>
        error.Code == "knowledge.kind.invalid"
            ? Problem(statusCode: StatusCodes.Status400BadRequest, title: error.Message)
            : error.Code == "knowledge.entry.not-found"
                ? NotFound()
                : ApplicationProblemDetailsMapper.ToActionResult(error);

    private Guid? GetActorId() =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId) ? actorId : null;

    private static KnowledgeDraftCommand ToCommand(KnowledgeRequest request) => new(
        request.Name, request.Slug, request.Summary, request.Markdown, request.Kind,
        request.Role, request.LocationEntryId, request.PortraitUrl,
        request.Difficulty, request.GiverNpcEntryId, request.RewardDescription,
        request.Steps?.Select(step => new QuestStepInput(step.Position, step.Title ?? "", step.Description ?? "")).ToArray(),
        request.Region, request.LocationType, request.Latitude, request.Longitude, request.MapImageUrl,
        request.StartsAt, request.EndsAt, request.Theme, request.EventDescription,
        request.Links?.Select(link => new KnowledgeLinkInput(
            link.LinkType, link.TargetEntryId, link.TargetCatalogItemId, link.SortOrder)).ToArray());

    private static string KindName(KnowledgeKind kind) => kind.ToString().ToLowerInvariant();
    private static string StateName(KnowledgeState state) => state.ToString().ToLowerInvariant();
    private static string LinkName(KnowledgeLinkType type) => type.ToString().ToLowerInvariant();
}
