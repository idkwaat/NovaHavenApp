using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NovaHaven.Application.Features.Integration.Repositories;
using NovaHaven.Domain.Integration.Entities;
using NovaHaven.Domain.Wiki.Entities;
using NovaHaven.Infrastructure.Data;

namespace NovaHaven.Infrastructure.Persistence.Repositories.Integration;

public sealed class EfIntegrationCapabilityRepository(NovaDbContext dbContext) : IIntegrationCapabilityRepository
{
    public async Task<IReadOnlyList<IntegrationCapability>> ListAsync(CancellationToken cancellationToken) =>
        await dbContext.IntegrationCapabilities.AsNoTracking()
            .OrderBy(capability => capability.CapabilityKey)
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyList<string>> ListKeysAsync(CancellationToken cancellationToken) =>
        await dbContext.IntegrationCapabilities.Select(capability => capability.CapabilityKey)
            .ToArrayAsync(cancellationToken);

    public Task<IntegrationCapability?> FindForUpdateAsync(string key, CancellationToken cancellationToken) =>
        dbContext.IntegrationCapabilities.SingleOrDefaultAsync(
            capability => capability.CapabilityKey == key, cancellationToken);

    public void Add(IntegrationCapability capability) => dbContext.IntegrationCapabilities.Add(capability);

    public void AddAudit(Guid? actorId, string action, Guid id, object? details = null)
    {
        if (actorId is null) return;
        var json = JsonSerializer.Serialize(details ?? new { });
        dbContext.AuditEvents.Add(new WikiAuditEvent
        {
            ActorUserId = actorId.Value,
            Action = action,
            EntityType = "IntegrationCapability",
            EntityId = id,
            DetailsJson = json.Length <= 4000 ? json : json[..4000],
            OccurredAt = DateTimeOffset.UtcNow
        });
    }
}
