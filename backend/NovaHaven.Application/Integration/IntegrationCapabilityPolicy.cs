using NovaHaven.Domain.Integration;
using NovaHaven.Domain.Integration.Entities;

namespace NovaHaven.Application.Integration;

public static class IntegrationCapabilityPolicy
{
    public static bool CanTransition(IntegrationStatus current, IntegrationStatus next) => current switch
    {
        IntegrationStatus.Unavailable => next is IntegrationStatus.Unavailable or IntegrationStatus.Configured,
        IntegrationStatus.Configured => next is IntegrationStatus.Configured or IntegrationStatus.Healthy or IntegrationStatus.Stale or IntegrationStatus.Unavailable,
        IntegrationStatus.Healthy => next is IntegrationStatus.Healthy or IntegrationStatus.Stale or IntegrationStatus.Unavailable,
        IntegrationStatus.Stale => next is IntegrationStatus.Stale or IntegrationStatus.Healthy or IntegrationStatus.Unavailable,
        _ => false
    };

    public static Dictionary<string, string[]> ValidateSafeMessage(string? message)
    {
        var errors = new Dictionary<string, string[]>();
        if ((message?.Length ?? 0) > 500) errors["message"] = ["Status message cannot exceed 500 characters."];
        var lower = message?.ToLowerInvariant() ?? "";
        if (lower.Contains("password") || lower.Contains("secret") || lower.Contains("token") || lower.Contains("apikey") || lower.Contains("api-key"))
            errors["message"] = ["Status message must not contain credentials or secret material."];
        return errors;
    }
}
