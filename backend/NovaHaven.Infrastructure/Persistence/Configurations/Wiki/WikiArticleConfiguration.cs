using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaHaven.Domain.Wiki.Entities;
namespace NovaHaven.Infrastructure.Persistence.Configurations.Wiki;

public sealed class WikiArticleConfiguration : IEntityTypeConfiguration<WikiArticle>
{
    public void Configure(EntityTypeBuilder<WikiArticle> entity)
    {
        entity.ToTable("WikiArticles");
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => x.Slug).IsUnique();
        entity.Property(x => x.Slug).HasMaxLength(120).IsRequired();
        entity.Property(x => x.DraftTitle).HasMaxLength(120).IsRequired();
        entity.Property(x => x.DraftSummary).HasMaxLength(300).IsRequired();
        entity.Property(x => x.DraftMarkdown).HasColumnType("text").IsRequired();
        entity.Property(x => x.State).HasConversion<string>().HasMaxLength(20);
        entity.Property(x => x.RowVersion).IsRowVersion();
        entity.HasOne<WikiCategory>().WithMany().HasForeignKey(x => x.DraftCategoryId).OnDelete(DeleteBehavior.Restrict);
    
    }
}
