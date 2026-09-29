using Microsoft.EntityFrameworkCore;
using NovaHaven.Application.Common.Results;
using NovaHaven.Application.Features.Wiki.Queries;
using NovaHaven.Application.Features.Wiki.Repositories;
using NovaHaven.Application.Features.Wiki.Results;
using NovaHaven.Domain.Wiki;
using NovaHaven.Domain.Wiki.Entities;
using NovaHaven.Infrastructure.Data;

namespace NovaHaven.Infrastructure.Persistence.Repositories.Wiki;

public sealed class EfWikiReadRepository(NovaDbContext dbContext) : IWikiReadRepository
{
    public async Task<ApplicationResult<WikiArticlePageResult>> ListArticlesAsync(
        WikiArticleListQuery query,
        CancellationToken cancellationToken)
    {
        var articles = from article in dbContext.Articles.AsNoTracking()
                       join revision in dbContext.Revisions.AsNoTracking()
                           on article.PublishedRevisionId equals (Guid?)revision.Id
                       join category in dbContext.Categories.AsNoTracking()
                           on revision.CategoryId equals category.Id
                       where article.State == ArticleState.Published
                       select new { article, revision, category };

        if (!string.IsNullOrWhiteSpace(query.Query))
        {
            articles = articles.Where(x =>
                x.revision.Title.Contains(query.Query) || x.revision.Summary.Contains(query.Query));
        }

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            articles = articles.Where(x => x.category.Slug == query.Category);
        }

        if (!string.IsNullOrWhiteSpace(query.Tag))
        {
            articles = articles.Where(x => dbContext.RevisionTags.Any(revisionTag =>
                revisionTag.RevisionId == x.revision.Id
                && dbContext.Tags.Any(tag => tag.Id == revisionTag.TagId
                    && tag.IsActive && tag.Slug == query.Tag)));
        }

        var total = await articles.CountAsync(cancellationToken);
        var offset = checked((query.Page - 1) * query.PageSize);
        var items = await articles
            .OrderByDescending(x => x.revision.PublishedAt)
            .ThenByDescending(x => x.article.Id)
            .Skip(offset)
            .Take(query.PageSize)
            .Select(x => new
            {
                ArticleId = x.article.Id,
                x.article.Slug,
                RevisionId = x.revision.Id,
                x.revision.Title,
                x.revision.Summary,
                Category = x.category.Slug,
                x.revision.PublishedAt,
                Revision = x.revision.Number
            })
            .ToListAsync(cancellationToken);

        var revisionIds = items.Select(item => item.RevisionId).ToArray();
        var previewRows = await (
            from revisionMedia in dbContext.RevisionMedia.AsNoTracking()
            join media in dbContext.Media.AsNoTracking() on revisionMedia.MediaId equals media.Id
            where revisionIds.Contains(revisionMedia.RevisionId)
            orderby revisionMedia.RevisionId, revisionMedia.MediaId
            select new { revisionMedia.RevisionId, revisionMedia.MediaId, revisionMedia.AltText })
            .ToListAsync(cancellationToken);
        var previewByRevision = previewRows
            .GroupBy(item => item.RevisionId)
            .ToDictionary(group => group.Key, group => group.First());

        var tagRows = await (
            from revisionTag in dbContext.RevisionTags.AsNoTracking()
            join tag in dbContext.Tags.AsNoTracking() on revisionTag.TagId equals tag.Id
            where revisionIds.Contains(revisionTag.RevisionId) && tag.IsActive
            select new { revisionTag.RevisionId, tag.Slug })
            .ToListAsync(cancellationToken);
        var tagsByRevision = tagRows.ToLookup(item => item.RevisionId, item => item.Slug);

        var summaries = items.Select(item => new WikiArticleSummaryResult(
            item.ArticleId,
            item.Slug,
            item.Title,
            item.Summary,
            item.Category,
            tagsByRevision[item.RevisionId].OrderBy(tag => tag, StringComparer.Ordinal).ToArray(),
            item.PublishedAt,
            item.Revision,
            previewByRevision.TryGetValue(item.RevisionId, out var preview) ? preview.MediaId : null,
            previewByRevision.TryGetValue(item.RevisionId, out var altPreview) ? altPreview.AltText : null))
            .ToArray();

        return ApplicationResult<WikiArticlePageResult>.Success(
            new WikiArticlePageResult(summaries, query.Page, query.PageSize, total));
    }

    public async Task<ApplicationResult<WikiArticleResult>> GetArticleAsync(
        string slug,
        CancellationToken cancellationToken)
    {
        var article = await (
            from current in dbContext.Articles.AsNoTracking()
            join revision in dbContext.Revisions.AsNoTracking()
                on current.PublishedRevisionId equals (Guid?)revision.Id
            join category in dbContext.Categories.AsNoTracking() on revision.CategoryId equals category.Id
            where current.State == ArticleState.Published && current.Slug == slug
            select new
            {
                current.Id,
                current.Slug,
                RevisionId = revision.Id,
                revision.Title,
                revision.Summary,
                revision.Markdown,
                Category = category.Slug,
                revision.CategoryId,
                Revision = revision.Number,
                revision.PublishedAt
            }).SingleOrDefaultAsync(cancellationToken);

        if (article is null)
        {
            return ApplicationResult<WikiArticleResult>.Failure(
                new ApplicationError("wiki.article.not-found", "The requested article was not found."));
        }

        var tags = await (
            from revisionTag in dbContext.RevisionTags.AsNoTracking()
            join tag in dbContext.Tags.AsNoTracking() on revisionTag.TagId equals tag.Id
            where revisionTag.RevisionId == article.RevisionId && tag.IsActive
            orderby tag.Slug
            select tag.Slug)
            .ToArrayAsync(cancellationToken);

        var media = await (
            from revisionMedia in dbContext.RevisionMedia.AsNoTracking()
            join item in dbContext.Media.AsNoTracking() on revisionMedia.MediaId equals item.Id
            where revisionMedia.RevisionId == article.RevisionId
            orderby revisionMedia.MediaId
            select new WikiArticleMediaResult(
                item.Id,
                item.ContentType,
                item.OriginalFileName,
                item.Width,
                item.Height,
                revisionMedia.AltText))
            .ToArrayAsync(cancellationToken);

        var related = await (
            from candidate in dbContext.Articles.AsNoTracking()
            join revision in dbContext.Revisions.AsNoTracking()
                on candidate.PublishedRevisionId equals (Guid?)revision.Id
            join category in dbContext.Categories.AsNoTracking() on revision.CategoryId equals category.Id
            where candidate.State == ArticleState.Published
                && candidate.Id != article.Id
                && (revision.CategoryId == article.CategoryId
                    || dbContext.RevisionTags.Any(candidateTag =>
                        candidateTag.RevisionId == revision.Id
                        && dbContext.RevisionTags.Any(currentTag =>
                            currentTag.RevisionId == article.RevisionId
                            && currentTag.TagId == candidateTag.TagId)))
            orderby revision.CategoryId == article.CategoryId descending,
                revision.PublishedAt descending,
                candidate.Id
            select new WikiRelatedArticleResult(
                candidate.Id,
                candidate.Slug,
                revision.Title,
                revision.Summary,
                category.Slug,
                revision.Number,
                revision.PublishedAt))
            .Take(5)
            .ToArrayAsync(cancellationToken);

        return ApplicationResult<WikiArticleResult>.Success(new WikiArticleResult(
            article.Id,
            article.Slug,
            article.Title,
            article.Summary,
            article.Markdown,
            article.Category,
            tags,
            media,
            related,
            article.Revision,
            article.PublishedAt));
    }

    public async Task<ApplicationResult<IReadOnlyList<WikiTagResult>>> ListTagsAsync(
        CancellationToken cancellationToken)
    {
        var counts = await (
            from article in dbContext.Articles.AsNoTracking()
            join revisionTag in dbContext.RevisionTags.AsNoTracking()
                on article.PublishedRevisionId equals (Guid?)revisionTag.RevisionId
            where article.State == ArticleState.Published
            group revisionTag by revisionTag.TagId into publishedTags
            select new { TagId = publishedTags.Key, Count = publishedTags.Count() })
            .ToListAsync(cancellationToken);
        var countByTag = counts.ToDictionary(item => item.TagId, item => item.Count);
        var tags = await dbContext.Tags.AsNoTracking()
            .Where(tag => tag.IsActive)
            .OrderBy(tag => tag.Name)
            .Select(tag => new { tag.Id, tag.Name, tag.Slug })
            .ToListAsync(cancellationToken);

        IReadOnlyList<WikiTagResult> results = tags
            .Where(tag => countByTag.GetValueOrDefault(tag.Id) > 0)
            .Select(tag => new WikiTagResult(tag.Id, tag.Name, tag.Slug, countByTag.GetValueOrDefault(tag.Id)))
            .ToArray();
        return ApplicationResult<IReadOnlyList<WikiTagResult>>.Success(results);
    }

    public async Task<ApplicationResult<IReadOnlyList<WikiCategoryResult>>> ListCategoriesAsync(
        CancellationToken cancellationToken)
    {
        var counts = await (
            from article in dbContext.Articles.AsNoTracking()
            join revision in dbContext.Revisions.AsNoTracking()
                on article.PublishedRevisionId equals (Guid?)revision.Id
            where article.State == ArticleState.Published
            group revision by revision.CategoryId into publishedRevisions
            select new { CategoryId = publishedRevisions.Key, Count = publishedRevisions.Count() })
            .ToListAsync(cancellationToken);
        var countByCategory = counts.ToDictionary(item => item.CategoryId, item => item.Count);
        var categories = await dbContext.Categories.AsNoTracking()
            .Where(category => category.IsActive)
            .OrderBy(category => category.DisplayOrder)
            .ThenBy(category => category.Name)
            .Select(category => new { category.Id, category.Name, category.Slug, category.DisplayOrder })
            .ToListAsync(cancellationToken);

        IReadOnlyList<WikiCategoryResult> results = categories
            .Select(category => new WikiCategoryResult(
                category.Id,
                category.Name,
                category.Slug,
                category.DisplayOrder,
                countByCategory.GetValueOrDefault(category.Id)))
            .ToArray();
        return ApplicationResult<IReadOnlyList<WikiCategoryResult>>.Success(results);
    }
}
