using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaHaven.Domain.Catalog.Entities;
namespace NovaHaven.Infrastructure.Persistence.Configurations.Catalog;

public sealed class GameRecipeConfiguration : IEntityTypeConfiguration<GameRecipe>
{
    public void Configure(EntityTypeBuilder<GameRecipe> entity)
    {
        entity.ToTable("CatalogRecipes");
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => x.Slug).IsUnique();
        entity.Property(x => x.Slug).HasMaxLength(120).IsRequired();
        entity.Property(x => x.DraftName).HasMaxLength(160).IsRequired();
        entity.Property(x => x.DraftSummary).HasMaxLength(500).IsRequired();
        entity.Property(x => x.DraftMarkdown).HasColumnType("nvarchar(max)").IsRequired();
        entity.Property(x => x.State).HasConversion<string>().HasMaxLength(20);
        entity.Property(x => x.RowVersion).IsRowVersion();
    
    }
}
