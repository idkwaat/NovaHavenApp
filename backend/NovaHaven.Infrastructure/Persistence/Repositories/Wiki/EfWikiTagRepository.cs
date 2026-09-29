using Microsoft.EntityFrameworkCore;
using NovaHaven.Application.Features.Wiki.Repositories;
using NovaHaven.Domain.Wiki.Entities;
using NovaHaven.Infrastructure.Data;

namespace NovaHaven.Infrastructure.Persistence.Repositories.Wiki;

public sealed class EfWikiTagRepository(NovaDbContext dbContext) : IWikiTagRepository
{
    public async Task<IReadOnlyList<WikiTag>> ListAsync(CancellationToken cancellationToken) =>
        await dbContext.Tags.AsNoTracking().OrderBy(tag => tag.Name).ToArrayAsync(cancellationToken);

    public Task<WikiTag?> FindForUpdateAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Tags.SingleOrDefaultAsync(tag => tag.Id == id, cancellationToken);

    public Task<bool> HasDuplicateAsync(
        string normalizedName,
        string slug,
        Guid? excludingId,
        CancellationToken cancellationToken)
    {
        var tags = dbContext.Tags.AsNoTracking()
            .Where(tag => tag.NormalizedName == normalizedName || tag.Slug == slug);
        if (excludingId.HasValue) tags = tags.Where(tag => tag.Id != excludingId.Value);
        return tags.AnyAsync(cancellationToken);
    }

    public Task<bool> HasDraftReferencesAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.DraftTags.AnyAsync(reference => reference.TagId == id, cancellationToken);

    public Task<bool> HasRevisionReferencesAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.RevisionTags.AnyAsync(reference => reference.TagId == id, cancellationToken);

    public void Add(WikiTag tag) => dbContext.Tags.Add(tag);

    public void Remove(WikiTag tag) => dbContext.Tags.Remove(tag);
}
