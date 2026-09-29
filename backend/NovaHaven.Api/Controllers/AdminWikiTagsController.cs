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
public sealed class AdminWikiTagsController(WikiTagService wikiTagService) : ControllerBase
{
    [HttpGet("tags")]
    [ProducesResponseType(typeof(IReadOnlyList<AdminWikiTagResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await wikiTagService.ListAsync(cancellationToken);
        if (!result.IsSuccess) return ApplicationProblemDetailsMapper.ToActionResult(result.Error!);

        return Ok(result.Value!.Select(ToResponse).ToArray());
    }

    [HttpPost("tags")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(AdminWikiTagResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        [FromBody] WikiTagRequest request,
        CancellationToken cancellationToken)
    {
        var result = await wikiTagService.CreateAsync(new WikiTagInput(request.Name, request.Slug), cancellationToken);
        if (!result.IsSuccess) return ApplicationProblemDetailsMapper.ToActionResult(result.Error!);

        var tag = result.Value!;
        return Created($"/api/v1/admin/wiki/tags/{tag.Id}", ToResponse(tag));
    }

    [HttpPatch("tags/{id:guid}")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(AdminWikiTagResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] WikiTagUpdateRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken)
    {
        var result = await wikiTagService.UpdateAsync(
            id,
            new WikiTagUpdateInput(request.Name, request.Slug, request.IsActive),
            RowVersionEtag.ParseExpectedVersion(ifMatch),
            cancellationToken);
        if (!result.IsSuccess) return MapFailure(result.Error!);

        var tag = result.Value!;
        Response.Headers.ETag = RowVersionEtag.Format(tag.RowVersion);
        return Ok(ToResponse(tag));
    }

    [HttpDelete("tags/{id:guid}")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken)
    {
        var result = await wikiTagService.DeleteAsync(
            id, RowVersionEtag.ParseExpectedVersion(ifMatch), cancellationToken);
        if (!result.IsSuccess) return MapFailure(result.Error!);
        return NoContent();
    }

    private IActionResult MapFailure(ApplicationError error) =>
        error.Code == "wiki.tag.not-found"
            ? NotFound()
            : ApplicationProblemDetailsMapper.ToActionResult(error);

    private static AdminWikiTagResponse ToResponse(Application.Features.Wiki.Results.WikiTagAdminResult tag) =>
        new(tag.Id, tag.Name, tag.Slug, tag.IsActive, RowVersionEtag.Format(tag.RowVersion));
}
