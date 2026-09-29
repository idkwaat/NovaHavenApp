using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaHaven.Domain.Knowledge.Entities;
namespace NovaHaven.Infrastructure.Persistence.Configurations.Knowledge;

public sealed class WorldLocationConfiguration : IEntityTypeConfiguration<WorldLocation>
{
    public void Configure(EntityTypeBuilder<WorldLocation> entity)
    {
        entity.ToTable("KnowledgeWorldLocations");
        entity.HasKey(x => x.EntryId);
        entity.Property(x => x.Region).HasMaxLength(120).IsRequired();
        entity.Property(x => x.LocationType).HasMaxLength(80).IsRequired();
        entity.Property(x => x.Latitude).HasPrecision(9, 6);
        entity.Property(x => x.Longitude).HasPrecision(9, 6);
        entity.Property(x => x.MapImageUrl).HasMaxLength(500);
        entity.HasOne<GameKnowledgeEntry>().WithMany().HasForeignKey(x => x.EntryId).OnDelete(DeleteBehavior.Restrict);
    
    }
}
