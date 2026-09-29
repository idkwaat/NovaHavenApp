using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaHaven.Domain.Community.Entities;
namespace NovaHaven.Infrastructure.Persistence.Configurations.Community;

public sealed class CommunityLeaderboardDraftRowConfiguration : IEntityTypeConfiguration<CommunityLeaderboardDraftRow>
{
    public void Configure(EntityTypeBuilder<CommunityLeaderboardDraftRow> entity)
    {
        entity.ToTable("CommunityLeaderboardDraftRows");
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => new { x.RecordId, x.Rank }).IsUnique();
        entity.Property(x => x.ParticipantName).HasMaxLength(120).IsRequired();
        entity.Property(x => x.Score).HasPrecision(18, 2);
        entity.Property(x => x.Note).HasMaxLength(300).IsRequired();
        entity.HasOne<CommunityRecord>().WithMany().HasForeignKey(x => x.RecordId).OnDelete(DeleteBehavior.Restrict);
    
    }
}
