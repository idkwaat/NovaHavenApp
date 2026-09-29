using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NovaHaven.Api.Contracts.Rewards;
using NovaHaven.Api.Errors;
using NovaHaven.Application.Features.Rewards.Queries;
using NovaHaven.Application.Features.Rewards.Services;
using NovaHaven.Domain.Rewards.Entities;

namespace NovaHaven.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/rewards")]
public sealed class RewardsController(RewardService rewardService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(RewardPageResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery(Name = "q")] string? search,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var result = await rewardService.ListPublishedAsync(new RewardListQuery(search, page, pageSize), cancellationToken);
        if (!result.IsSuccess) return ApplicationProblemDetailsMapper.ToActionResult(result.Error!);
        var pageResult = result.Value!;
        return Ok(new RewardPageResponse(pageResult.Items.Select(item => new RewardListItemResponse(
            item.Id, item.Slug, item.Name, item.Summary, KindName(item.Kind), item.Revision,
            item.ExternalAcknowledgementRequired, item.UpdatedAt)).ToArray(),
            pageResult.Page, pageResult.PageSize, pageResult.Total));
    }

    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(RewardDetailResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(string slug, CancellationToken cancellationToken)
    {
        var result = await rewardService.GetPublishedAsync(slug, cancellationToken);
        if (!result.IsSuccess)
            return result.Error!.Code == "reward.definition.not-found"
                ? NotFound()
                : ApplicationProblemDetailsMapper.ToActionResult(result.Error);
        var reward = result.Value!;
        return Ok(new RewardDetailResponse(
            reward.Id, reward.Slug, reward.Name, reward.Summary, reward.Markdown,
            KindName(reward.Kind), reward.Number, reward.DeliveryDescription,
            reward.ExternalAcknowledgementRequired, reward.PublishedAt, reward.UpdatedAt));
    }

    private static string KindName(RewardDefinitionKind kind) => kind.ToString().ToLowerInvariant();
}
