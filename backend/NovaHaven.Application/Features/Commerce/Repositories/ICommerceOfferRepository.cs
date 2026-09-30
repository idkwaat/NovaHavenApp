using NovaHaven.Application.Features.Commerce.Results;
using NovaHaven.Domain.Commerce.Entities;

namespace NovaHaven.Application.Features.Commerce.Repositories;

public interface ICommerceOfferRepository
{
    Task<int> CountPublishedAsync(string? search, CancellationToken cancellationToken);
    Task<IReadOnlyList<CommerceOfferSummaryResult>> ListPublishedAsync(
        string? search, int offset, int pageSize, CancellationToken cancellationToken);
    Task<CommerceOfferDetailResult?> FindPublishedAsync(string slug, CancellationToken cancellationToken);
    Task<IReadOnlyList<CommerceOfferAdminListResult>> ListAdminAsync(CancellationToken cancellationToken);
    Task<CommerceOfferAdminDraftResult?> FindAdminAsync(Guid id, CancellationToken cancellationToken);
    Task<CommerceOffer?> FindForUpdateAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> IsSlugInUseAsync(string slug, Guid? excludingId, CancellationToken cancellationToken);
    Task<bool> IsPublishedSlugInUseAsync(string slug, Guid excludingId, CancellationToken cancellationToken);
    void Add(CommerceOffer offer);
    void AddRevision(CommerceOfferRevision revision);
    void AddAudit(Guid? actorId, string action, Guid id, object? details = null);
}
