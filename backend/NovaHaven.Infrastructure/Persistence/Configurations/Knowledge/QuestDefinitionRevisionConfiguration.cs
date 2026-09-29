using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaHaven.Domain.Knowledge.Entities;
namespace NovaHaven.Infrastructure.Persistence.Configurations.Knowledge;

public sealed class QuestDefinitionRevisionConfiguration : IEntityTypeConfiguration<QuestDefinitionRevision>
{
    public void Configure(EntityTypeBuilder<QuestDefinitionRevision> entity)
    {
        entity.ToTable("KnowledgeQuestDefinitionRevisions");
        entity.HasKey(x => x.RevisionId);
        entity.Property(x => x.RewardDescription).HasMaxLength(500).IsRequired();
        entity.HasOne<GameKnowledgeRevision>().WithMany().HasForeignKey(x => x.RevisionId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<GameKnowledgeRevision>().WithMany().HasForeignKey(x => x.GiverNpcRevisionId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<GameKnowledgeRevision>().WithMany().HasForeignKey(x => x.LocationRevisionId).OnDelete(DeleteBehavior.Restrict);
    
    }
}
