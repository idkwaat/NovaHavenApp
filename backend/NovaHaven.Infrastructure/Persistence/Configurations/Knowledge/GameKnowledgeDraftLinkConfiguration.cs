using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaHaven.Domain.Catalog.Entities;
using NovaHaven.Domain.Knowledge.Entities;
namespace NovaHaven.Infrastructure.Persistence.Configurations.Knowledge;

public sealed class GameKnowledgeDraftLinkConfiguration : IEntityTypeConfiguration<GameKnowledgeDraftLink>
{
    public void Configure(EntityTypeBuilder<GameKnowledgeDraftLink> entity)
    {
        entity.ToTable("KnowledgeDraftLinks");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.LinkType).HasConversion<string>().HasMaxLength(30);
        entity.HasIndex(x => new { x.EntryId, x.SortOrder });
        entity.HasOne<GameKnowledgeEntry>().WithMany().HasForeignKey(x => x.EntryId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<GameKnowledgeEntry>().WithMany().HasForeignKey(x => x.TargetEntryId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<GameCatalogItem>().WithMany().HasForeignKey(x => x.TargetCatalogItemId).OnDelete(DeleteBehavior.Restrict);
    
    }
}
