using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaHaven.Domain.Integration.Entities;
namespace NovaHaven.Infrastructure.Persistence.Configurations.Integration;

public sealed class IntegrationCapabilityConfiguration : IEntityTypeConfiguration<IntegrationCapability>
{
    public void Configure(EntityTypeBuilder<IntegrationCapability> entity)
    {
        entity.ToTable("IntegrationCapabilities");
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => x.CapabilityKey).IsUnique();
        entity.Property(x => x.CapabilityKey).HasMaxLength(80).IsRequired();
        entity.Property(x => x.DisplayName).HasMaxLength(160).IsRequired();
        entity.Property(x => x.Owner).HasMaxLength(80).IsRequired();
        entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        entity.Property(x => x.SafeMessage).HasMaxLength(500).IsRequired();
        entity.Property(x => x.RowVersion).IsRowVersion();
    
    }
}
