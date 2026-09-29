using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaHaven.Domain.Catalog.Entities;
namespace NovaHaven.Infrastructure.Persistence.Configurations.Catalog;

public sealed class GameRecipeRevisionConfiguration : IEntityTypeConfiguration<GameRecipeRevision>
{
    public void Configure(EntityTypeBuilder<GameRecipeRevision> entity)
    {
        entity.ToTable("CatalogRecipeRevisions");
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => new { x.RecipeId, x.Number }).IsUnique();
        entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
        entity.Property(x => x.Summary).HasMaxLength(500).IsRequired();
        entity.Property(x => x.Markdown).HasColumnType("nvarchar(max)").IsRequired();
        entity.HasOne<GameRecipe>().WithMany().HasForeignKey(x => x.RecipeId).OnDelete(DeleteBehavior.Restrict);
    
    }
}
