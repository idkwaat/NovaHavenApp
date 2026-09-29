using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NovaHaven.Api.Contracts.Media;
using NovaHaven.Api.Errors;
using NovaHaven.Application.Common.Results;
using NovaHaven.Application.Features.Media;
using NovaHaven.Application.Features.Media.Services;

namespace NovaHaven.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/v1")]
public sealed class MediaController(WikiMediaService wikiMediaService) : ControllerBase
{
    [HttpGet("admin/wiki/media")]
    [ProducesResponseType(typeof(IReadOnlyList<AdminWikiMediaResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAdmin(CancellationToken cancellationToken)
    {
        var result = await wikiMediaService.ListAsync(cancellationToken);
        if (!result.IsSuccess) return ApplicationProblemDetailsMapper.ToActionResult(result.Error!);

        return Ok(result.Value!.Select(media => new AdminWikiMediaResponse(
            media.Id,
            media.OriginalFileName,
            media.ContentType,
            media.Length,
            media.Width,
            media.Height,
            media.CreatedAt,
            $"/api/v1/admin/wiki/media/{media.Id}")).ToArray());
    }

    [HttpPost("admin/wiki/media")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(WikiMediaUploadResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Upload(CancellationToken cancellationToken)
    {
        if (!Request.HasFormContentType)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Multipart form data is required.");

        var form = await Request.ReadFormAsync(cancellationToken);
        var file = form.Files.GetFile("file");
        if (file is null || file.Length == 0)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "An image file is required.");
        if (file.Length > WikiMediaInspector.MaxBytes)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Image must not exceed 5 MB.");

        await using var input = file.OpenReadStream();
        await using var buffer = new MemoryStream(capacity: checked((int)file.Length));
        await input.CopyToAsync(buffer, cancellationToken);
        var result = await wikiMediaService.UploadAsync(
            buffer.ToArray(), file.FileName, file.ContentType, GetActorId(), cancellationToken);
        if (!result.IsSuccess) return MapFailure(result.Error!);

        var media = result.Value!;
        return Created($"/api/v1/admin/wiki/media/{media.Id}", new WikiMediaUploadResponse(
            media.Id,
            media.OriginalFileName,
            media.ContentType,
            media.Length,
            media.Width,
            media.Height,
            $"/api/v1/wiki/media/{media.Id}"));
    }

    [HttpGet("admin/wiki/media/{id:guid}")]
    public async Task<IActionResult> Preview(Guid id, CancellationToken cancellationToken)
    {
        var result = await wikiMediaService.PreviewAsync(id, cancellationToken);
        if (!result.IsSuccess) return MapFailure(result.Error!);
        return File(result.Value!.Content, result.Value.ContentType, enableRangeProcessing: false);
    }

    [AllowAnonymous]
    [HttpGet("wiki/media/{id:guid}")]
    public async Task<IActionResult> ReadPublished(Guid id, CancellationToken cancellationToken)
    {
        var result = await wikiMediaService.ReadPublishedAsync(id, cancellationToken);
        if (!result.IsSuccess) return MapFailure(result.Error!);
        return File(result.Value!.Content, result.Value.ContentType, enableRangeProcessing: false);
    }

    private IActionResult MapFailure(ApplicationError error) =>
        error.Code == "wiki.media.not-found"
            ? NotFound()
            : ApplicationProblemDetailsMapper.ToActionResult(error);

    private Guid? GetActorId() =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId) ? actorId : null;
}
