using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaHaven.Domain.Community.Entities;
using NovaHaven.Domain.Knowledge.Entities;
namespace NovaHaven.Infrastructure.Persistence.Configurations.Community;

public sealed class CommunityRecordConfiguration : IEntityTypeConfiguration<CommunityRecord>
{
    public void Configure(EntityTypeBuilder<CommunityRecord> entity)
    {
        entity.ToTable("CommunityRecords");
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => x.Slug).IsUnique();
        entity.Property(x => x.Slug).HasMaxLength(160).IsRequired();
        entity.Property(x => x.DraftName).HasMaxLength(160).IsRequired();
        entity.Property(x => x.DraftSummary).HasMaxLength(500).IsRequired();
        entity.Property(x => x.DraftMarkdown).HasColumnType("nvarchar(max)").IsRequired();
        entity.Property(x => x.Kind).HasConversion<string>().HasMaxLength(20);
        entity.Property(x => x.State).HasConversion<string>().HasMaxLength(20);
        entity.Property(x => x.DraftMotto).HasMaxLength(300).IsRequired();
        entity.Property(x => x.DraftDiscordUrl).HasMaxLength(500).IsRequired();
        entity.Property(x => x.DraftHandle).HasMaxLength(80).IsRequired();
        entity.Property(x => x.DraftBio).HasMaxLength(2_000).IsRequired();
        entity.Property(x => x.DraftAvatarUrl).HasMaxLength(500).IsRequired();
        entity.Property(x => x.DraftOwnerDisplayName).HasMaxLength(120).IsRequired();
        entity.Property(x => x.DraftGalleryMarkdown).HasColumnType("nvarchar(max)").IsRequired();
        entity.Property(x => x.DraftLeaderboardCategory).HasMaxLength(120).IsRequired();
        entity.Property(x => x.RowVersion).IsRowVersion();
        entity.HasOne<GameKnowledgeEntry>().WithMany().HasForeignKey(x => x.DraftLocationEntryId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<GameKnowledgeEntry>().WithMany().HasForeignKey(x => x.DraftSeasonEntryId).OnDelete(DeleteBehavior.Restrict);
    
    }
}
