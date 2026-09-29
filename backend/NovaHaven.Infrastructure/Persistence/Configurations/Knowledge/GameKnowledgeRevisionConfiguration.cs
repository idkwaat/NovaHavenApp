using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaHaven.Domain.Knowledge.Entities;
namespace NovaHaven.Infrastructure.Persistence.Configurations.Knowledge;

public sealed class GameKnowledgeRevisionConfiguration : IEntityTypeConfiguration<GameKnowledgeRevision>
{
    public void Configure(EntityTypeBuilder<GameKnowledgeRevision> entity)
    {
        entity.ToTable("KnowledgeRevisions");
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => new { x.EntryId, x.Number }).IsUnique();
        entity.Property(x => x.Kind).HasConversion<string>().HasMaxLength(20);
        entity.Property(x => x.Slug).HasMaxLength(160).IsRequired();
        entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
        entity.Property(x => x.Summary).HasMaxLength(500).IsRequired();
        entity.Property(x => x.Markdown).HasColumnType("text").IsRequired();
        entity.HasOne<GameKnowledgeEntry>().WithMany().HasForeignKey(x => x.EntryId).OnDelete(DeleteBehavior.Restrict);
    
    }
}
