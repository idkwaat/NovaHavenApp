using NovaHaven.Application.Common.Concurrency;
using NovaHaven.Application.Common.Results;
using NovaHaven.Application.Common.Transactions;
using NovaHaven.Application.Features.Integration.Repositories;
using NovaHaven.Application.Features.Integration.Results;
using NovaHaven.Application.Integration;
using NovaHaven.Domain.Integration.Entities;

namespace NovaHaven.Application.Features.Integration.Services;

public sealed class IntegrationCapabilityService(
    IIntegrationCapabilityRepository repository,
    IUnitOfWork unitOfWork)
{
    private const string UnavailableMessage = "No external adapter or provider contract is configured in this local workspace.";

    private static readonly IntegrationCapabilityDefinition[] Known =
    [
        new("minecraft-bridge", "Minecraft Bridge", "minecraft"),
        new("minecraft-player-sync", "Minecraft Player Sync", "minecraft"),
        new("minecraft-rewards", "Minecraft Rewards", "minecraft"),
        new("commerce-provider", "Commerce Provider", "commerce")
    ];

    public async Task<IReadOnlyList<IntegrationCapabilityResult>> ListAdminAsync(CancellationToken cancellationToken)
    {
        await EnsureKnownAsync(cancellationToken);
        return (await repository.ListAsync(cancellationToken)).Select(ToResult).ToArray();
    }

    public async Task<IReadOnlyList<PublicIntegrationCapabilityResult>> ListPublicAsync(CancellationToken cancellationToken)
    {
        var persisted = await repository.ListAsync(cancellationToken);
        return Known.Select(definition =>
        {
            var row = persisted.SingleOrDefault(capability => capability.CapabilityKey == definition.Key);
            return row is null
                ? new PublicIntegrationCapabilityResult(definition.Key, definition.Name, definition.Owner,
                    IntegrationStatus.Unavailable, null, null, UnavailableMessage)
                : new PublicIntegrationCapabilityResult(row.CapabilityKey, row.DisplayName, row.Owner,
                    row.Status, row.LastCheckedAt, row.LastSuccessAt, row.SafeMessage);
        }).ToArray();
    }

    public async Task<ApplicationResult<IntegrationCapabilityUpdateResult>> UpdateAsync(
        string key, string? status, string? safeMessage, byte[]? expectedVersion,
        Guid? actorId, CancellationToken cancellationToken)
    {
        await EnsureKnownAsync(cancellationToken);
        var capability = await repository.FindForUpdateAsync(key, cancellationToken);
        if (capability is null) return Failure("integration.capability.not-found", "Integration capability was not found.");
        if (expectedVersion is null)
            return Failure("http.precondition-required", "If-Match is required.");
        if (!ConcurrencyVersion.Matches(capability.RowVersion, expectedVersion))
            return Failure("http.precondition-failed", "Capability changed; reload before editing.");
        if (!Enum.TryParse<IntegrationStatus>(status, true, out var next) || !Enum.IsDefined(next))
            return Invalid("Unsupported integration status.", "status");

        var safeErrors = IntegrationCapabilityPolicy.ValidateSafeMessage(safeMessage ?? capability.SafeMessage);
        if (safeErrors.Count > 0)
            return ApplicationResult<IntegrationCapabilityUpdateResult>.Failure(
                new ApplicationError("validation.failed", "One or more validation errors occurred.", safeErrors));
        if (!IntegrationCapabilityPolicy.CanTransition(capability.Status, next))
            return Invalid("Capability cannot transition to this status without a configured adapter.", "status");

        var now = DateTimeOffset.UtcNow;
        capability.Status = next;
        capability.SafeMessage = safeMessage?.Trim() ?? capability.SafeMessage;
        capability.LastCheckedAt = now;
        if (next == IntegrationStatus.Healthy) capability.LastSuccessAt = now;
        capability.UpdatedAt = now;
        repository.AddAudit(actorId, "integration.capability.updated", capability.Id,
            new { capability.CapabilityKey, status = StatusName(next) });
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApplicationResult<IntegrationCapabilityUpdateResult>.Success(
            new IntegrationCapabilityUpdateResult(capability.CapabilityKey, next, capability.SafeMessage, capability.RowVersion));
    }

    private async Task EnsureKnownAsync(CancellationToken cancellationToken)
    {
        var existing = await repository.ListKeysAsync(cancellationToken);
        foreach (var definition in Known.Where(item => !existing.Contains(item.Key, StringComparer.Ordinal)))
        {
            repository.Add(new IntegrationCapability
            {
                CapabilityKey = definition.Key,
                DisplayName = definition.Name,
                Owner = definition.Owner,
                Status = IntegrationStatus.Unavailable,
                SafeMessage = UnavailableMessage
            });
        }
        if (Known.Any(item => !existing.Contains(item.Key, StringComparer.Ordinal)))
            await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static IntegrationCapabilityResult ToResult(IntegrationCapability capability) => new(
        capability.Id, capability.CapabilityKey, capability.DisplayName, capability.Owner,
        capability.Status, capability.LastCheckedAt, capability.LastSuccessAt,
        capability.SafeMessage, capability.UpdatedAt, capability.RowVersion);

    private static string StatusName(IntegrationStatus status) => status.ToString().ToLowerInvariant();

    private static ApplicationResult<IntegrationCapabilityUpdateResult> Invalid(string message, string field) =>
        ApplicationResult<IntegrationCapabilityUpdateResult>.Failure(new ApplicationError(
            "validation.failed", "One or more validation errors occurred.",
            new Dictionary<string, string[]> { [field] = [message] }));

    private static ApplicationResult<IntegrationCapabilityUpdateResult> Failure(string code, string message) =>
        ApplicationResult<IntegrationCapabilityUpdateResult>.Failure(new ApplicationError(code, message));
}
