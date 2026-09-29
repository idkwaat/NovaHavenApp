using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
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
            .UseSqlServer("Server=localhost;Database=NovaHaven_ModelOnly;Integrated Security=True;TrustServerCertificate=True")
            .Options;
        using var context = new NovaDbContext(options);
        var mappedEntities = context.Model.GetEntityTypes()
            .Where(entity => entity.ClrType.Assembly == domainAssembly)
            .Select(entity => entity.ClrType)
            .ToHashSet();

        Assert.Equal(domainEntities.OrderBy(type => type.FullName), mappedEntities.OrderBy(type => type.FullName));
    }
}
