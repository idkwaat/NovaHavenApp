namespace NovaHaven.Api.Contracts.Notifications;

public sealed record UserNotificationResponse(
    Guid Id, string Title, string Body, string? Href, DateTimeOffset CreatedAtUtc, DateTimeOffset? ReadAtUtc);

public sealed record UserNotificationPageResponse(
    IReadOnlyList<UserNotificationResponse> Items, int Page, int PageSize, int Total, int UnreadCount);

public sealed record PushKeysRequest(string? P256dh, string? Auth);
public sealed record PushSubscriptionRequest(string? Endpoint, PushKeysRequest? Keys);
public sealed record DeletePushSubscriptionRequest(string? Endpoint);
public sealed record AnnouncementRequest(string? Title, string? Body, string? Href);
public sealed record AnnouncementDeliveryResponse(int RecipientCount, int PushAttemptCount, int PushSentCount);
