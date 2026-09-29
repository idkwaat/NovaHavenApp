using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaHaven.Domain.Rewards.Entities;
namespace NovaHaven.Infrastructure.Persistence.Configurations.Rewards;

public sealed class RewardDefinitionRevisionConfiguration : IEntityTypeConfiguration<RewardDefinitionRevision>
{
    public void Configure(EntityTypeBuilder<RewardDefinitionRevision> entity)
    {
        entity.ToTable("RewardDefinitionRevisions");
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => new { x.DefinitionId, x.Number }).IsUnique();
        entity.Property(x => x.Slug).HasMaxLength(120).IsRequired();
        entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
        entity.Property(x => x.Summary).HasMaxLength(500).IsRequired();
        entity.Property(x => x.Markdown).HasColumnType("text").IsRequired();
        entity.Property(x => x.Kind).HasConversion<string>().HasMaxLength(20);
        entity.Property(x => x.DeliveryDescription).HasMaxLength(1000).IsRequired();
        entity.HasOne<RewardDefinition>().WithMany().HasForeignKey(x => x.DefinitionId).OnDelete(DeleteBehavior.Restrict);
    
    }
}
