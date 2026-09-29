namespace NovaHaven.Application.Features.Notifications;

public static class PushEndpointRules
{
    public static bool IsSupported(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 2048 ||
            !Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps || uri.Port != 443 ||
            !string.IsNullOrEmpty(uri.UserInfo) || uri.IsLoopback ||
            System.Net.IPAddress.TryParse(uri.Host, out _))
            return false;

        var host = uri.IdnHost;
        return host.Equals("fcm.googleapis.com", StringComparison.OrdinalIgnoreCase) ||
               host.Equals("updates.push.services.mozilla.com", StringComparison.OrdinalIgnoreCase) ||
               host.Equals("push.services.mozilla.com", StringComparison.OrdinalIgnoreCase) ||
               host.Equals("web.push.apple.com", StringComparison.OrdinalIgnoreCase) ||
               host.EndsWith(".push.apple.com", StringComparison.OrdinalIgnoreCase);
    }
}
