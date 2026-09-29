using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaHaven.Domain.Commerce.Entities;
namespace NovaHaven.Infrastructure.Persistence.Configurations.Commerce;

public sealed class CommerceOrderConfiguration : IEntityTypeConfiguration<CommerceOrder>
{
    public void Configure(EntityTypeBuilder<CommerceOrder> entity)
    {
        entity.ToTable("CommerceOrders", table => table.HasCheckConstraint(
            "CK_CommerceOrders_TotalMinorUnits", "[TotalMinorUnits] > 0"));
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => x.OrderNumber).IsUnique();
        entity.HasIndex(x => x.IdempotencyKey).IsUnique();
        entity.Property(x => x.OrderNumber).HasMaxLength(40).IsRequired();
        entity.Property(x => x.RequestFingerprint).HasMaxLength(64).IsRequired();
        entity.Property(x => x.CurrencyCode).HasMaxLength(3).IsFixedLength().IsRequired();
        entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
    
    }
}
