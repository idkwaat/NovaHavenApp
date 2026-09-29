using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaHaven.Domain.Wiki.Entities;
namespace NovaHaven.Infrastructure.Persistence.Configurations.Wiki;

public sealed class WikiAuditEventConfiguration : IEntityTypeConfiguration<WikiAuditEvent>
{
    public void Configure(EntityTypeBuilder<WikiAuditEvent> entity)
    {
        entity.ToTable("WikiAuditEvents");
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => new { x.EntityType, x.EntityId, x.OccurredAt });
        entity.HasIndex(x => x.OccurredAt);
        entity.Property(x => x.Action).HasMaxLength(64).IsRequired();
        entity.Property(x => x.EntityType).HasMaxLength(64).IsRequired();
        entity.Property(x => x.DetailsJson).HasMaxLength(4000).IsRequired();
    
    }
}
