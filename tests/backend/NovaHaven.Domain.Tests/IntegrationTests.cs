using NovaHaven.Application.Integration;
using NovaHaven.Domain.Integration;
using NovaHaven.Domain.Integration.Entities;
using Xunit;

namespace NovaHaven.Domain.Tests;

public sealed class IntegrationTests
{
    [Fact]
    public void Capability_cannot_be_marked_healthy_without_configuration()
    {
        Assert.False(IntegrationCapabilityPolicy.CanTransition(IntegrationStatus.Unavailable, IntegrationStatus.Healthy));
        Assert.True(IntegrationCapabilityPolicy.CanTransition(IntegrationStatus.Configured, IntegrationStatus.Healthy));
    }

    [Fact]
    public void Capability_message_is_safe_and_bounded()
    {
        var errors = IntegrationCapabilityPolicy.ValidateSafeMessage("password=super-secret");

        Assert.Contains("message", errors.Keys);
    }
}
