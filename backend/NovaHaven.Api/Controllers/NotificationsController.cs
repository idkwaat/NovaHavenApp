using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NovaHaven.Api.Contracts.Notifications;
using NovaHaven.Api.Errors;
using NovaHaven.Application.Features.Notifications.Services;

namespace NovaHaven.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/notifications")]
public sealed class NotificationsController(UserNotificationService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int? page, [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await service.ListInboxAsync(userId, page, pageSize, cancellationToken);
        if (!result.IsSuccess) return ApplicationProblemDetailsMapper.ToActionResult(result.Error!);
        Response.Headers.CacheControl = "no-store";
        var inbox = result.Value!;
        return Ok(new UserNotificationPageResponse(inbox.Items.Select(item => new UserNotificationResponse(
            item.Id, item.Title, item.Body, item.Href, item.CreatedAtUtc, item.ReadAtUtc)).ToArray(),
            inbox.Page, inbox.PageSize, inbox.Total, inbox.UnreadCount));
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        Response.Headers.CacheControl = "no-store";
        return Ok(new { unreadCount = await service.GetUnreadCountAsync(userId, cancellationToken) });
    }

    [HttpPut("{id:guid}/read")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await service.MarkReadAsync(userId, id, cancellationToken);
        return result.IsSuccess ? NoContent() : MapFailure(result.Error!);
    }

    [HttpPost("read-all")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        await service.MarkAllReadAsync(userId, cancellationToken);
        return NoContent();
    }

    [HttpGet("push/config")]
    public async Task<IActionResult> PushConfiguration(CancellationToken cancellationToken) =>
        Ok(new { publicKey = await service.GetWebPushPublicKeyAsync(cancellationToken) });

    [HttpPut("push/subscriptions")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePushSubscription(
        [FromBody] PushSubscriptionRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await service.SavePushSubscriptionAsync(userId, request.Endpoint,
            request.Keys?.P256dh, request.Keys?.Auth, cancellationToken);
        return result.IsSuccess ? NoContent() : MapFailure(result.Error!);
    }

    [HttpDelete("push/subscriptions")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePushSubscription(
        [FromBody] DeletePushSubscriptionRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await service.DeletePushSubscriptionAsync(userId, request.Endpoint, cancellationToken);
        return result.IsSuccess ? NoContent() : MapFailure(result.Error!);
    }

    private IActionResult MapFailure(Application.Common.Results.ApplicationError error) =>
        error.Code == "notifications.item.not-found" ? NotFound() : ApplicationProblemDetailsMapper.ToActionResult(error);

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}
