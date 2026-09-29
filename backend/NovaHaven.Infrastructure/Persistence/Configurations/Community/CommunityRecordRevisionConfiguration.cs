using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaHaven.Domain.Community.Entities;
using NovaHaven.Domain.Knowledge.Entities;
namespace NovaHaven.Infrastructure.Persistence.Configurations.Community;

public sealed class CommunityRecordRevisionConfiguration : IEntityTypeConfiguration<CommunityRecordRevision>
{
    public void Configure(EntityTypeBuilder<CommunityRecordRevision> entity)
    {
        entity.ToTable("CommunityRecordRevisions");
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => new { x.RecordId, x.Number }).IsUnique();
        entity.Property(x => x.Kind).HasConversion<string>().HasMaxLength(20);
        entity.Property(x => x.Slug).HasMaxLength(160).IsRequired();
        entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
        entity.Property(x => x.Summary).HasMaxLength(500).IsRequired();
        entity.Property(x => x.Markdown).HasColumnType("nvarchar(max)").IsRequired();
        entity.Property(x => x.Motto).HasMaxLength(300).IsRequired();
        entity.Property(x => x.DiscordUrl).HasMaxLength(500).IsRequired();
        entity.Property(x => x.Handle).HasMaxLength(80).IsRequired();
        entity.Property(x => x.Bio).HasMaxLength(2_000).IsRequired();
        entity.Property(x => x.AvatarUrl).HasMaxLength(500).IsRequired();
        entity.Property(x => x.OwnerDisplayName).HasMaxLength(120).IsRequired();
        entity.Property(x => x.GalleryMarkdown).HasColumnType("nvarchar(max)").IsRequired();
        entity.Property(x => x.LeaderboardCategory).HasMaxLength(120).IsRequired();
        entity.HasOne<CommunityRecord>().WithMany().HasForeignKey(x => x.RecordId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<GameKnowledgeRevision>().WithMany().HasForeignKey(x => x.LocationRevisionId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<GameKnowledgeRevision>().WithMany().HasForeignKey(x => x.SeasonRevisionId).OnDelete(DeleteBehavior.Restrict);
    
    }
}
