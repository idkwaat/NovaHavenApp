using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NovaHaven.Api.Contracts.News;
using NovaHaven.Api.Errors;
using NovaHaven.Application.Features.News.Queries;
using NovaHaven.Application.Features.News.Services;

namespace NovaHaven.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/news")]
public sealed class NewsController(NewsService newsService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(NewsPageResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] string? q,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var result = await newsService.ListPublishedAsync(new NewsListQuery(q, page, pageSize), cancellationToken);
        if (!result.IsSuccess) return ApplicationProblemDetailsMapper.ToActionResult(result.Error!);

        var pageResult = result.Value!;
        return Ok(new NewsPageResponse(pageResult.Items.Select(ToResponse).ToArray(),
            pageResult.Page, pageResult.PageSize, pageResult.Total));
    }

    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(NewsSummaryResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(string slug, CancellationToken cancellationToken)
    {
        var result = await newsService.GetPublishedAsync(slug, cancellationToken);
        if (!result.IsSuccess)
            return result.Error!.Code == "news.post.not-found"
                ? NotFound()
                : ApplicationProblemDetailsMapper.ToActionResult(result.Error);

        return Ok(ToResponse(result.Value!));
    }

    private static NewsSummaryResponse ToResponse(Application.Features.News.Results.NewsPostResult result) => new(
        result.Id, result.Slug, result.Title, result.Summary, result.Markdown, result.PublishedAt, result.UpdatedAt);
}
