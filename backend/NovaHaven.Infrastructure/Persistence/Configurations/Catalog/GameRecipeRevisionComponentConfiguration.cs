using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaHaven.Domain.Catalog.Entities;
namespace NovaHaven.Infrastructure.Persistence.Configurations.Catalog;

public sealed class GameRecipeRevisionComponentConfiguration : IEntityTypeConfiguration<GameRecipeRevisionComponent>
{
    public void Configure(EntityTypeBuilder<GameRecipeRevisionComponent> entity)
    {
        entity.ToTable("CatalogRecipeRevisionComponents");
        entity.HasKey(x => new { x.RecipeRevisionId, x.CatalogItemRevisionId, x.Role });
        entity.Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
        entity.HasOne<GameRecipeRevision>().WithMany().HasForeignKey(x => x.RecipeRevisionId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<GameCatalogItemRevision>().WithMany().HasForeignKey(x => x.CatalogItemRevisionId).OnDelete(DeleteBehavior.Restrict);
    
    }
}
