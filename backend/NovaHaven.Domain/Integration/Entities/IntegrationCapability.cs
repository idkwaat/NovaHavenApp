namespace NovaHaven.Domain.Integration.Entities;

public enum IntegrationStatus
{
    Unavailable,
    Configured,
    Healthy,
    Stale
}

public sealed class IntegrationCapability
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CapabilityKey { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Owner { get; set; } = "";
    public IntegrationStatus Status { get; set; } = IntegrationStatus.Unavailable;
    public DateTimeOffset? LastCheckedAt { get; set; }
    public DateTimeOffset? LastSuccessAt { get; set; }
    public string SafeMessage { get; set; } = "";
    public uint RowVersion { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
