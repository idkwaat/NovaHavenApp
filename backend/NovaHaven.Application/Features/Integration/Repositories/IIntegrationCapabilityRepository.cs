using NovaHaven.Domain.Integration.Entities;

namespace NovaHaven.Application.Features.Integration.Repositories;

public interface IIntegrationCapabilityRepository
{
    Task<IReadOnlyList<IntegrationCapability>> ListAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> ListKeysAsync(CancellationToken cancellationToken);
    Task<IntegrationCapability?> FindForUpdateAsync(string key, CancellationToken cancellationToken);
    void Add(IntegrationCapability capability);
    void AddAudit(Guid? actorId, string action, Guid id, object? details = null);
}
