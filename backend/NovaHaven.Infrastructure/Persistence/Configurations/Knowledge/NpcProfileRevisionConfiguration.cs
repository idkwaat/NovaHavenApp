using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaHaven.Domain.Knowledge.Entities;
namespace NovaHaven.Infrastructure.Persistence.Configurations.Knowledge;

public sealed class NpcProfileRevisionConfiguration : IEntityTypeConfiguration<NpcProfileRevision>
{
    public void Configure(EntityTypeBuilder<NpcProfileRevision> entity)
    {
        entity.ToTable("KnowledgeNpcProfileRevisions");
        entity.HasKey(x => x.RevisionId);
        entity.Property(x => x.Role).HasMaxLength(120).IsRequired();
        entity.Property(x => x.PortraitUrl).HasMaxLength(500);
        entity.HasOne<GameKnowledgeRevision>().WithMany().HasForeignKey(x => x.RevisionId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<GameKnowledgeRevision>().WithMany().HasForeignKey(x => x.LocationRevisionId).OnDelete(DeleteBehavior.Restrict);
    
    }
}
