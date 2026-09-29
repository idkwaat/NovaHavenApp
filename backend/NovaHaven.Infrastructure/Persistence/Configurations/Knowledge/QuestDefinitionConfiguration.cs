using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaHaven.Domain.Knowledge.Entities;
namespace NovaHaven.Infrastructure.Persistence.Configurations.Knowledge;

public sealed class QuestDefinitionConfiguration : IEntityTypeConfiguration<QuestDefinition>
{
    public void Configure(EntityTypeBuilder<QuestDefinition> entity)
    {
        entity.ToTable("KnowledgeQuestDefinitions");
        entity.HasKey(x => x.EntryId);
        entity.Property(x => x.RewardDescription).HasMaxLength(500).IsRequired();
        entity.HasOne<GameKnowledgeEntry>().WithMany().HasForeignKey(x => x.EntryId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<GameKnowledgeEntry>().WithMany().HasForeignKey(x => x.GiverNpcEntryId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<GameKnowledgeEntry>().WithMany().HasForeignKey(x => x.LocationEntryId).OnDelete(DeleteBehavior.Restrict);
    
    }
}
