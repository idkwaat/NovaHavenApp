using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaHaven.Domain.Catalog.Entities;
using NovaHaven.Domain.Knowledge.Entities;
namespace NovaHaven.Infrastructure.Persistence.Configurations.Knowledge;

public sealed class GameKnowledgeRevisionLinkConfiguration : IEntityTypeConfiguration<GameKnowledgeRevisionLink>
{
    public void Configure(EntityTypeBuilder<GameKnowledgeRevisionLink> entity)
    {
        entity.ToTable("KnowledgeRevisionLinks");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.LinkType).HasConversion<string>().HasMaxLength(30);
        entity.Property(x => x.TargetSlug).HasMaxLength(160).IsRequired();
        entity.Property(x => x.TargetName).HasMaxLength(160).IsRequired();
        entity.Property(x => x.TargetType).HasMaxLength(30).IsRequired();
        entity.HasIndex(x => new { x.RevisionId, x.SortOrder });
        entity.HasOne<GameKnowledgeRevision>().WithMany().HasForeignKey(x => x.RevisionId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<GameKnowledgeRevision>().WithMany().HasForeignKey(x => x.TargetRevisionId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<GameCatalogItemRevision>().WithMany().HasForeignKey(x => x.TargetCatalogItemRevisionId).OnDelete(DeleteBehavior.Restrict);
    
    }
}
