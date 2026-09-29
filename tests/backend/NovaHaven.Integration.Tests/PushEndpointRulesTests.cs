using NovaHaven.Application.Features.Notifications;
using Xunit;

namespace NovaHaven.Integration.Tests;

public sealed class PushEndpointRulesTests
{
    [Theory]
    [InlineData("http://fcm.googleapis.com/fcm/send/abc")]
    [InlineData("https://127.0.0.1/fcm/send/abc")]
    [InlineData("https://attacker.example/fcm/send/abc")]
    [InlineData("https://user@fcm.googleapis.com/fcm/send/abc")]
    public void Rejects_insecure_local_or_untrusted_push_endpoints(string endpoint) =>
        Assert.False(PushEndpointRules.IsSupported(endpoint));

    [Theory]
    [InlineData("https://fcm.googleapis.com/fcm/send/abc")]
    [InlineData("https://updates.push.services.mozilla.com/wpush/v2/abc")]
    [InlineData("https://web.push.apple.com/Qabc")]
    public void Allows_known_public_web_push_services(string endpoint) =>
        Assert.True(PushEndpointRules.IsSupported(endpoint));
}
