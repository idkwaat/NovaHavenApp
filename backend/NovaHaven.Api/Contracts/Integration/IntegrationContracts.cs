namespace NovaHaven.Api.Contracts.Integration;

public sealed record IntegrationCapabilityResponse(
    string CapabilityKey, string DisplayName, string Owner, string Status,
    DateTimeOffset? LastCheckedAt, DateTimeOffset? LastSuccessAt, string SafeMessage);

public sealed record AdminIntegrationCapabilityResponse(
    Guid Id, string CapabilityKey, string DisplayName, string Owner, string Status,
    DateTimeOffset? LastCheckedAt, DateTimeOffset? LastSuccessAt, string SafeMessage,
    DateTimeOffset UpdatedAt, string Etag);

public sealed record IntegrationCapabilityUpdateRequest(string? Status, string? SafeMessage);

public sealed record IntegrationCapabilityUpdateResponse(
    string CapabilityKey, string Status, string SafeMessage, string Etag);
