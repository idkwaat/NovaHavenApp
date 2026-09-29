using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaHaven.Domain.Notifications.Entities;
using NovaHaven.Infrastructure.Identity;

namespace NovaHaven.Infrastructure.Persistence.Configurations.Notifications;

public sealed class UserNotificationConfiguration : IEntityTypeConfiguration<UserNotification>
{
    public void Configure(EntityTypeBuilder<UserNotification> entity)
    {
        entity.ToTable("UserNotifications");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Title).HasMaxLength(120).IsRequired();
        entity.Property(x => x.Body).HasMaxLength(500).IsRequired();
        entity.Property(x => x.Href).HasMaxLength(200);
        entity.HasIndex(x => new { x.UserId, x.CreatedAtUtc });
        entity.HasIndex(x => new { x.UserId, x.ReadAtUtc });
        entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
