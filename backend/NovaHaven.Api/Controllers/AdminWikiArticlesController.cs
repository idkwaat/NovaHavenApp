using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NovaHaven.Api.Contracts.Wiki;
using NovaHaven.Api.Errors;
using NovaHaven.Api.Filters;
using NovaHaven.Api.Http;
using NovaHaven.Application.Common.Results;
using NovaHaven.Application.Features.Wiki.Services;
using NovaHaven.Application.Wiki;

namespace NovaHaven.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[PersistenceConflictExceptionFilter]
[Route("api/v1/admin/wiki")]
public sealed class AdminWikiArticlesController(WikiArticleService wikiArticleService) : ControllerBase
{
    [HttpGet("articles")]
    [ProducesResponseType(typeof(IReadOnlyList<AdminWikiArticleListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await wikiArticleService.ListAsync(cancellationToken);
        if (!result.IsSuccess) return ApplicationProblemDetailsMapper.ToActionResult(result.Error!);
        return Ok(result.Value!.Select(article => new AdminWikiArticleListItemResponse(
            article.Id, article.Slug, article.DraftTitle, (int)article.State, article.UpdatedAt,
            article.LatestRevisionNumber)).ToArray());
    }

    [HttpPost("articles")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(AdminWikiArticleCreateResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] WikiDraftRequest request, CancellationToken cancellationToken)
    {
        var result = await wikiArticleService.CreateAsync(ToInput(request), GetActorId(), cancellationToken);
        if (!result.IsSuccess) return MapFailure(result.Error!);

        var article = result.Value!;
        var etag = RowVersionEtag.Format(article.RowVersion);
        return Created($"/api/v1/admin/wiki/articles/{article.Id}",
            new AdminWikiArticleCreateResponse(article.Id, article.Slug, etag));
    }

    [HttpGet("articles/{id:guid}")]
    [ProducesResponseType(typeof(AdminWikiArticleDraftResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await wikiArticleService.GetDraftAsync(id, cancellationToken);
        if (!result.IsSuccess) return MapFailure(result.Error!);

        var article = result.Value!;
        Response.Headers.ETag = RowVersionEtag.Format(article.RowVersion);
        Response.Headers.CacheControl = "no-store";
        return Ok(new AdminWikiArticleDraftResponse(
            article.Id, article.Slug, article.Title, article.Summary, article.Markdown, article.CategoryId,
            article.TagIds, (int)article.State, article.MediaIds, article.LatestRevisionNumber,
            article.PublishedRevisionId));
    }

    [HttpPatch("articles/{id:guid}")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(AdminWikiArticleWriteResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] WikiDraftRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken)
    {
        var result = await wikiArticleService.UpdateAsync(
            id, ToInput(request), RowVersionEtag.ParseExpectedVersion(ifMatch), GetActorId(), cancellationToken);
        if (!result.IsSuccess) return MapFailure(result.Error!);

        var article = result.Value!;
        var etag = RowVersionEtag.Format(article.RowVersion);
        Response.Headers.ETag = etag;
        return Ok(new AdminWikiArticleWriteResponse(article.Id, etag));
    }

    [HttpPost("articles/{id:guid}/publish")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(AdminWikiArticlePublishResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Publish(
        Guid id,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken)
    {
        var actorId = GetActorId();
        if (actorId is null) return Unauthorized();

        var result = await wikiArticleService.PublishAsync(
            id, RowVersionEtag.ParseExpectedVersion(ifMatch), actorId.Value, actorId, cancellationToken);
        if (!result.IsSuccess) return MapFailure(result.Error!);

        var article = result.Value!;
        Response.Headers.ETag = RowVersionEtag.Format(article.RowVersion);
        return Ok(new AdminWikiArticlePublishResponse(article.Id, article.Slug, article.RevisionId,
            article.RevisionNumber));
    }

    [HttpPost("articles/{id:guid}/unpublish")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Unpublish(
        Guid id,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken)
    {
        var result = await wikiArticleService.UnpublishAsync(
            id, RowVersionEtag.ParseExpectedVersion(ifMatch), GetActorId(), cancellationToken);
        if (!result.IsSuccess) return MapFailure(result.Error!);
        return NoContent();
    }

    [HttpGet("articles/{id:guid}/revisions")]
    [ProducesResponseType(typeof(IReadOnlyList<AdminWikiArticleRevisionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListRevisions(Guid id, CancellationToken cancellationToken)
    {
        var result = await wikiArticleService.ListRevisionsAsync(id, cancellationToken);
        if (!result.IsSuccess) return MapFailure(result.Error!);
        return Ok(result.Value!.Select(revision => new AdminWikiArticleRevisionResponse(
            revision.Id, revision.Number, revision.Title, revision.PublishedAt, revision.PublishedBy)).ToArray());
    }

    [HttpPost("articles/{id:guid}/revisions/{revisionId:guid}/restore")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(AdminWikiArticleWriteResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Restore(
        Guid id,
        Guid revisionId,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken)
    {
        var result = await wikiArticleService.RestoreAsync(
            id, revisionId, RowVersionEtag.ParseExpectedVersion(ifMatch), GetActorId(), cancellationToken);
        if (!result.IsSuccess) return MapFailure(result.Error!);

        var article = result.Value!;
        var etag = RowVersionEtag.Format(article.RowVersion);
        Response.Headers.ETag = etag;
        return Ok(new AdminWikiArticleWriteResponse(article.Id, etag));
    }

    private IActionResult MapFailure(ApplicationError error) =>
        error.Code == "wiki.article.not-found"
            ? NotFound()
            : ApplicationProblemDetailsMapper.ToActionResult(error);

    private Guid? GetActorId() =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId) ? actorId : null;

    private static WikiDraftInput ToInput(WikiDraftRequest request) =>
        new(request.Title, request.Slug, request.Summary, request.Markdown, request.CategoryId,
            request.TagIds, request.MediaIds);
}
