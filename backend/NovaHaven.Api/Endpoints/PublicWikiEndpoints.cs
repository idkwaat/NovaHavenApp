using Microsoft.EntityFrameworkCore;
using NovaHaven.Domain.Wiki;
using NovaHaven.Domain.Wiki.Entities;
using NovaHaven.Infrastructure.Data;

namespace NovaHaven.Api.Endpoints;

public static class PublicWikiEndpoints
{
    public static void MapPublicWikiEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/wiki");
        group.MapGet("/articles", async (NovaDbContext db, string? q, string? category, string? tag, int? page, int? pageSize, CancellationToken ct) =>
        {
            var currentPage = page ?? 1;
            var size = pageSize ?? 20;
            if (currentPage < 1 || size is < 1 or > 50 || (q?.Length ?? 0) > 100 || (category?.Length ?? 0) > 80 || (tag?.Length ?? 0) > 80)
                return Results.Problem(statusCode: 400, title: "Invalid pagination or filter.");
            var query = from article in db.Articles.AsNoTracking()
                        join revision in db.Revisions.AsNoTracking() on article.PublishedRevisionId equals (Guid?)revision.Id
                        join categoryRow in db.Categories.AsNoTracking() on revision.CategoryId equals categoryRow.Id
                        where article.State == ArticleState.Published
                        select new { article, revision, categoryRow };
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(x => x.revision.Title.Contains(term) || x.revision.Summary.Contains(term));
            }
            if (!string.IsNullOrWhiteSpace(category)) query = query.Where(x => x.categoryRow.Slug == category);
            if (!string.IsNullOrWhiteSpace(tag))
            {
                query = query.Where(x => db.RevisionTags.Any(rt => rt.RevisionId == x.revision.Id &&
                    db.Tags.Any(t => t.Id == rt.TagId && t.IsActive && t.Slug == tag)));
            }
            var total = await query.CountAsync(ct);
            // Avoid integer overflow for user-controlled page arithmetic.
            var offset = ((long)currentPage - 1) * size;
            if (offset > int.MaxValue) return Results.Problem(statusCode: 400, title: "Page is out of range.");
            var items = await query.OrderByDescending(x => x.revision.PublishedAt).ThenByDescending(x => x.article.Id)
                .Skip((int)offset).Take(size)
                .Select(x => new { id = x.article.Id, revisionId = x.revision.Id,
                    slug = x.article.Slug, title = x.revision.Title,
                    summary = x.revision.Summary, category = x.categoryRow.Slug,
                    publishedAt = x.revision.PublishedAt, revision = x.revision.Number }).ToListAsync(ct);
            var revisionIds = items.Select(x => x.revisionId).ToArray();
            var previewRows = await (from revisionMedia in db.RevisionMedia.AsNoTracking()
                join media in db.Media.AsNoTracking() on revisionMedia.MediaId equals media.Id
                where revisionIds.Contains(revisionMedia.RevisionId)
                orderby revisionMedia.RevisionId, revisionMedia.MediaId
                select new { revisionMedia.RevisionId, revisionMedia.MediaId, revisionMedia.AltText })
                .ToListAsync(ct);
            var previewByRevision = previewRows.GroupBy(x => x.RevisionId)
                .ToDictionary(group => group.Key, group => group.First());
            var tagRows = await (from rt in db.RevisionTags.AsNoTracking()
                join t in db.Tags.AsNoTracking() on rt.TagId equals t.Id
                where revisionIds.Contains(rt.RevisionId) && t.IsActive
                select new { rt.RevisionId, t.Slug }).ToListAsync(ct);
            var tagsByRevision = tagRows.ToLookup(x => x.RevisionId, x => x.Slug);
            var result = items.Select(x => new { x.id, x.slug, x.title, x.summary, x.category,
                tags = tagsByRevision[x.revisionId].OrderBy(t => t).ToArray(), x.publishedAt, x.revision,
                previewImageUrl = previewByRevision.TryGetValue(x.revisionId, out var preview)
                    ? $"/api/v1/wiki/media/{preview.MediaId}" : null,
                previewImageAlt = previewByRevision.TryGetValue(x.revisionId, out var altPreview)
                    ? altPreview.AltText : null });
            return Results.Ok(new { items = result, page = currentPage, pageSize = size, total });
        });

        group.MapGet("/articles/{slug}", async (NovaDbContext db, string slug, CancellationToken ct) =>
        {
            var item = await (from article in db.Articles.AsNoTracking()
                join revision in db.Revisions.AsNoTracking() on article.PublishedRevisionId equals (Guid?)revision.Id
                join category in db.Categories.AsNoTracking() on revision.CategoryId equals category.Id
                where article.State == ArticleState.Published && article.Slug == slug
                select new { id = article.Id, revisionId = revision.Id,
                    slug = article.Slug, title = revision.Title,
                    summary = revision.Summary, markdown = revision.Markdown, category = category.Slug,
                    categoryId = revision.CategoryId, revision = revision.Number, publishedAt = revision.PublishedAt }).SingleOrDefaultAsync(ct);
            if (item is null) return Results.NotFound();
            var tags = await (from rt in db.RevisionTags.AsNoTracking()
                join t in db.Tags.AsNoTracking() on rt.TagId equals t.Id
                where rt.RevisionId == item.revisionId && t.IsActive
                orderby t.Slug select t.Slug).ToArrayAsync(ct);
            var media = await (from rm in db.RevisionMedia.AsNoTracking()
                join m in db.Media.AsNoTracking() on rm.MediaId equals m.Id
                where rm.RevisionId == item.revisionId
                orderby rm.MediaId
                select new { id = m.Id, url = $"/api/v1/wiki/media/{m.Id}", m.ContentType,
                    m.OriginalFileName, m.Width, m.Height, alt = rm.AltText }).ToArrayAsync(ct);
            var related = await (from candidate in db.Articles.AsNoTracking()
                join candidateRevision in db.Revisions.AsNoTracking() on candidate.PublishedRevisionId equals (Guid?)candidateRevision.Id
                join candidateCategory in db.Categories.AsNoTracking() on candidateRevision.CategoryId equals candidateCategory.Id
                where candidate.State == ArticleState.Published && candidate.Id != item.id
                    && (candidateRevision.CategoryId == item.categoryId || db.RevisionTags.Any(candidateTag =>
                        candidateTag.RevisionId == candidateRevision.Id && db.RevisionTags.Any(currentTag =>
                            currentTag.RevisionId == item.revisionId && currentTag.TagId == candidateTag.TagId)))
                orderby candidateRevision.CategoryId == item.categoryId descending,
                    candidateRevision.PublishedAt descending, candidate.Id
                select new { id = candidate.Id, slug = candidate.Slug, title = candidateRevision.Title,
                    summary = candidateRevision.Summary, category = candidateCategory.Slug,
                    revision = candidateRevision.Number, publishedAt = candidateRevision.PublishedAt })
                .Take(5).ToArrayAsync(ct);
            return Results.Ok(new { item.id, item.slug, item.title, item.summary, item.markdown,
                item.category, tags, media, related, item.revision, item.publishedAt });
        });

        group.MapGet("/tags", async (NovaDbContext db, CancellationToken ct) =>
        {
            // Count only the CURRENT published revision, not drafts or superseded history.
            var counts = await (from article in db.Articles.AsNoTracking()
                join rt in db.RevisionTags.AsNoTracking() on article.PublishedRevisionId equals (Guid?)rt.RevisionId
                where article.State == ArticleState.Published
                group rt by rt.TagId into g
                select new { tagId = g.Key, count = g.Count() }).ToListAsync(ct);
            var byId = counts.ToDictionary(x => x.tagId, x => x.count);
            var tags = await db.Tags.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name)
                .Select(x => new { x.Id, x.Name, x.Slug }).ToListAsync(ct);
            return Results.Ok(tags.Where(x => byId.GetValueOrDefault(x.Id) > 0)
                .Select(x => new { id = x.Id, name = x.Name, slug = x.Slug,
                    articleCount = byId.GetValueOrDefault(x.Id) }));
        });

        group.MapGet("/categories", async (NovaDbContext db, CancellationToken ct) =>
        {
            var counts = await (from article in db.Articles.AsNoTracking()
                join revision in db.Revisions.AsNoTracking() on article.PublishedRevisionId equals (Guid?)revision.Id
                where article.State == ArticleState.Published
                group revision by revision.CategoryId into g
                select new { categoryId = g.Key, count = g.Count() }).ToListAsync(ct);
            var countByCategory = counts.ToDictionary(x => x.categoryId, x => x.count);
            var categories = await db.Categories.AsNoTracking().Where(x => x.IsActive)
                .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name)
                .Select(x => new { x.Id, x.Name, x.Slug, x.DisplayOrder }).ToListAsync(ct);
            return Results.Ok(categories.Select(x => new { id = x.Id, name = x.Name, slug = x.Slug,
                displayOrder = x.DisplayOrder, articleCount = countByCategory.GetValueOrDefault(x.Id) }));
        });
    }
}
