using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NovaHaven.Infrastructure.Data;
using Xunit;

namespace NovaHaven.Integration.Tests;

public sealed class OperationsApiIntegrationTests : IClassFixture<LocalApiFactory>
{
    private readonly LocalApiFactory factory;

    public OperationsApiIntegrationTests(LocalApiFactory factory) => this.factory = factory;

    [Fact]
    public async Task Diagnostics_requires_admin_and_reports_a_healthy_migrated_local_database()
    {
        using var anonymous = factory.CreateClient();
        using var denied = await anonymous.GetAsync("/api/v1/admin/operations/diagnostics");
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);

        using var client = await factory.CreateAuthenticatedClientAsync(factory.AdminEmail);
        using var response = await client.GetAsync("/api/v1/admin/operations/diagnostics");
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        var database = json.RootElement.GetProperty("database");
        Assert.True(database.GetProperty("canConnect").GetBoolean());
        Assert.Equal(0, database.GetProperty("pendingMigrations").GetInt32());
        Assert.True(database.GetProperty("appliedMigrations").GetInt32() >= 10);
        Assert.True(json.RootElement.GetProperty("content").GetProperty("wikiArticles").GetInt32() >= 0);
        Assert.Equal(JsonValueKind.Array, json.RootElement.GetProperty("integrations").ValueKind);
    }

    [Fact]
    public async Task Diagnostics_is_read_only_for_persisted_capabilities()
    {
        using (var scope = factory.Services.CreateScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<NovaDbContext>();
            database.IntegrationCapabilities.RemoveRange(database.IntegrationCapabilities);
            await database.SaveChangesAsync();
        }

        using var client = await factory.CreateAuthenticatedClientAsync(factory.AdminEmail);
        using var response = await client.GetAsync("/api/v1/admin/operations/diagnostics");
        response.EnsureSuccessStatusCode();

        using var checkScope = factory.Services.CreateScope();
        var checkDatabase = checkScope.ServiceProvider.GetRequiredService<NovaDbContext>();
        Assert.Equal(0, await checkDatabase.IntegrationCapabilities.CountAsync());
    }
}
