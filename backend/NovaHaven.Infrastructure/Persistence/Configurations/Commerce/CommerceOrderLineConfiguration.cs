using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaHaven.Domain.Commerce.Entities;
namespace NovaHaven.Infrastructure.Persistence.Configurations.Commerce;

public sealed class CommerceOrderLineConfiguration : IEntityTypeConfiguration<CommerceOrderLine>
{
    public void Configure(EntityTypeBuilder<CommerceOrderLine> entity)
    {
        entity.ToTable("CommerceOrderLines", table => table.HasCheckConstraint(
            "CK_CommerceOrderLines_PositiveValues", "\"Quantity\" BETWEEN 1 AND 99 AND \"UnitPriceMinorUnits\" > 0 AND \"LineTotalMinorUnits\" > 0"));
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => new { x.OrderId, x.OfferId }).IsUnique();
        entity.Property(x => x.OfferSlug).HasMaxLength(120).IsRequired();
        entity.Property(x => x.OfferName).HasMaxLength(160).IsRequired();
        entity.HasOne<CommerceOrder>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<CommerceOffer>().WithMany().HasForeignKey(x => x.OfferId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<CommerceOfferRevision>().WithMany().HasForeignKey(x => x.OfferRevisionId).OnDelete(DeleteBehavior.Restrict);
    
    }
}
