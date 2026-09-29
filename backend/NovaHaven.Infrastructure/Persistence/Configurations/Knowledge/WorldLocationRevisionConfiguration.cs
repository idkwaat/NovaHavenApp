using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaHaven.Domain.Knowledge.Entities;
namespace NovaHaven.Infrastructure.Persistence.Configurations.Knowledge;

public sealed class WorldLocationRevisionConfiguration : IEntityTypeConfiguration<WorldLocationRevision>
{
    public void Configure(EntityTypeBuilder<WorldLocationRevision> entity)
    {
        entity.ToTable("KnowledgeWorldLocationRevisions");
        entity.HasKey(x => x.RevisionId);
        entity.Property(x => x.Region).HasMaxLength(120).IsRequired();
        entity.Property(x => x.LocationType).HasMaxLength(80).IsRequired();
        entity.Property(x => x.Latitude).HasPrecision(9, 6);
        entity.Property(x => x.Longitude).HasPrecision(9, 6);
        entity.Property(x => x.MapImageUrl).HasMaxLength(500);
        entity.HasOne<GameKnowledgeRevision>().WithMany().HasForeignKey(x => x.RevisionId).OnDelete(DeleteBehavior.Restrict);
    
    }
}
