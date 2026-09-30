using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NovaHaven.Api.Contracts.Rewards;
using NovaHaven.Api.Errors;
using NovaHaven.Api.Filters;
using NovaHaven.Api.Http;
using NovaHaven.Application.Features.Rewards.Commands;
using NovaHaven.Application.Features.Rewards.Services;
using NovaHaven.Domain.Rewards.Entities;

namespace NovaHaven.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[PersistenceConflictExceptionFilter]
[Route("api/v1/admin/rewards")]
public sealed class AdminRewardsController(RewardService rewardService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RewardAdminListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await rewardService.ListAdminAsync(cancellationToken);
        if (!result.IsSuccess) return ApplicationProblemDetailsMapper.ToActionResult(result.Error!);
        return Ok(result.Value!.Select(item => new RewardAdminListItemResponse(
            item.Id, item.Slug, item.Name, KindName(item.Kind), (int)item.State,
            item.LatestRevisionNumber, item.UpdatedAt, RowVersionEtag.Format(item.RowVersion))).ToArray());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(RewardCreateResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] RewardRequest request, CancellationToken cancellationToken)
    {
        var result = await rewardService.CreateAsync(ToInput(request), GetActorId(), cancellationToken);
        if (!result.IsSuccess) return ApplicationProblemDetailsMapper.ToActionResult(result.Error!);
        var reward = result.Value!;
        return Created($"/api/v1/admin/rewards/{reward.Id}",
            new RewardCreateResponse(reward.Id, reward.Slug, RowVersionEtag.Format(reward.RowVersion), true));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RewardAdminDraftResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await rewardService.GetAdminDraftAsync(id, cancellationToken);
        if (!result.IsSuccess)
            return result.Error!.Code == "reward.definition.not-found"
                ? NotFound()
                : ApplicationProblemDetailsMapper.ToActionResult(result.Error);
        var reward = result.Value!;
        Response.Headers.ETag = RowVersionEtag.Format(reward.RowVersion);
        Response.Headers.CacheControl = "no-store";
        return Ok(new RewardAdminDraftResponse(
            reward.Id, reward.Slug, reward.Name, reward.Summary, reward.Markdown,
            KindName(reward.Kind), reward.DeliveryDescription, true, (int)reward.State,
            reward.LatestRevisionNumber));
    }

    [HttpPatch("{id:guid}")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(RewardWriteResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] RewardRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch, CancellationToken cancellationToken)
    {
        var result = await rewardService.UpdateAsync(
            id, ToInput(request), RowVersionEtag.ParseExpectedVersion(ifMatch), GetActorId(), cancellationToken);
        if (!result.IsSuccess) return MapFailure(result.Error!);
        var reward = result.Value!;
        var etag = RowVersionEtag.Format(reward.RowVersion);
        Response.Headers.ETag = etag;
        return Ok(new RewardWriteResponse(reward.Id, etag));
    }

    [HttpPost("{id:guid}/publish")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(RewardPublishResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Publish(
        Guid id, [FromHeader(Name = "If-Match")] string? ifMatch, CancellationToken cancellationToken)
    {
        var publisherId = GetActorId();
        if (publisherId is null) return Unauthorized();
        var result = await rewardService.PublishAsync(
            id, RowVersionEtag.ParseExpectedVersion(ifMatch), publisherId.Value, publisherId, cancellationToken);
        if (!result.IsSuccess) return MapFailure(result.Error!);
        var reward = result.Value!;
        var etag = RowVersionEtag.Format(reward.RowVersion);
        Response.Headers.ETag = etag;
        return Ok(new RewardPublishResponse(reward.Id, reward.Revision, true, etag));
    }

    [HttpPost("{id:guid}/unpublish")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Unpublish(
        Guid id, [FromHeader(Name = "If-Match")] string? ifMatch, CancellationToken cancellationToken)
    {
        var result = await rewardService.UnpublishAsync(
            id, RowVersionEtag.ParseExpectedVersion(ifMatch), GetActorId(), cancellationToken);
        if (!result.IsSuccess) return MapFailure(result.Error!);
        Response.Headers.ETag = RowVersionEtag.Format(result.Value);
        return NoContent();
    }

    private IActionResult MapFailure(Application.Common.Results.ApplicationError error) =>
        error.Code == "reward.definition.not-found"
            ? NotFound()
            : ApplicationProblemDetailsMapper.ToActionResult(error);
    private Guid? GetActorId() =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId) ? actorId : null;
    private static RewardInput ToInput(RewardRequest request) => new(
        request.Name ?? "", request.Slug ?? "", request.Summary ?? "", request.Markdown ?? "",
        request.Kind ?? "", request.DeliveryDescription ?? "");
    private static string KindName(RewardDefinitionKind kind) => kind.ToString().ToLowerInvariant();
}
