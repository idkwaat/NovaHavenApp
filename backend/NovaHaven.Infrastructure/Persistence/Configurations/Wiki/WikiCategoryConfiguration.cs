using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaHaven.Domain.Wiki.Entities;
namespace NovaHaven.Infrastructure.Persistence.Configurations.Wiki;

public sealed class WikiCategoryConfiguration : IEntityTypeConfiguration<WikiCategory>
{
    public void Configure(EntityTypeBuilder<WikiCategory> entity)
    {
        entity.ToTable("WikiCategories");
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => x.Slug).IsUnique();
        entity.HasIndex(x => x.NormalizedName).IsUnique();
        entity.Property(x => x.Name).HasMaxLength(80).IsRequired();
        entity.Property(x => x.NormalizedName).HasMaxLength(80).IsRequired();
        entity.Property(x => x.Slug).HasMaxLength(80).IsRequired();
        entity.Property(x => x.RowVersion).IsRowVersion();
    
    }
}
