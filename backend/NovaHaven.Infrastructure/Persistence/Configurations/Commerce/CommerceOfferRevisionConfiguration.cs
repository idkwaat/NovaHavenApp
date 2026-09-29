using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaHaven.Domain.Commerce.Entities;
namespace NovaHaven.Infrastructure.Persistence.Configurations.Commerce;

public sealed class CommerceOfferRevisionConfiguration : IEntityTypeConfiguration<CommerceOfferRevision>
{
    public void Configure(EntityTypeBuilder<CommerceOfferRevision> entity)
    {
        entity.ToTable("CommerceOfferRevisions");
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => new { x.OfferId, x.Number }).IsUnique();
        entity.Property(x => x.Slug).HasMaxLength(120).IsRequired();
        entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
        entity.Property(x => x.Summary).HasMaxLength(500).IsRequired();
        entity.Property(x => x.Markdown).HasColumnType("text").IsRequired();
        entity.Property(x => x.Kind).HasConversion<string>().HasMaxLength(20);
        entity.Property(x => x.DisplayPrice).HasMaxLength(80).IsRequired();
        entity.Property(x => x.ProviderProductCode).HasMaxLength(120);
        entity.Property(x => x.PriceMinorUnits).HasColumnType("bigint");
        entity.HasOne<CommerceOffer>().WithMany().HasForeignKey(x => x.OfferId).OnDelete(DeleteBehavior.Restrict);
    
    }
}
