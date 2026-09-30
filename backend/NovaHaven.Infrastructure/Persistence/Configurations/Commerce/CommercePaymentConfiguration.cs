using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaHaven.Domain.Commerce.Entities;
namespace NovaHaven.Infrastructure.Persistence.Configurations.Commerce;

public sealed class CommercePaymentConfiguration : IEntityTypeConfiguration<CommercePayment>
{
    public void Configure(EntityTypeBuilder<CommercePayment> entity)
    {
        entity.ToTable("CommercePayments", table => table.HasCheckConstraint(
            "CK_CommercePayments_PositiveAmount", "\"AmountMinorUnits\" > 0"));
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => x.OrderId).IsUnique();
        entity.Property(x => x.Method).HasConversion<string>().HasMaxLength(30).IsRequired();
        entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        entity.Property(x => x.CurrencyCode).HasMaxLength(3).IsFixedLength().IsRequired();
        entity.HasOne<CommerceOrder>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
    
    }
}
