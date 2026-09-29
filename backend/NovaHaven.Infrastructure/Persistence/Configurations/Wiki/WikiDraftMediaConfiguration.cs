using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaHaven.Domain.Wiki.Entities;
namespace NovaHaven.Infrastructure.Persistence.Configurations.Wiki;

public sealed class WikiDraftMediaConfiguration : IEntityTypeConfiguration<WikiDraftMedia>
{
    public void Configure(EntityTypeBuilder<WikiDraftMedia> entity)
    {
        entity.ToTable("WikiDraftMedia");
        entity.HasKey(x => new { x.ArticleId, x.MediaId });
        entity.Property(x => x.AltText).HasMaxLength(200).IsRequired();
        entity.HasOne<WikiArticle>().WithMany().HasForeignKey(x => x.ArticleId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<WikiMedia>().WithMany().HasForeignKey(x => x.MediaId).OnDelete(DeleteBehavior.Restrict);
    
    }
}
