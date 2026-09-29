using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NovaHaven.Application.Features.Wiki.Repositories;
using NovaHaven.Application.Features.Wiki.Results;
using NovaHaven.Domain.Wiki.Entities;
using NovaHaven.Infrastructure.Data;

namespace NovaHaven.Infrastructure.Persistence.Repositories.Wiki;

public sealed class EfWikiArticleRepository(NovaDbContext dbContext) : IWikiArticleRepository
{
    public async Task<IReadOnlyList<WikiArticleAdminListItemResult>> ListAsync(CancellationToken cancellationToken) =>
        await dbContext.Articles.AsNoTracking()
            .OrderByDescending(article => article.UpdatedAt)
            .Select(article => new WikiArticleAdminListItemResult(
                article.Id, article.Slug, article.DraftTitle, article.State, article.UpdatedAt, article.LatestRevisionNumber))
            .Take(100)
            .ToArrayAsync(cancellationToken);

    public async Task<WikiArticleDraftResult?> FindDraftAsync(Guid id, CancellationToken cancellationToken)
    {
        var article = await dbContext.Articles.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (article is null) return null;

        var tagIds = await GetDraftTagIdsAsync(id, cancellationToken);
        var mediaIds = await GetDraftMediaIdsAsync(id, cancellationToken);
        return new WikiArticleDraftResult(
            article.Id,
            article.Slug,
            article.DraftTitle,
            article.DraftSummary,
            article.DraftMarkdown,
            article.DraftCategoryId,
            tagIds,
            article.State,
            mediaIds,
            article.LatestRevisionNumber,
            article.PublishedRevisionId,
            article.RowVersion);
    }

    public Task<WikiArticle?> FindForUpdateAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Articles.SingleOrDefaultAsync(article => article.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Articles.AsNoTracking().AnyAsync(article => article.Id == id, cancellationToken);

    public Task<WikiArticleRevision?> FindRevisionAsync(
        Guid articleId,
        Guid revisionId,
        CancellationToken cancellationToken) =>
        dbContext.Revisions.AsNoTracking().SingleOrDefaultAsync(
            revision => revision.Id == revisionId && revision.ArticleId == articleId, cancellationToken);

    public async Task<IReadOnlyList<WikiArticleRevisionResult>> ListRevisionsAsync(
        Guid articleId,
        CancellationToken cancellationToken) =>
        await dbContext.Revisions.AsNoTracking()
            .Where(revision => revision.ArticleId == articleId)
            .OrderByDescending(revision => revision.Number)
            .Select(revision => new WikiArticleRevisionResult(
                revision.Id, revision.Number, revision.Title, revision.PublishedAt, revision.PublishedBy))
            .ToArrayAsync(cancellationToken);

    public Task<bool> IsSlugReservedAsync(
        string slug,
        Guid? excludingArticleId,
        CancellationToken cancellationToken)
    {
        var articles = dbContext.Articles.AsNoTracking().Where(article => article.Slug == slug);
        if (excludingArticleId.HasValue)
            articles = articles.Where(article => article.Id != excludingArticleId.Value);
        return articles.AnyAsync(cancellationToken);
    }

    public Task<bool> IsActiveCategoryAsync(Guid categoryId, CancellationToken cancellationToken) =>
        dbContext.Categories.AsNoTracking().AnyAsync(
            category => category.Id == categoryId && category.IsActive, cancellationToken);

    public async Task<bool> AreActiveTagsAsync(
        IReadOnlyCollection<Guid> tagIds,
        CancellationToken cancellationToken) =>
        tagIds.Count == 0 || await dbContext.Tags.AsNoTracking()
            .CountAsync(tag => tagIds.Contains(tag.Id) && tag.IsActive, cancellationToken) == tagIds.Count;

    public async Task<bool> AreExistingMediaAsync(
        IReadOnlyCollection<Guid> mediaIds,
        CancellationToken cancellationToken) =>
        mediaIds.Count == 0 || await dbContext.Media.AsNoTracking()
            .CountAsync(media => mediaIds.Contains(media.Id), cancellationToken) == mediaIds.Count;

    public Task<Guid[]> GetDraftTagIdsAsync(Guid articleId, CancellationToken cancellationToken) =>
        dbContext.DraftTags.AsNoTracking().Where(reference => reference.ArticleId == articleId)
            .OrderBy(reference => reference.TagId).Select(reference => reference.TagId).ToArrayAsync(cancellationToken);

    public Task<Guid[]> GetDraftMediaIdsAsync(Guid articleId, CancellationToken cancellationToken) =>
        dbContext.DraftMedia.AsNoTracking().Where(reference => reference.ArticleId == articleId)
            .OrderBy(reference => reference.MediaId).Select(reference => reference.MediaId).ToArrayAsync(cancellationToken);

    public Task<Guid[]> GetRevisionTagIdsAsync(Guid revisionId, CancellationToken cancellationToken) =>
        dbContext.RevisionTags.AsNoTracking().Where(reference => reference.RevisionId == revisionId)
            .OrderBy(reference => reference.TagId).Select(reference => reference.TagId).ToArrayAsync(cancellationToken);

    public Task<Guid[]> GetRevisionMediaIdsAsync(Guid revisionId, CancellationToken cancellationToken) =>
        dbContext.RevisionMedia.AsNoTracking().Where(reference => reference.RevisionId == revisionId)
            .OrderBy(reference => reference.MediaId).Select(reference => reference.MediaId).ToArrayAsync(cancellationToken);

    public void Add(WikiArticle article) => dbContext.Articles.Add(article);

    public void AddDraftTags(Guid articleId, IReadOnlyCollection<Guid> tagIds) =>
        dbContext.DraftTags.AddRange(tagIds.Select(tagId => new WikiDraftTag { ArticleId = articleId, TagId = tagId }));

    public void AddDraftMedia(Guid articleId, IReadOnlyCollection<Guid> mediaIds) =>
        dbContext.DraftMedia.AddRange(mediaIds.Select(mediaId => new WikiDraftMedia { ArticleId = articleId, MediaId = mediaId }));

    public async Task ReplaceDraftTagsAsync(
        Guid articleId,
        IReadOnlyCollection<Guid> tagIds,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.DraftTags.Where(reference => reference.ArticleId == articleId).ToListAsync(cancellationToken);
        var next = tagIds.ToHashSet();
        var previous = existing.Select(reference => reference.TagId).ToHashSet();
        dbContext.DraftTags.RemoveRange(existing.Where(reference => !next.Contains(reference.TagId)));
        dbContext.DraftTags.AddRange(next.Where(tagId => !previous.Contains(tagId))
            .Select(tagId => new WikiDraftTag { ArticleId = articleId, TagId = tagId }));
    }

    public async Task ReplaceDraftMediaAsync(
        Guid articleId,
        IReadOnlyCollection<Guid> mediaIds,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.DraftMedia.Where(reference => reference.ArticleId == articleId).ToListAsync(cancellationToken);
        var next = mediaIds.ToHashSet();
        var previous = existing.Select(reference => reference.MediaId).ToHashSet();
        dbContext.DraftMedia.RemoveRange(existing.Where(reference => !next.Contains(reference.MediaId)));
        dbContext.DraftMedia.AddRange(next.Where(mediaId => !previous.Contains(mediaId))
            .Select(mediaId => new WikiDraftMedia { ArticleId = articleId, MediaId = mediaId }));
    }

    public void AddPublishedSnapshot(
        WikiArticleRevision revision,
        IReadOnlyCollection<Guid> tagIds,
        IReadOnlyCollection<Guid> mediaIds)
    {
        dbContext.Revisions.Add(revision);
        dbContext.RevisionTags.AddRange(tagIds.Select(tagId => new WikiRevisionTag
            { RevisionId = revision.Id, TagId = tagId }));
        dbContext.RevisionMedia.AddRange(mediaIds.Select(mediaId => new WikiRevisionMedia
            { RevisionId = revision.Id, MediaId = mediaId }));
    }

    public void AddAudit(Guid? actorId, string action, Guid articleId, object? details = null)
    {
        if (actorId is null) return;
        var detailsJson = JsonSerializer.Serialize(details ?? new { });
        dbContext.AuditEvents.Add(new WikiAuditEvent
        {
            ActorUserId = actorId.Value,
            Action = action,
            EntityType = "WikiArticle",
            EntityId = articleId,
            DetailsJson = detailsJson.Length <= 4000 ? detailsJson : detailsJson[..4000],
            OccurredAt = DateTimeOffset.UtcNow
        });
    }
}
