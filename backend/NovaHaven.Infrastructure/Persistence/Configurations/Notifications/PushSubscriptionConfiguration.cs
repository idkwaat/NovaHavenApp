using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaHaven.Domain.Notifications.Entities;
using NovaHaven.Infrastructure.Identity;

namespace NovaHaven.Infrastructure.Persistence.Configurations.Notifications;

public sealed class PushSubscriptionConfiguration : IEntityTypeConfiguration<PushSubscription>
{
    public void Configure(EntityTypeBuilder<PushSubscription> entity)
    {
        entity.ToTable("PushSubscriptions");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Endpoint).HasMaxLength(2048).IsRequired();
        entity.Property(x => x.EndpointHash).HasMaxLength(64).IsUnicode(false).IsRequired();
        entity.Property(x => x.P256dh).HasMaxLength(128).IsRequired();
        entity.Property(x => x.Auth).HasMaxLength(64).IsRequired();
        entity.HasIndex(x => x.EndpointHash).IsUnique();
        entity.HasIndex(x => x.UserId);
        entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
