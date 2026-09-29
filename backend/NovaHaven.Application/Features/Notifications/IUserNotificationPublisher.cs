namespace NovaHaven.Application.Features.Notifications;

public sealed record NotificationBatch(
    IReadOnlyCollection<Guid> RecipientIds,
    string Title,
    string Body,
    string? Href);

public sealed record NotificationDeliveryResult(int RecipientCount, int PushAttemptCount, int PushSentCount);

/// <summary>Stages durable inbox rows in the caller's DbContext save, then sends push after commit.</summary>
public interface IUserNotificationPublisher
{
    Task<NotificationBatch> StageForAllUsersAsync(
        string title, string body, string? href, CancellationToken cancellationToken = default);

    Task<NotificationDeliveryResult> DeliverPushAsync(
        NotificationBatch batch, CancellationToken cancellationToken = default);
}
