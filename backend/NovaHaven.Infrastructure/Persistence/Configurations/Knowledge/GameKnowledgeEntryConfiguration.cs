using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaHaven.Domain.Knowledge.Entities;
namespace NovaHaven.Infrastructure.Persistence.Configurations.Knowledge;

public sealed class GameKnowledgeEntryConfiguration : IEntityTypeConfiguration<GameKnowledgeEntry>
{
    public void Configure(EntityTypeBuilder<GameKnowledgeEntry> entity)
    {
        entity.ToTable("KnowledgeEntries");
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => x.Slug).IsUnique();
        entity.Property(x => x.Slug).HasMaxLength(160).IsRequired();
        entity.Property(x => x.DraftName).HasMaxLength(160).IsRequired();
        entity.Property(x => x.DraftSummary).HasMaxLength(500).IsRequired();
        entity.Property(x => x.DraftMarkdown).HasColumnType("text").IsRequired();
        entity.Property(x => x.Kind).HasConversion<string>().HasMaxLength(20);
        entity.Property(x => x.State).HasConversion<string>().HasMaxLength(20);
        entity.Property(x => x.RowVersion).IsRowVersion();
    
    }
}
