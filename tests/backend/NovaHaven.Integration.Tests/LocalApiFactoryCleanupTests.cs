using System.Text.RegularExpressions;
using Npgsql;
using Xunit;

namespace NovaHaven.Integration.Tests;

public sealed class LocalApiFactoryCleanupTests
{
    [Fact]
    public async Task Disposing_factory_removes_only_its_temporary_integration_database()
    {
        var databasesBefore = await ReadIntegrationDatabaseNamesAsync();
        var factory = new LocalApiFactory();
        var databasesCreatedByTest = Array.Empty<string>();

        try
        {
            try
            {
                await factory.InitializeAsync();
            }
            finally
            {
                await ((IAsyncLifetime)factory).DisposeAsync();
            }

            var databasesAfter = await ReadIntegrationDatabaseNamesAsync();
            databasesCreatedByTest = databasesAfter.Except(databasesBefore, StringComparer.OrdinalIgnoreCase).ToArray();

            Assert.Empty(databasesCreatedByTest);
        }
        finally
        {
            if (databasesCreatedByTest.Length == 0)
            {
                var currentDatabases = await ReadIntegrationDatabaseNamesAsync();
                databasesCreatedByTest = currentDatabases.Except(databasesBefore, StringComparer.OrdinalIgnoreCase).ToArray();
            }

            await DropDatabasesAsync(databasesCreatedByTest);
        }
    }

    private static async Task<HashSet<string>> ReadIntegrationDatabaseNamesAsync()
    {
        await using var connection = new NpgsqlConnection(AdminConnectionString());
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT datname FROM pg_database WHERE datname ~ '^NovaHaven_Integration_[0-9a-f]{32}$';";

        await using var reader = await command.ExecuteReaderAsync();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (await reader.ReadAsync())
        {
            var name = reader.GetString(0);
            if (Regex.IsMatch(name, @"\ANovaHaven_Integration_[0-9a-f]{32}\z", RegexOptions.CultureInvariant))
                names.Add(name);
        }
        return names;
    }

    private static async Task DropDatabasesAsync(IEnumerable<string> databaseNames)
    {
        foreach (var name in databaseNames)
        {
            if (!Regex.IsMatch(name, @"\ANovaHaven_Integration_[0-9a-f]{32}\z", RegexOptions.CultureInvariant))
                throw new InvalidOperationException("Refusing to drop a database outside the integration-test naming pattern.");

            await using var connection = new NpgsqlConnection(AdminConnectionString());
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = $"DROP DATABASE IF EXISTS \"{name}\" WITH (FORCE);";
            await command.ExecuteNonQueryAsync();
        }
    }

    private static string AdminConnectionString()
    {
        var connection = Environment.GetEnvironmentVariable("NOVA_HAVEN_TEST_ADMIN_CONNECTION");
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException(
                "Set NOVA_HAVEN_TEST_ADMIN_CONNECTION to a local PostgreSQL admin connection for disposable integration databases.");

        return new NpgsqlConnectionStringBuilder(connection)
        {
            Database = "postgres",
            Pooling = false,
            Timeout = 15
        }.ConnectionString;
    }
}
