using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaHaven.Domain.Knowledge.Entities;
namespace NovaHaven.Infrastructure.Persistence.Configurations.Knowledge;

public sealed class NpcProfileConfiguration : IEntityTypeConfiguration<NpcProfile>
{
    public void Configure(EntityTypeBuilder<NpcProfile> entity)
    {
        entity.ToTable("KnowledgeNpcProfiles");
        entity.HasKey(x => x.EntryId);
        entity.Property(x => x.Role).HasMaxLength(120).IsRequired();
        entity.Property(x => x.PortraitUrl).HasMaxLength(500);
        entity.HasOne<GameKnowledgeEntry>().WithMany().HasForeignKey(x => x.EntryId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<GameKnowledgeEntry>().WithMany().HasForeignKey(x => x.LocationEntryId).OnDelete(DeleteBehavior.Restrict);
    
    }
}
