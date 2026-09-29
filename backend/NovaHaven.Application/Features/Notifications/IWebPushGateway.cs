using NovaHaven.Domain.Notifications.Entities;

namespace NovaHaven.Application.Features.Notifications;

public interface IWebPushGateway
{
    Task<string?> GetPublicKeyAsync(CancellationToken cancellationToken = default);
    Task<PushDeliveryStatus> SendAsync(PushSubscription subscription, string payload, CancellationToken cancellationToken = default);
}

public enum PushDeliveryStatus
{
    Sent,
    Disabled,
    Gone,
    Failed
}
