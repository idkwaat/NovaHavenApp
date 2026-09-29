using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaHaven.Domain.Wiki.Entities;
namespace NovaHaven.Infrastructure.Persistence.Configurations.Wiki;

public sealed class WikiRevisionMediaConfiguration : IEntityTypeConfiguration<WikiRevisionMedia>
{
    public void Configure(EntityTypeBuilder<WikiRevisionMedia> entity)
    {
        entity.ToTable("WikiRevisionMedia");
        entity.HasKey(x => new { x.RevisionId, x.MediaId });
        entity.Property(x => x.AltText).HasMaxLength(200).IsRequired();
        entity.HasOne<WikiArticleRevision>().WithMany().HasForeignKey(x => x.RevisionId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<WikiMedia>().WithMany().HasForeignKey(x => x.MediaId).OnDelete(DeleteBehavior.Restrict);
    
    }
}
