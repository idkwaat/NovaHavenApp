using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaHaven.Domain.Commerce.Entities;
namespace NovaHaven.Infrastructure.Persistence.Configurations.Commerce;

public sealed class CommerceOfferConfiguration : IEntityTypeConfiguration<CommerceOffer>
{
    public void Configure(EntityTypeBuilder<CommerceOffer> entity)
    {
        entity.ToTable("CommerceOffers");
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => x.Slug).IsUnique();
        entity.Property(x => x.Slug).HasMaxLength(120).IsRequired();
        entity.Property(x => x.DraftName).HasMaxLength(160).IsRequired();
        entity.Property(x => x.DraftSummary).HasMaxLength(500).IsRequired();
        entity.Property(x => x.DraftMarkdown).HasColumnType("text").IsRequired();
        entity.Property(x => x.DraftKind).HasConversion<string>().HasMaxLength(20);
        entity.Property(x => x.DraftDisplayPrice).HasMaxLength(80).IsRequired();
        entity.Property(x => x.DraftProviderProductCode).HasMaxLength(120);
        entity.Property(x => x.DraftPriceMinorUnits).HasColumnType("bigint");
        entity.Property(x => x.State).HasConversion<string>().HasMaxLength(20);
        entity.Property(x => x.RowVersion).IsRowVersion();
    
    }
}
