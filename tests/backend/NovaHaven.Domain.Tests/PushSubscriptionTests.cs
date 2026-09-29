using NovaHaven.Domain.Notifications.Entities;
using Xunit;

namespace NovaHaven.Domain.Tests;

public sealed class PushSubscriptionTests
{
    [Fact]
    public void Endpoint_fingerprint_is_fixed_width_and_deterministic_for_sql_server_indexing()
    {
        var fingerprint = PushSubscription.FingerprintEndpoint("https://fcm.googleapis.com/fcm/send/a-very-long-endpoint");

        Assert.Equal(64, fingerprint.Length);
        Assert.Matches("^[0-9A-F]{64}$", fingerprint);
        Assert.Equal(fingerprint, PushSubscription.FingerprintEndpoint("https://fcm.googleapis.com/fcm/send/a-very-long-endpoint"));
        Assert.NotEqual(fingerprint, PushSubscription.FingerprintEndpoint("https://fcm.googleapis.com/fcm/send/another-endpoint"));
    }
}
