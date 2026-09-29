using Microsoft.EntityFrameworkCore;
using NovaHaven.Application.Features.Notifications.Repositories;
using NovaHaven.Application.Features.Notifications.Results;
using NovaHaven.Domain.Notifications.Entities;
using NovaHaven.Infrastructure.Data;

namespace NovaHaven.Infrastructure.Persistence.Repositories.Notifications;

public sealed class EfUserNotificationRepository(NovaDbContext dbContext) : IUserNotificationRepository
{
    public Task<int> CountUserNotificationsAsync(Guid userId, CancellationToken cancellationToken) =>
        dbContext.UserNotifications.AsNoTracking().CountAsync(item => item.UserId == userId, cancellationToken);

    public Task<int> CountUnreadAsync(Guid userId, CancellationToken cancellationToken) =>
        dbContext.UserNotifications.AsNoTracking()
            .CountAsync(item => item.UserId == userId && item.ReadAtUtc == null, cancellationToken);

    public async Task<IReadOnlyList<UserNotificationItemResult>> ListUserNotificationsAsync(
        Guid userId, int offset, int pageSize, CancellationToken cancellationToken) =>
        await dbContext.UserNotifications.AsNoTracking().Where(item => item.UserId == userId)
            .OrderByDescending(item => item.CreatedAtUtc).ThenByDescending(item => item.Id)
            .Skip(offset).Take(pageSize)
            .Select(item => new UserNotificationItemResult(item.Id, item.Title, item.Body,
                item.Href, item.CreatedAtUtc, item.ReadAtUtc)).ToArrayAsync(cancellationToken);

    public Task<UserNotification?> FindForUserAsync(
        Guid userId, Guid notificationId, CancellationToken cancellationToken) =>
        dbContext.UserNotifications.SingleOrDefaultAsync(
            item => item.Id == notificationId && item.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<UserNotification>> ListUnreadForUserAsync(
        Guid userId, CancellationToken cancellationToken) =>
        await dbContext.UserNotifications.Where(item => item.UserId == userId && item.ReadAtUtc == null)
            .ToArrayAsync(cancellationToken);

    public Task<PushSubscription?> FindPushByHashAsync(string endpointHash, CancellationToken cancellationToken) =>
        dbContext.PushSubscriptions.SingleOrDefaultAsync(item => item.EndpointHash == endpointHash, cancellationToken);

    public void AddPush(PushSubscription subscription) => dbContext.PushSubscriptions.Add(subscription);

    public async Task DeletePushForUserAsync(Guid userId, string endpoint, CancellationToken cancellationToken)
    {
        await dbContext.PushSubscriptions.Where(item => item.UserId == userId && item.Endpoint == endpoint)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
