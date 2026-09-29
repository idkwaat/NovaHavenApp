using Microsoft.EntityFrameworkCore;
using NovaHaven.Application.Features.Wiki.Repositories;
using NovaHaven.Domain.Wiki.Entities;
using NovaHaven.Infrastructure.Data;

namespace NovaHaven.Infrastructure.Persistence.Repositories.Wiki;

public sealed class EfWikiCategoryRepository(NovaDbContext dbContext) : IWikiCategoryRepository
{
    public async Task<IReadOnlyList<WikiCategory>> ListAsync(CancellationToken cancellationToken) =>
        await dbContext.Categories.AsNoTracking()
            .OrderBy(category => category.DisplayOrder)
            .ThenBy(category => category.Name)
            .ToArrayAsync(cancellationToken);

    public Task<WikiCategory?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Categories.AsNoTracking().SingleOrDefaultAsync(category => category.Id == id, cancellationToken);

    public Task<WikiCategory?> FindForUpdateAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Categories.SingleOrDefaultAsync(category => category.Id == id, cancellationToken);

    public Task<bool> HasDuplicateAsync(
        string normalizedName,
        string slug,
        Guid? excludingId,
        CancellationToken cancellationToken)
    {
        var categories = dbContext.Categories.AsNoTracking()
            .Where(category => category.NormalizedName == normalizedName || category.Slug == slug);
        if (excludingId.HasValue) categories = categories.Where(category => category.Id != excludingId.Value);
        return categories.AnyAsync(cancellationToken);
    }

    public Task<bool> HasArticleReferencesAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Articles.AnyAsync(article => article.DraftCategoryId == id, cancellationToken);

    public Task<bool> HasRevisionReferencesAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Revisions.AnyAsync(revision => revision.CategoryId == id, cancellationToken);

    public void Add(WikiCategory category) => dbContext.Categories.Add(category);

    public void Remove(WikiCategory category) => dbContext.Categories.Remove(category);
}
