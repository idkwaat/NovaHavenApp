using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaHaven.Domain.Wiki.Entities;
namespace NovaHaven.Infrastructure.Persistence.Configurations.Wiki;

public sealed class WikiArticleRevisionConfiguration : IEntityTypeConfiguration<WikiArticleRevision>
{
    public void Configure(EntityTypeBuilder<WikiArticleRevision> entity)
    {
        entity.ToTable("WikiArticleRevisions");
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => new { x.ArticleId, x.Number }).IsUnique();
        entity.Property(x => x.Title).HasMaxLength(120).IsRequired();
        entity.Property(x => x.Summary).HasMaxLength(300).IsRequired();
        entity.Property(x => x.Markdown).HasColumnType("nvarchar(max)").IsRequired();
        entity.HasOne<WikiArticle>().WithMany().HasForeignKey(x => x.ArticleId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<WikiCategory>().WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
    
    }
}
