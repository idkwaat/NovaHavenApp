namespace NovaHaven.Application.Features.Notifications.Results;

public sealed record UserNotificationItemResult(
    Guid Id, string Title, string Body, string? Href, DateTimeOffset CreatedAtUtc, DateTimeOffset? ReadAtUtc);

public sealed record UserNotificationPageResult(
    IReadOnlyList<UserNotificationItemResult> Items, int Page, int PageSize, int Total, int UnreadCount);

public sealed record AnnouncementDeliveryResult(int RecipientCount, int PushAttemptCount, int PushSentCount);
