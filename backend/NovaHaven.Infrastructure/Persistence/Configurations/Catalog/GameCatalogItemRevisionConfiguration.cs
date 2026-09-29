using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaHaven.Domain.Catalog.Entities;
namespace NovaHaven.Infrastructure.Persistence.Configurations.Catalog;

public sealed class GameCatalogItemRevisionConfiguration : IEntityTypeConfiguration<GameCatalogItemRevision>
{
    public void Configure(EntityTypeBuilder<GameCatalogItemRevision> entity)
    {
        entity.ToTable("CatalogItemRevisions");
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => new { x.ItemId, x.Number }).IsUnique();
        entity.Property(x => x.Slug).HasMaxLength(120).IsRequired();
        entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
        entity.Property(x => x.Summary).HasMaxLength(500).IsRequired();
        entity.Property(x => x.Markdown).HasColumnType("nvarchar(max)").IsRequired();
        entity.Property(x => x.Kind).HasConversion<string>().HasMaxLength(20);
        entity.HasOne<GameCatalogItem>().WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
    
    }
}
