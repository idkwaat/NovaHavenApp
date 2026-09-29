using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaHaven.Domain.Knowledge.Entities;
namespace NovaHaven.Infrastructure.Persistence.Configurations.Knowledge;

public sealed class QuestStepRevisionConfiguration : IEntityTypeConfiguration<QuestStepRevision>
{
    public void Configure(EntityTypeBuilder<QuestStepRevision> entity)
    {
        entity.ToTable("KnowledgeQuestStepRevisions");
        entity.HasKey(x => new { x.QuestRevisionId, x.Position });
        entity.Property(x => x.Title).HasMaxLength(160).IsRequired();
        entity.Property(x => x.Description).HasMaxLength(2_000).IsRequired();
        entity.HasOne<GameKnowledgeRevision>().WithMany().HasForeignKey(x => x.QuestRevisionId).OnDelete(DeleteBehavior.Restrict);
    
    }
}
