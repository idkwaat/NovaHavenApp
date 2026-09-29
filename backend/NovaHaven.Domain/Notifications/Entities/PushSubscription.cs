using System.Security.Cryptography;
using System.Text;

namespace NovaHaven.Domain.Notifications.Entities;

public sealed class PushSubscription
{
    private PushSubscription() { }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Endpoint { get; private set; } = string.Empty;
    public string EndpointHash { get; private set; } = string.Empty;
    public string P256dh { get; private set; } = string.Empty;
    public string Auth { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static PushSubscription Create(Guid userId, string endpoint, string p256dh, string auth, DateTimeOffset now)
    {
        if (userId == Guid.Empty) throw new ArgumentException("A user is required.", nameof(userId));
        var subscription = new PushSubscription { Id = Guid.NewGuid(), UserId = userId, CreatedAtUtc = now.ToUniversalTime() };
        subscription.Update(endpoint, p256dh, auth, now);
        return subscription;
    }

    public static string FingerprintEndpoint(string endpoint) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(endpoint)));

    public void Update(string endpoint, string p256dh, string auth, DateTimeOffset now)
    {
        Endpoint = endpoint;
        EndpointHash = FingerprintEndpoint(endpoint);
        P256dh = p256dh;
        Auth = auth;
        UpdatedAtUtc = now.ToUniversalTime();
    }
}
