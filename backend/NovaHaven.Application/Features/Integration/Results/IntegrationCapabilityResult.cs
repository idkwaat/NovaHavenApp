using NovaHaven.Domain.Integration.Entities;

namespace NovaHaven.Application.Features.Integration.Results;

public sealed record IntegrationCapabilityResult(
    Guid Id,
    string CapabilityKey,
    string DisplayName,
    string Owner,
    IntegrationStatus Status,
    DateTimeOffset? LastCheckedAt,
    DateTimeOffset? LastSuccessAt,
    string SafeMessage,
    DateTimeOffset UpdatedAt,
    uint RowVersion);

public sealed record PublicIntegrationCapabilityResult(
    string CapabilityKey,
    string DisplayName,
    string Owner,
    IntegrationStatus Status,
    DateTimeOffset? LastCheckedAt,
    DateTimeOffset? LastSuccessAt,
    string SafeMessage);

public sealed record IntegrationCapabilityUpdateResult(
    string CapabilityKey,
    IntegrationStatus Status,
    string SafeMessage,
    uint RowVersion);

public sealed record IntegrationCapabilityDefinition(string Key, string Name, string Owner);
