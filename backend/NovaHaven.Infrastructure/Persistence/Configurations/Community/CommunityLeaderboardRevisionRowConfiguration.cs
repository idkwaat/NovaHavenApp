using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaHaven.Domain.Community.Entities;
namespace NovaHaven.Infrastructure.Persistence.Configurations.Community;

public sealed class CommunityLeaderboardRevisionRowConfiguration : IEntityTypeConfiguration<CommunityLeaderboardRevisionRow>
{
    public void Configure(EntityTypeBuilder<CommunityLeaderboardRevisionRow> entity)
    {
        entity.ToTable("CommunityLeaderboardRevisionRows");
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => new { x.RevisionId, x.Rank }).IsUnique();
        entity.Property(x => x.ParticipantName).HasMaxLength(120).IsRequired();
        entity.Property(x => x.Score).HasPrecision(18, 2);
        entity.Property(x => x.Note).HasMaxLength(300).IsRequired();
        entity.HasOne<CommunityRecordRevision>().WithMany().HasForeignKey(x => x.RevisionId).OnDelete(DeleteBehavior.Restrict);
    
    }
}
