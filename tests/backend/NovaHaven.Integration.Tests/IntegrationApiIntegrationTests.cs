using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NovaHaven.Infrastructure.Data;
using Xunit;

namespace NovaHaven.Integration.Tests;

public sealed class IntegrationApiIntegrationTests : IClassFixture<LocalApiFactory>
{
    private readonly LocalApiFactory factory;

    public IntegrationApiIntegrationTests(LocalApiFactory factory) => this.factory = factory;

    [Fact]
    public async Task Integration_status_is_explicitly_unavailable_and_blocks_false_healthy_transition()
    {
        using var client = await factory.CreateAuthenticatedClientAsync(factory.AdminEmail);
        using var response = await client.GetAsync("/api/v1/admin/integrations/capabilities");
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        var bridge = json.RootElement.EnumerateArray().Single(item => item.GetProperty("capabilityKey").GetString() == "minecraft-bridge");
        Assert.Equal("unavailable", bridge.GetProperty("status").GetString());
        var etag = bridge.GetProperty("etag").GetString();

        var csrf = await LocalApiFactory.GetCsrfAsync(client);
        using var update = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Patch,
            "/api/v1/admin/integrations/capabilities/minecraft-bridge", new { status = "healthy", safeMessage = "No plugin contract is configured." }, csrf, etag);
        Assert.Equal(HttpStatusCode.BadRequest, update.StatusCode);
    }

    [Fact]
    public async Task Public_integration_status_does_not_write_capability_rows()
    {
        using (var scope = factory.Services.CreateScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<NovaDbContext>();
            database.IntegrationCapabilities.RemoveRange(database.IntegrationCapabilities);
            await database.SaveChangesAsync();
        }

        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/v1/integrations/status");
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        Assert.Equal(4, json.RootElement.GetArrayLength());

        using var checkScope = factory.Services.CreateScope();
        var checkDatabase = checkScope.ServiceProvider.GetRequiredService<NovaDbContext>();
        Assert.Equal(0, await checkDatabase.IntegrationCapabilities.CountAsync());
    }
}
