using NovaHaven.Application.Features.Notifications.Results;
using NovaHaven.Domain.Notifications.Entities;

namespace NovaHaven.Application.Features.Notifications.Repositories;

public interface IUserNotificationRepository
{
    Task<int> CountUserNotificationsAsync(Guid userId, CancellationToken cancellationToken);
    Task<int> CountUnreadAsync(Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<UserNotificationItemResult>> ListUserNotificationsAsync(
        Guid userId, int offset, int pageSize, CancellationToken cancellationToken);
    Task<UserNotification?> FindForUserAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken);
    Task<IReadOnlyList<UserNotification>> ListUnreadForUserAsync(Guid userId, CancellationToken cancellationToken);
    Task<PushSubscription?> FindPushByHashAsync(string endpointHash, CancellationToken cancellationToken);
    void AddPush(PushSubscription subscription);
    Task DeletePushForUserAsync(Guid userId, string endpoint, CancellationToken cancellationToken);
}
