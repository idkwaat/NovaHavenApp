using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NovaHaven.Api.Contracts.News;
using NovaHaven.Api.Errors;
using NovaHaven.Api.Filters;
using NovaHaven.Api.Http;
using NovaHaven.Application.Features.News.Commands;
using NovaHaven.Application.Features.News.Services;

namespace NovaHaven.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[PersistenceConflictExceptionFilter]
[Route("api/v1/admin/news")]
public sealed class AdminNewsController(NewsService newsService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AdminNewsListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await newsService.ListAdminAsync(cancellationToken);
        if (!result.IsSuccess) return ApplicationProblemDetailsMapper.ToActionResult(result.Error!);

        return Ok(result.Value!.Select(post => new AdminNewsListItemResponse(
            post.Id, post.Slug, post.Title, post.State, post.PublishedAt, post.UpdatedAt,
            RowVersionEtag.Format(post.RowVersion))).ToArray());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(NewsCreateResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] NewsPostRequest request, CancellationToken cancellationToken)
    {
        var result = await newsService.CreateAsync(ToInput(request), GetActorId(), cancellationToken);
        if (!result.IsSuccess) return ApplicationProblemDetailsMapper.ToActionResult(result.Error!);

        var post = result.Value!;
        var etag = RowVersionEtag.Format(post.RowVersion);
        return Created($"/api/v1/admin/news/{post.Id}", new NewsCreateResponse(post.Id, post.Slug, etag));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AdminNewsPostResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await newsService.GetAdminAsync(id, cancellationToken);
        if (!result.IsSuccess)
            return result.Error!.Code == "news.post.not-found"
                ? NotFound()
                : ApplicationProblemDetailsMapper.ToActionResult(result.Error);

        var post = result.Value!;
        Response.Headers.ETag = RowVersionEtag.Format(post.RowVersion);
        Response.Headers.CacheControl = "no-store";
        return Ok(new AdminNewsPostResponse(
            post.Id, post.Slug, post.Title, post.Summary, post.Markdown, post.State, post.PublishedAt));
    }

    [HttpPatch("{id:guid}")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(NewsWriteResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] NewsPostRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken)
    {
        var result = await newsService.UpdateAsync(
            id, ToInput(request), RowVersionEtag.ParseExpectedVersion(ifMatch), GetActorId(), cancellationToken);
        if (!result.IsSuccess) return MapFailure(result.Error!);

        var post = result.Value!;
        var etag = RowVersionEtag.Format(post.RowVersion);
        Response.Headers.ETag = etag;
        return Ok(new NewsWriteResponse(post.Id, etag));
    }

    [HttpPost("{id:guid}/publish")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(NewsPublishResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Publish(
        Guid id,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken)
    {
        var actorId = GetActorId();
        if (actorId is null) return Unauthorized();

        var result = await newsService.PublishAsync(
            id, RowVersionEtag.ParseExpectedVersion(ifMatch), actorId.Value, actorId, cancellationToken);
        if (!result.IsSuccess) return MapFailure(result.Error!);

        var post = result.Value!;
        var etag = RowVersionEtag.Format(post.RowVersion);
        Response.Headers.ETag = etag;
        return Ok(new NewsPublishResponse(post.Id, post.PublishedAt, etag));
    }

    [HttpPost("{id:guid}/unpublish")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Unpublish(
        Guid id,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken)
    {
        var result = await newsService.UnpublishAsync(
            id, RowVersionEtag.ParseExpectedVersion(ifMatch), GetActorId(), cancellationToken);
        if (!result.IsSuccess) return MapFailure(result.Error!);
        return NoContent();
    }

    private IActionResult MapFailure(Application.Common.Results.ApplicationError error) =>
        error.Code == "news.post.not-found"
            ? NotFound()
            : ApplicationProblemDetailsMapper.ToActionResult(error);

    private Guid? GetActorId() =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId) ? actorId : null;

    private static NewsPostInput ToInput(NewsPostRequest request) =>
        new(request.Title, request.Slug, request.Summary, request.Markdown);
}
