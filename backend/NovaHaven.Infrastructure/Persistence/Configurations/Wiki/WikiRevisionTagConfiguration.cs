using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaHaven.Domain.Wiki.Entities;
namespace NovaHaven.Infrastructure.Persistence.Configurations.Wiki;

public sealed class WikiRevisionTagConfiguration : IEntityTypeConfiguration<WikiRevisionTag>
{
    public void Configure(EntityTypeBuilder<WikiRevisionTag> entity)
    {
        entity.ToTable("WikiRevisionTags");
        entity.HasKey(x => new { x.RevisionId, x.TagId });
        entity.HasIndex(x => x.TagId);
        entity.HasOne<WikiArticleRevision>().WithMany().HasForeignKey(x => x.RevisionId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<WikiTag>().WithMany().HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.Restrict);
    
    }
}
