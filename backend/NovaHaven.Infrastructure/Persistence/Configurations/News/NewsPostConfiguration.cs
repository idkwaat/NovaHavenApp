using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaHaven.Domain.News.Entities;
namespace NovaHaven.Infrastructure.Persistence.Configurations.News;

public sealed class NewsPostConfiguration : IEntityTypeConfiguration<NewsPost>
{
    public void Configure(EntityTypeBuilder<NewsPost> entity)
    {
        entity.ToTable("NewsPosts");
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => x.Slug).IsUnique();
        entity.Property(x => x.Slug).HasMaxLength(120).IsRequired();
        entity.Property(x => x.DraftTitle).HasMaxLength(160).IsRequired();
        entity.Property(x => x.DraftSummary).HasMaxLength(500).IsRequired();
        entity.Property(x => x.DraftMarkdown).HasColumnType("text").IsRequired();
        entity.Property(x => x.State).HasConversion<string>().HasMaxLength(20);
        entity.Property(x => x.RowVersion).IsRowVersion();
    
    }
}
