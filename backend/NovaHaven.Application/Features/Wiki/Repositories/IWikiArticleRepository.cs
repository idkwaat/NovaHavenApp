using NovaHaven.Application.Features.Wiki.Results;
using NovaHaven.Domain.Wiki.Entities;

namespace NovaHaven.Application.Features.Wiki.Repositories;

public interface IWikiArticleRepository
{
    Task<IReadOnlyList<WikiArticleAdminListItemResult>> ListAsync(CancellationToken cancellationToken);

    Task<WikiArticleDraftResult?> FindDraftAsync(Guid id, CancellationToken cancellationToken);

    Task<WikiArticle?> FindForUpdateAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);

    Task<WikiArticleRevision?> FindRevisionAsync(Guid articleId, Guid revisionId, CancellationToken cancellationToken);

    Task<IReadOnlyList<WikiArticleRevisionResult>> ListRevisionsAsync(Guid articleId, CancellationToken cancellationToken);

    Task<bool> IsSlugReservedAsync(string slug, Guid? excludingArticleId, CancellationToken cancellationToken);

    Task<bool> IsActiveCategoryAsync(Guid categoryId, CancellationToken cancellationToken);

    Task<bool> AreActiveTagsAsync(IReadOnlyCollection<Guid> tagIds, CancellationToken cancellationToken);

    Task<bool> AreExistingMediaAsync(IReadOnlyCollection<Guid> mediaIds, CancellationToken cancellationToken);

    Task<Guid[]> GetDraftTagIdsAsync(Guid articleId, CancellationToken cancellationToken);

    Task<Guid[]> GetDraftMediaIdsAsync(Guid articleId, CancellationToken cancellationToken);

    Task<Guid[]> GetRevisionTagIdsAsync(Guid revisionId, CancellationToken cancellationToken);

    Task<Guid[]> GetRevisionMediaIdsAsync(Guid revisionId, CancellationToken cancellationToken);

    void Add(WikiArticle article);

    void AddDraftTags(Guid articleId, IReadOnlyCollection<Guid> tagIds);

    void AddDraftMedia(Guid articleId, IReadOnlyCollection<Guid> mediaIds);

    Task ReplaceDraftTagsAsync(Guid articleId, IReadOnlyCollection<Guid> tagIds, CancellationToken cancellationToken);

    Task ReplaceDraftMediaAsync(Guid articleId, IReadOnlyCollection<Guid> mediaIds, CancellationToken cancellationToken);

    void AddPublishedSnapshot(WikiArticleRevision revision, IReadOnlyCollection<Guid> tagIds,
        IReadOnlyCollection<Guid> mediaIds);

    void AddAudit(Guid? actorId, string action, Guid articleId, object? details = null);
}
