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
public sealed class AdminWikiCategoriesController(WikiCategoryService wikiCategoryService) : ControllerBase
{
    [HttpGet("categories")]
    [ProducesResponseType(typeof(IReadOnlyList<AdminWikiCategoryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await wikiCategoryService.ListAsync(cancellationToken);
        if (!result.IsSuccess) return ApplicationProblemDetailsMapper.ToActionResult(result.Error!);

        return Ok(result.Value!.Select(ToResponse).ToArray());
    }

    [HttpPost("categories")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(AdminWikiCategoryResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        [FromBody] WikiCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await wikiCategoryService.CreateAsync(
            new WikiCategoryInput(request.Name, request.Slug, request.DisplayOrder), cancellationToken);
        if (!result.IsSuccess) return ApplicationProblemDetailsMapper.ToActionResult(result.Error!);

        var category = result.Value!;
        return Created($"/api/v1/admin/wiki/categories/{category.Id}", ToResponse(category));
    }

    [HttpGet("categories/{id:guid}")]
    [ProducesResponseType(typeof(AdminWikiCategoryDetailResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await wikiCategoryService.GetAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            if (result.Error!.Code == "wiki.category.not-found") return NotFound();
            return ApplicationProblemDetailsMapper.ToActionResult(result.Error);
        }

        var category = result.Value!;
        Response.Headers.ETag = RowVersionEtag.Format(category.RowVersion);
        Response.Headers.CacheControl = "no-store";
        return Ok(new AdminWikiCategoryDetailResponse(
            category.Id, category.Name, category.Slug, category.DisplayOrder, category.IsActive));
    }

    [HttpPatch("categories/{id:guid}")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(AdminWikiCategoryResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] WikiCategoryUpdateRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken)
    {
        var result = await wikiCategoryService.UpdateAsync(
            id,
            new WikiCategoryUpdateInput(request.Name, request.Slug, request.DisplayOrder, request.IsActive),
            RowVersionEtag.ParseExpectedVersion(ifMatch),
            cancellationToken);
        if (!result.IsSuccess) return MapFailure(result.Error!);

        var category = result.Value!;
        Response.Headers.ETag = RowVersionEtag.Format(category.RowVersion);
        return Ok(ToResponse(category));
    }

    [HttpDelete("categories/{id:guid}")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken)
    {
        var result = await wikiCategoryService.DeleteAsync(
            id, RowVersionEtag.ParseExpectedVersion(ifMatch), cancellationToken);
        if (!result.IsSuccess) return MapFailure(result.Error!);
        return NoContent();
    }

    private IActionResult MapFailure(ApplicationError error) =>
        error.Code == "wiki.category.not-found"
            ? NotFound()
            : ApplicationProblemDetailsMapper.ToActionResult(error);

    private static AdminWikiCategoryResponse ToResponse(Application.Features.Wiki.Results.WikiCategoryAdminResult category) =>
        new(category.Id, category.Name, category.Slug, category.DisplayOrder, category.IsActive,
            RowVersionEtag.Format(category.RowVersion));
}
