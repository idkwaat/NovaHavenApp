using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaHaven.Domain.Wiki.Entities;
namespace NovaHaven.Infrastructure.Persistence.Configurations.Wiki;

public sealed class WikiDraftTagConfiguration : IEntityTypeConfiguration<WikiDraftTag>
{
    public void Configure(EntityTypeBuilder<WikiDraftTag> entity)
    {
        entity.ToTable("WikiDraftTags");
        entity.HasKey(x => new { x.ArticleId, x.TagId });
        entity.HasIndex(x => x.TagId);
        entity.HasOne<WikiArticle>().WithMany().HasForeignKey(x => x.ArticleId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<WikiTag>().WithMany().HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.Restrict);
    
    }
}
