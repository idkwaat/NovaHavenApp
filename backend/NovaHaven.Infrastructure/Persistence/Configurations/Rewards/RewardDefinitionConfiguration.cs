using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaHaven.Domain.Rewards.Entities;
namespace NovaHaven.Infrastructure.Persistence.Configurations.Rewards;

public sealed class RewardDefinitionConfiguration : IEntityTypeConfiguration<RewardDefinition>
{
    public void Configure(EntityTypeBuilder<RewardDefinition> entity)
    {
        entity.ToTable("RewardDefinitions");
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => x.Slug).IsUnique();
        entity.Property(x => x.Slug).HasMaxLength(120).IsRequired();
        entity.Property(x => x.DraftName).HasMaxLength(160).IsRequired();
        entity.Property(x => x.DraftSummary).HasMaxLength(500).IsRequired();
        entity.Property(x => x.DraftMarkdown).HasColumnType("nvarchar(max)").IsRequired();
        entity.Property(x => x.DraftKind).HasConversion<string>().HasMaxLength(20);
        entity.Property(x => x.DraftDeliveryDescription).HasMaxLength(1000).IsRequired();
        entity.Property(x => x.State).HasConversion<string>().HasMaxLength(20);
        entity.Property(x => x.RowVersion).IsRowVersion();
    
    }
}
