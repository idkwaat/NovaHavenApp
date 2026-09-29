using System.Net;
using Microsoft.Extensions.Logging;
using NovaHaven.Application.Features.Notifications;
using NovaHaven.Domain.Notifications.Entities;
using WebPush;
using DomainPushSubscription = NovaHaven.Domain.Notifications.Entities.PushSubscription;

namespace NovaHaven.Infrastructure.Notifications;

public sealed class WebPushGateway(IVapidKeyProvider keys, WebPushClient client, ILogger<WebPushGateway> logger) : IWebPushGateway
{
    public async Task<string?> GetPublicKeyAsync(CancellationToken cancellationToken = default) =>
        (await keys.GetAsync(cancellationToken))?.PublicKey;

    public async Task<PushDeliveryStatus> SendAsync(DomainPushSubscription subscription, string payload, CancellationToken cancellationToken = default)
    {
        if (!PushEndpointRules.IsSupported(subscription.Endpoint)) return PushDeliveryStatus.Gone;
        var vapid = await keys.GetAsync(cancellationToken);
        if (vapid is null) return PushDeliveryStatus.Disabled;

        try
        {
            var browserSubscription = new WebPush.PushSubscription(subscription.Endpoint, subscription.P256dh, subscription.Auth);
            await client.SendNotificationAsync(browserSubscription, payload, vapid, cancellationToken);
            return PushDeliveryStatus.Sent;
        }
        catch (WebPushException exception) when (exception.StatusCode is HttpStatusCode.Gone or HttpStatusCode.NotFound)
        {
            return PushDeliveryStatus.Gone;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Web Push delivery failed for subscription {SubscriptionId}.", subscription.Id);
            return PushDeliveryStatus.Failed;
        }
    }
}
