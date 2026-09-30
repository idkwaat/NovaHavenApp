using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NovaHaven.Api.Contracts.Notifications;
using NovaHaven.Api.Errors;
using NovaHaven.Application.Features.Notifications.Services;

namespace NovaHaven.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/v1/admin/notifications")]
public sealed class AdminNotificationsController(UserNotificationService service) : ControllerBase
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendAnnouncement(
        [FromBody] AnnouncementRequest request, CancellationToken cancellationToken)
    {
        var result = await service.SendAnnouncementAsync(request.Title, request.Body, request.Href, cancellationToken);
        if (!result.IsSuccess) return ApplicationProblemDetailsMapper.ToActionResult(result.Error!);
        var delivery = result.Value!;
        return Ok(new AnnouncementDeliveryResponse(
            delivery.RecipientCount, delivery.PushAttemptCount, delivery.PushSentCount));
    }
}
