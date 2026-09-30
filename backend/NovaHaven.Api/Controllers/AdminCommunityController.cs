using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NovaHaven.Api.Contracts.Community;
using NovaHaven.Api.Errors;
using NovaHaven.Api.Filters;
using NovaHaven.Api.Http;
using NovaHaven.Application.Community;
using NovaHaven.Application.Features.Community.Commands;
using NovaHaven.Application.Features.Community.Services;
using NovaHaven.Domain.Community;

namespace NovaHaven.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[PersistenceConflictExceptionFilter]
[Route("api/v1/admin/community")]
public sealed class AdminCommunityController(CommunityService service) : ControllerBase
{
    [HttpGet("{kind}")]
    public async Task<IActionResult> List(string kind, CancellationToken cancellationToken)
    {
        var result = await service.ListAdminAsync(kind, cancellationToken);
        if (!result.IsSuccess) return MapFailure(result.Error!);
        return Ok(result.Value!.Select(item => new CommunityAdminListItemResponse(
            item.Id, item.Slug, item.Name, KindName(item.Kind), StateName(item.State),
            item.LatestRevisionNumber, item.UpdatedAt, RowVersionEtag.Format(item.RowVersion))).ToArray());
    }

    [HttpPost("{kind}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string kind, [FromBody] CommunityRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(kind, ToCommand(request), GetActorId(), cancellationToken);
        if (!result.IsSuccess) return MapFailure(result.Error!);
        var item = result.Value!;
        var etag = RowVersionEtag.Format(item.RowVersion);
        return Created($"/api/v1/admin/community/{kind}/{item.Id}", new CommunityCreateResponse(item.Id, item.Slug, etag));
    }

    [HttpGet("{kind}/{id:guid}")]
    public async Task<IActionResult> Get(string kind, Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetAdminAsync(kind, id, cancellationToken);
        if (!result.IsSuccess) return MapFailure(result.Error!);
        var item = result.Value!;
        var etag = RowVersionEtag.Format(item.RowVersion);
        Response.Headers.ETag = etag;
        Response.Headers.CacheControl = "no-store";
        return Ok(new CommunityAdminDraftResponse(item.Id, item.Slug, item.Name, item.Summary,
            item.Markdown, KindName(item.Kind), StateName(item.State), item.LatestRevisionNumber, item.Metadata));
    }

    [HttpPatch("{kind}/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        string kind, Guid id, [FromBody] CommunityRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch, CancellationToken cancellationToken)
    {
        var result = await service.UpdateAsync(kind, id, ToCommand(request),
            RowVersionEtag.ParseExpectedVersion(ifMatch), GetActorId(), cancellationToken);
        if (!result.IsSuccess) return MapFailure(result.Error!);
        var item = result.Value!;
        var etag = RowVersionEtag.Format(item.RowVersion);
        Response.Headers.ETag = etag;
        return Ok(new CommunityWriteResponse(item.Id, etag));
    }

    [HttpPost("{kind}/{id:guid}/publish")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(
        string kind, Guid id, [FromHeader(Name = "If-Match")] string? ifMatch, CancellationToken cancellationToken)
    {
        var actorId = GetActorId();
        if (actorId is null) return Unauthorized();
        var result = await service.PublishAsync(kind, id, RowVersionEtag.ParseExpectedVersion(ifMatch),
            actorId, actorId, cancellationToken);
        if (!result.IsSuccess) return MapFailure(result.Error!);
        var item = result.Value!;
        var etag = RowVersionEtag.Format(item.RowVersion);
        Response.Headers.ETag = etag;
        return Ok(new CommunityPublishResponse(item.Id, item.RevisionId, item.Revision, etag));
    }

    [HttpPost("{kind}/{id:guid}/unpublish")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unpublish(
        string kind, Guid id, [FromHeader(Name = "If-Match")] string? ifMatch, CancellationToken cancellationToken)
    {
        var result = await service.UnpublishAsync(kind, id, RowVersionEtag.ParseExpectedVersion(ifMatch),
            GetActorId(), cancellationToken);
        if (!result.IsSuccess) return MapFailure(result.Error!);
        Response.Headers.ETag = RowVersionEtag.Format(result.Value);
        return NoContent();
    }

    [HttpGet("event/{id:guid}/registrations")]
    public async Task<IActionResult> ListRegistrations(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.ListRegistrationsAsync(id, cancellationToken);
        if (!result.IsSuccess) return MapFailure(result.Error!);
        return Ok(result.Value!.Select(item => new CommunityRegistrationResponse(item.Id,
            item.DisplayName, item.Contact, RegistrationStateName(item.State), item.CreatedAt,
            RowVersionEtag.Format(item.RowVersion))).ToArray());
    }

    [HttpPost("event/{id:guid}/registrations")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddRegistration(
        Guid id, [FromBody] CommunityRegistrationRequest request, CancellationToken cancellationToken)
    {
        var result = await service.AddRegistrationAsync(id,
            new CommunityRegistrationCommand(request.DisplayName, request.Contact), GetActorId(), cancellationToken);
        if (!result.IsSuccess) return MapFailure(result.Error!);
        var item = result.Value!;
        var etag = RowVersionEtag.Format(item.RowVersion);
        return Created($"/api/v1/admin/community/event/{id}/registrations",
            new CommunityRegistrationCreateResponse(item.Id, item.DisplayName, item.Contact,
                RegistrationStateName(item.State), etag));
    }

    [HttpPatch("event/{id:guid}/registrations/{registrationId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRegistration(
        Guid id, Guid registrationId, [FromBody] CommunityRegistrationUpdateRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch, CancellationToken cancellationToken)
    {
        var result = await service.UpdateRegistrationAsync(id, registrationId,
            new CommunityRegistrationUpdateCommand(request.State), RowVersionEtag.ParseExpectedVersion(ifMatch),
            GetActorId(), cancellationToken);
        if (!result.IsSuccess) return MapFailure(result.Error!);
        var item = result.Value!;
        var etag = RowVersionEtag.Format(item.RowVersion);
        Response.Headers.ETag = etag;
        return Ok(new CommunityRegistrationUpdateResponse(item.Id, RegistrationStateName(item.State), etag));
    }

    private IActionResult MapFailure(Application.Common.Results.ApplicationError error) =>
        error.Code is "community.kind.invalid" ? Problem(statusCode: StatusCodes.Status400BadRequest, title: error.Message)
        : error.Code is "community.record.not-found" or "community.event.not-found" or "community.registration.not-found"
            ? NotFound()
            : ApplicationProblemDetailsMapper.ToActionResult(error);

    private Guid? GetActorId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    private static CommunityDraftCommand ToCommand(CommunityRequest request) => new(
        request.Name, request.Slug, request.Summary, request.Markdown, request.Kind,
        request.StartsAt, request.EndsAt, request.LocationEntryId, request.Capacity, request.RegistrationOpen,
        request.Motto, request.DiscordUrl, request.Handle, request.Bio, request.AvatarUrl,
        request.OwnerDisplayName, request.GalleryMarkdown, request.LeaderboardCategory, request.SeasonEntryId,
        request.Rows?.Select(row => new LeaderboardRowInput(row.Rank, row.ParticipantName ?? "", row.Score, row.Note ?? "")).ToArray());

    private static string KindName(CommunityKind kind) => kind.ToString().ToLowerInvariant();
    private static string StateName(CommunityState state) => state.ToString().ToLowerInvariant();
    private static string RegistrationStateName(EventRegistrationState state) => state.ToString().ToLowerInvariant();
}
