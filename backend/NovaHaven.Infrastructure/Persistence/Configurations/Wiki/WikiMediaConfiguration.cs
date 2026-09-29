using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaHaven.Domain.Wiki.Entities;
namespace NovaHaven.Infrastructure.Persistence.Configurations.Wiki;

public sealed class WikiMediaConfiguration : IEntityTypeConfiguration<WikiMedia>
{
    public void Configure(EntityTypeBuilder<WikiMedia> entity)
    {
        entity.ToTable("WikiMedia");
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => x.Sha256);
        entity.Property(x => x.StorageKey).HasMaxLength(120).IsRequired();
        entity.HasIndex(x => x.StorageKey).IsUnique();
        entity.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
        entity.Property(x => x.ContentType).HasMaxLength(64).IsRequired();
        entity.Property(x => x.Sha256).HasMaxLength(64).IsRequired();
    
    }
}
