using NovaHaven.Application.Features.Rewards.Results;
using NovaHaven.Domain.Rewards.Entities;

namespace NovaHaven.Application.Features.Rewards.Repositories;

public interface IRewardRepository
{
    Task<int> CountPublishedAsync(string? search, CancellationToken cancellationToken);
    Task<IReadOnlyList<RewardListItemResult>> ListPublishedAsync(string? search, int offset, int pageSize, CancellationToken cancellationToken);
    Task<RewardDetailResult?> FindPublishedBySlugAsync(string slug, CancellationToken cancellationToken);
    Task<IReadOnlyList<RewardAdminListItemResult>> ListAdminAsync(CancellationToken cancellationToken);
    Task<RewardAdminDraftResult?> FindAdminDraftAsync(Guid id, CancellationToken cancellationToken);
    Task<RewardDefinition?> FindForUpdateAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> IsSlugInUseAsync(string slug, Guid? excludingId, CancellationToken cancellationToken);
    void Add(RewardDefinition definition);
    void AddRevision(RewardDefinitionRevision revision);
    void AddAudit(Guid? actorId, string action, Guid id, object? details = null);
}
