using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaHaven.Domain.Community.Entities;
namespace NovaHaven.Infrastructure.Persistence.Configurations.Community;

public sealed class CommunityEventRegistrationConfiguration : IEntityTypeConfiguration<CommunityEventRegistration>
{
    public void Configure(EntityTypeBuilder<CommunityEventRegistration> entity)
    {
        entity.ToTable("CommunityEventRegistrations");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.DisplayName).HasMaxLength(120).IsRequired();
        entity.Property(x => x.Contact).HasMaxLength(200).IsRequired();
        entity.Property(x => x.State).HasConversion<string>().HasMaxLength(20);
        entity.Property(x => x.RowVersion).IsRowVersion();
        entity.HasIndex(x => new { x.EventRecordId, x.DisplayName });
        entity.HasOne<CommunityRecord>().WithMany().HasForeignKey(x => x.EventRecordId).OnDelete(DeleteBehavior.Restrict);
    
    }
}
