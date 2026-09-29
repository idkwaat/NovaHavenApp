using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaHaven.Domain.Catalog.Entities;
namespace NovaHaven.Infrastructure.Persistence.Configurations.Catalog;

public sealed class GameRecipeDraftComponentConfiguration : IEntityTypeConfiguration<GameRecipeDraftComponent>
{
    public void Configure(EntityTypeBuilder<GameRecipeDraftComponent> entity)
    {
        entity.ToTable("CatalogRecipeDraftComponents");
        entity.HasKey(x => new { x.RecipeId, x.CatalogItemId, x.Role });
        entity.Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
        entity.HasOne<GameRecipe>().WithMany().HasForeignKey(x => x.RecipeId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<GameCatalogItem>().WithMany().HasForeignKey(x => x.CatalogItemId).OnDelete(DeleteBehavior.Restrict);
    
    }
}
