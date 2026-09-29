using NovaHaven.Application.Common.Results;
using NovaHaven.Application.Common.Transactions;
using NovaHaven.Application.Features.Notifications.Repositories;
using NovaHaven.Application.Features.Notifications.Results;
using NovaHaven.Domain.Notifications.Entities;

namespace NovaHaven.Application.Features.Notifications.Services;

public sealed class UserNotificationService(
    IUserNotificationRepository repository,
    IUnitOfWork unitOfWork,
    IWebPushGateway push,
    IUserNotificationPublisher publisher)
{
    public async Task<ApplicationResult<UserNotificationPageResult>> ListInboxAsync(
        Guid userId, int? requestedPage, int? requestedPageSize, CancellationToken cancellationToken)
    {
        var page = requestedPage ?? 1;
        var pageSize = requestedPageSize ?? 20;
        if (page < 1 || pageSize is < 1 or > 50)
            return Invalid<UserNotificationPageResult>("Invalid notification pagination.");
        var offset = ((long)page - 1) * pageSize;
        if (offset > int.MaxValue) return Invalid<UserNotificationPageResult>("Notification page is out of range.");
        var total = await repository.CountUserNotificationsAsync(userId, cancellationToken);
        var items = await repository.ListUserNotificationsAsync(userId, (int)offset, pageSize, cancellationToken);
        var unreadCount = await repository.CountUnreadAsync(userId, cancellationToken);
        return Success(new UserNotificationPageResult(items, page, pageSize, total, unreadCount));
    }

    public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken) =>
        repository.CountUnreadAsync(userId, cancellationToken);

    public async Task<ApplicationResult<bool>> MarkReadAsync(
        Guid userId, Guid notificationId, CancellationToken cancellationToken)
    {
        var notification = await repository.FindForUserAsync(userId, notificationId, cancellationToken);
        if (notification is null) return Failure<bool>("notifications.item.not-found", "Notification was not found.");
        notification.MarkRead(DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Success(true);
    }

    public async Task MarkAllReadAsync(Guid userId, CancellationToken cancellationToken)
    {
        var unread = await repository.ListUnreadForUserAsync(userId, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        foreach (var notification in unread) notification.MarkRead(now);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public Task<string?> GetWebPushPublicKeyAsync(CancellationToken cancellationToken) =>
        push.GetPublicKeyAsync(cancellationToken);

    public async Task<ApplicationResult<bool>> SavePushSubscriptionAsync(
        Guid userId, string? endpoint, string? p256dh, string? auth, CancellationToken cancellationToken)
    {
        if (!PushEndpointRules.IsSupported(endpoint) || !IsBase64Url(p256dh, 128) || !IsBase64Url(auth, 64))
            return ValidationFailure<bool>(new Dictionary<string, string[]>
                { ["subscription"] = ["Thông tin nhận thông báo không hợp lệ."] });
        if (await push.GetPublicKeyAsync(cancellationToken) is null)
            return Failure<bool>("notifications.push.unavailable", "Web Push chưa được cấu hình.");
        var endpointHash = PushSubscription.FingerprintEndpoint(endpoint!);
        var existing = await repository.FindPushByHashAsync(endpointHash, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        if (existing is not null)
        {
            if (existing.UserId != userId) return Failure<bool>("notifications.push.conflict", "Push endpoint is already registered to another user.");
            existing.Update(endpoint!, p256dh!, auth!, now);
        }
        else repository.AddPush(PushSubscription.Create(userId, endpoint!, p256dh!, auth!, now));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Success(true);
    }

    public async Task<ApplicationResult<bool>> DeletePushSubscriptionAsync(
        Guid userId, string? endpoint, CancellationToken cancellationToken)
    {
        if (PushEndpointRules.IsSupported(endpoint))
            await repository.DeletePushForUserAsync(userId, endpoint!, cancellationToken);
        return Success(true);
    }

    public async Task<ApplicationResult<AnnouncementDeliveryResult>> SendAnnouncementAsync(
        string? title, string? body, string? href, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 120
            || string.IsNullOrWhiteSpace(body) || body.Trim().Length > 500 || !IsSafeHref(href))
            return ValidationFailure<AnnouncementDeliveryResult>(new Dictionary<string, string[]>
                { ["announcement"] = ["Tiêu đề, nội dung hoặc đường dẫn không hợp lệ."] });
        var batch = await publisher.StageForAllUsersAsync(title.Trim(), body.Trim(), href, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        var delivery = await publisher.DeliverPushAsync(batch, cancellationToken);
        return Success(new AnnouncementDeliveryResult(delivery.RecipientCount, delivery.PushAttemptCount, delivery.PushSentCount));
    }

    private static bool IsBase64Url(string? value, int maxLength) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= maxLength && value.All(character =>
            character is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '-' or '_');

    private static bool IsSafeHref(string? href) => href is null ||
        (href.Length <= 200 && href.StartsWith('/') && !href.StartsWith("//", StringComparison.Ordinal) && !href.Contains('\\'));

    private static ApplicationResult<T> Invalid<T>(string message) =>
        ApplicationResult<T>.Failure(new ApplicationError("validation.failed", message));
    private static ApplicationResult<T> ValidationFailure<T>(Dictionary<string, string[]> errors) =>
        ApplicationResult<T>.Failure(new ApplicationError("validation.failed", "One or more validation errors occurred.", errors));
    private static ApplicationResult<T> Failure<T>(string code, string message) =>
        ApplicationResult<T>.Failure(new ApplicationError(code, message));
    private static ApplicationResult<T> Success<T>(T value) => ApplicationResult<T>.Success(value);
}
