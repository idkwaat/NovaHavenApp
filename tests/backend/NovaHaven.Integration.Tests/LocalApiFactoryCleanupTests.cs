using Microsoft.Data.SqlClient;
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
        await using var connection = new SqlConnection(MasterConnectionString());
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT [name] FROM sys.databases WHERE [name] LIKE N'NovaHaven_Integration[_]%';";

        await using var reader = await command.ExecuteReaderAsync();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (await reader.ReadAsync()) names.Add(reader.GetString(0));
        return names;
    }

    private static async Task DropDatabasesAsync(IEnumerable<string> databaseNames)
    {
        foreach (var name in databaseNames)
        {
            var identifier = $"[{name.Replace("]", "]]", StringComparison.Ordinal)}]";
            await using var connection = new SqlConnection(MasterConnectionString());
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = $"IF DB_ID(@name) IS NOT NULL BEGIN ALTER DATABASE {identifier} SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE {identifier}; END;";
            command.Parameters.AddWithValue("@name", name);
            await command.ExecuteNonQueryAsync();
        }
    }

    private static string MasterConnectionString() => new SqlConnectionStringBuilder
    {
        DataSource = @"(localdb)\MSSQLLocalDB",
        InitialCatalog = "master",
        IntegratedSecurity = true,
        TrustServerCertificate = true,
        ConnectTimeout = 15,
        Pooling = false
    }.ConnectionString;
}
