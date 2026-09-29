using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaHaven.Domain.Knowledge.Entities;
namespace NovaHaven.Infrastructure.Persistence.Configurations.Knowledge;

public sealed class SeasonDefinitionRevisionConfiguration : IEntityTypeConfiguration<SeasonDefinitionRevision>
{
    public void Configure(EntityTypeBuilder<SeasonDefinitionRevision> entity)
    {
        entity.ToTable("KnowledgeSeasonDefinitionRevisions");
        entity.HasKey(x => x.RevisionId);
        entity.Property(x => x.Theme).HasMaxLength(160).IsRequired();
        entity.Property(x => x.EventDescription).HasMaxLength(2_000);
        entity.HasOne<GameKnowledgeRevision>().WithMany().HasForeignKey(x => x.RevisionId).OnDelete(DeleteBehavior.Restrict);
    
    }
}
