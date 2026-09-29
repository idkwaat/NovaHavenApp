using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata;
using NovaHaven.Domain.Knowledge;
using NovaHaven.Infrastructure.Data;
using Xunit;

namespace NovaHaven.Integration.Tests;

public sealed class EfConfigurationDiscoveryTests
{
    [Fact]
    public void Every_domain_entity_has_an_assembly_discovered_configuration_and_is_mapped()
    {
        var domainAssembly = typeof(KnowledgeKind).Assembly;
        var infrastructureAssembly = typeof(NovaDbContext).Assembly;
        var domainEntities = domainAssembly.GetTypes()
            .Where(type => type.IsClass && type.IsPublic && type.Namespace?.EndsWith(".Entities", StringComparison.Ordinal) == true)
            .ToHashSet();
        var configuredEntities = infrastructureAssembly.GetTypes()
            .SelectMany(type => type.GetInterfaces())
            .Where(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>))
            .Select(type => type.GenericTypeArguments[0])
            .ToHashSet();

        Assert.Equal(domainEntities.OrderBy(type => type.FullName), configuredEntities.OrderBy(type => type.FullName));

        var options = new DbContextOptionsBuilder<NovaDbContext>()
            .UseNpgsql("Host=127.0.0.1;Database=NovaHaven_ModelOnly;Username=postgres;Password=not-used")
            .Options;
        using var context = new NovaDbContext(options);
        Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL", context.Database.ProviderName);
        var mappedEntities = context.Model.GetEntityTypes()
            .Where(entity => entity.ClrType.Assembly == domainAssembly)
            .Select(entity => entity.ClrType)
            .ToHashSet();

        Assert.Equal(domainEntities.OrderBy(type => type.FullName), mappedEntities.OrderBy(type => type.FullName));
    }

    [Fact]
    public void Active_migration_chain_is_the_PostgreSQL_baseline_and_versions_use_xmin()
    {
        var options = new DbContextOptionsBuilder<NovaDbContext>()
            .UseNpgsql("Host=127.0.0.1;Database=NovaHaven_ModelOnly;Username=postgres;Password=not-used")
            .Options;
        using var context = new NovaDbContext(options);

        var migrations = context.Database.GetMigrations().ToArray();
        Assert.Single(migrations);
        Assert.EndsWith("_InitialPostgreSql", migrations[0], StringComparison.Ordinal);

        var concurrencyProperties = context.Model.GetEntityTypes()
            .SelectMany(entity => entity.GetProperties())
            .Where(property => property.Name == "RowVersion")
            .ToArray();

        Assert.NotEmpty(concurrencyProperties);
        Assert.All(concurrencyProperties, property =>
        {
            Assert.Equal(typeof(uint), property.ClrType);
            Assert.True(property.IsConcurrencyToken);
            Assert.Equal(ValueGenerated.OnAddOrUpdate, property.ValueGenerated);
            Assert.Equal("xmin", property.GetColumnName());
        });
    }
}
