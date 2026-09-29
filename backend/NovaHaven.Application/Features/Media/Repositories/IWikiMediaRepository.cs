using NovaHaven.Domain.Wiki.Entities;

namespace NovaHaven.Application.Features.Media.Repositories;

public interface IWikiMediaRepository
{
    Task<IReadOnlyList<WikiMediaListItemResult>> ListAsync(CancellationToken cancellationToken);

    Task<WikiMedia?> FindAsync(Guid id, CancellationToken cancellationToken);

    Task<WikiMedia?> FindCurrentlyPublishedAsync(Guid id, CancellationToken cancellationToken);

    void Add(WikiMedia media);

    void AddAudit(Guid? actorId, Guid mediaId, string contentType, long length, int width, int height);
}

public sealed record WikiMediaListItemResult(
    Guid Id,
    string OriginalFileName,
    string ContentType,
    long Length,
    int Width,
    int Height,
    DateTimeOffset CreatedAt);
