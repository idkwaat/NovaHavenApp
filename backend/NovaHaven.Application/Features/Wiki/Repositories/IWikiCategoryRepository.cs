using NovaHaven.Domain.Wiki.Entities;

namespace NovaHaven.Application.Features.Wiki.Repositories;

public interface IWikiCategoryRepository
{
    Task<IReadOnlyList<WikiCategory>> ListAsync(CancellationToken cancellationToken);

    Task<WikiCategory?> FindAsync(Guid id, CancellationToken cancellationToken);

    Task<WikiCategory?> FindForUpdateAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> HasDuplicateAsync(string normalizedName, string slug, Guid? excludingId, CancellationToken cancellationToken);

    Task<bool> HasArticleReferencesAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> HasRevisionReferencesAsync(Guid id, CancellationToken cancellationToken);

    void Add(WikiCategory category);

    void Remove(WikiCategory category);
}
