using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaHaven.Domain.Knowledge.Entities;
namespace NovaHaven.Infrastructure.Persistence.Configurations.Knowledge;

public sealed class SeasonDefinitionConfiguration : IEntityTypeConfiguration<SeasonDefinition>
{
    public void Configure(EntityTypeBuilder<SeasonDefinition> entity)
    {
        entity.ToTable("KnowledgeSeasonDefinitions");
        entity.HasKey(x => x.EntryId);
        entity.Property(x => x.Theme).HasMaxLength(160).IsRequired();
        entity.Property(x => x.EventDescription).HasMaxLength(2_000);
        entity.HasOne<GameKnowledgeEntry>().WithMany().HasForeignKey(x => x.EntryId).OnDelete(DeleteBehavior.Restrict);
    
    }
}
