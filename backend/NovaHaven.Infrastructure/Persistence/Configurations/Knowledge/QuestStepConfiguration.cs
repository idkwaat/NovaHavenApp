using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaHaven.Domain.Knowledge.Entities;
namespace NovaHaven.Infrastructure.Persistence.Configurations.Knowledge;

public sealed class QuestStepConfiguration : IEntityTypeConfiguration<QuestStep>
{
    public void Configure(EntityTypeBuilder<QuestStep> entity)
    {
        entity.ToTable("KnowledgeQuestSteps");
        entity.HasKey(x => new { x.QuestEntryId, x.Position });
        entity.Property(x => x.Title).HasMaxLength(160).IsRequired();
        entity.Property(x => x.Description).HasMaxLength(2_000).IsRequired();
        entity.HasOne<GameKnowledgeEntry>().WithMany().HasForeignKey(x => x.QuestEntryId).OnDelete(DeleteBehavior.Restrict);
    
    }
}
