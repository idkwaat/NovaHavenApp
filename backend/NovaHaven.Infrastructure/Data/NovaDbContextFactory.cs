using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NovaHaven.Infrastructure.Data;

public sealed class NovaDbContextFactory : IDesignTimeDbContextFactory<NovaDbContext>
{
    public NovaDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("NOVA_DB_CONNECTION")
            ?? throw new InvalidOperationException("Set NOVA_DB_CONNECTION before creating EF migrations.");
        var options = new DbContextOptionsBuilder<NovaDbContext>().UseSqlServer(connection).Options;
        return new NovaDbContext(options);
    }
}
