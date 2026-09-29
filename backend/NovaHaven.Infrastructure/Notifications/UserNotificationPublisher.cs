using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NovaHaven.Application.Features.Notifications;
using NovaHaven.Domain.Notifications.Entities;
using NovaHaven.Infrastructure.Data;

namespace NovaHaven.Infrastructure.Notifications;

public sealed class UserNotificationPublisher(
    NovaDbContext db,
    IWebPushGateway push,
    ILogger<UserNotificationPublisher> logger) : IUserNotificationPublisher
{
    public async Task<NotificationBatch> StageForAllUsersAsync(
        string title, string body, string? href, CancellationToken cancellationToken = default)
    {
        var recipientIds = await db.Users.AsNoTracking().Select(user => user.Id).ToListAsync(cancellationToken);
        var safeTitle = Limit(title, 120);
        var safeBody = Limit(body, 500);
        db.UserNotifications.AddRange(recipientIds.Select(userId =>
            UserNotification.Create(userId, safeTitle, safeBody, href, DateTimeOffset.UtcNow)));
        return new NotificationBatch(recipientIds, safeTitle, safeBody, href);
    }

    public async Task<NotificationDeliveryResult> DeliverPushAsync(
        NotificationBatch batch, CancellationToken cancellationToken = default)
    {
        if (batch.RecipientIds.Count == 0)
            return new NotificationDeliveryResult(0, 0, 0);

        try
        {
            var subscriptions = await db.PushSubscriptions.AsNoTracking()
                .Where(subscription => batch.RecipientIds.Contains(subscription.UserId))
                .ToListAsync(cancellationToken);
            var payload = JsonSerializer.Serialize(new { title = batch.Title, body = batch.Body, href = batch.Href });
            var expiredIds = new List<Guid>();
            var sent = 0;
            foreach (var subscription in subscriptions)
            {
                var status = await push.SendAsync(subscription, payload, cancellationToken);
                if (status == PushDeliveryStatus.Gone) expiredIds.Add(subscription.Id);
                if (status == PushDeliveryStatus.Sent) sent++;
                if (status == PushDeliveryStatus.Failed)
                    logger.LogWarning("Web Push failed for notification recipient {UserId}.", subscription.UserId);
            }

            if (expiredIds.Count > 0)
                await db.PushSubscriptions.Where(subscription => expiredIds.Contains(subscription.Id))
                    .ExecuteDeleteAsync(cancellationToken);

            return new NotificationDeliveryResult(batch.RecipientIds.Count, subscriptions.Count, sent);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Push delivery failed; durable inbox notifications remain available.");
            return new NotificationDeliveryResult(batch.RecipientIds.Count, 0, 0);
        }
    }

    private static string Limit(string value, int maxLength)
    {
        var normalized = value.Trim();
        return normalized.Length <= maxLength ? normalized : normalized[..(maxLength - 1)].TrimEnd() + "…";
    }
}
