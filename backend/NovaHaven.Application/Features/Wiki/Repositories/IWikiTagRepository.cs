using NovaHaven.Domain.Wiki.Entities;

namespace NovaHaven.Application.Features.Wiki.Repositories;

public interface IWikiTagRepository
{
    Task<IReadOnlyList<WikiTag>> ListAsync(CancellationToken cancellationToken);

    Task<WikiTag?> FindForUpdateAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> HasDuplicateAsync(string normalizedName, string slug, Guid? excludingId, CancellationToken cancellationToken);

    Task<bool> HasDraftReferencesAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> HasRevisionReferencesAsync(Guid id, CancellationToken cancellationToken);

    void Add(WikiTag tag);

    void Remove(WikiTag tag);
}
