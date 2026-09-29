using System.Net;
using System.Text.Json;
using Xunit;

namespace NovaHaven.Integration.Tests;

public sealed class KnowledgeApiIntegrationTests : IClassFixture<LocalApiFactory>
{
    private readonly LocalApiFactory factory;

    public KnowledgeApiIntegrationTests(LocalApiFactory factory) => this.factory = factory;

    [Fact]
    public async Task Knowledge_is_published_only_and_npc_keeps_location_relation_snapshot()
    {
        using var client = await factory.CreateAuthenticatedClientAsync(factory.AdminEmail);
        var suffix = Guid.NewGuid().ToString("N");

        var csrf = await LocalApiFactory.GetCsrfAsync(client);
        using var createLocation = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Post,
            "/api/v1/admin/knowledge/location", new
            {
                name = "Harbor Gate", slug = $"harbor-gate-{suffix}", summary = "A northern gate.", markdown = "# Harbor Gate",
                kind = "location", region = "North Reach", locationType = "Gate", latitude = 10.2, longitude = 106.4
            }, csrf);
        createLocation.EnsureSuccessStatusCode();
        var locationId = await ReadGuidAsync(createLocation, "id");

        using var locationAdmin = await client.GetAsync($"/api/v1/admin/knowledge/location/{locationId}");
        locationAdmin.EnsureSuccessStatusCode();
        csrf = await LocalApiFactory.GetCsrfAsync(client);
        using var publishLocation = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Post,
            $"/api/v1/admin/knowledge/location/{locationId}/publish", new { }, csrf,
            locationAdmin.Headers.ETag?.ToString());
        publishLocation.EnsureSuccessStatusCode();

        var npcSlug = $"warden-lyra-{suffix}";
        csrf = await LocalApiFactory.GetCsrfAsync(client);
        using var createNpc = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Post,
            "/api/v1/admin/knowledge/npc", new
            {
                name = "Warden Lyra", slug = npcSlug, summary = "Harbor warden.", markdown = "# Warden Lyra",
                kind = "npc", role = "Harbor Warden", locationEntryId = locationId
            }, csrf);
        createNpc.EnsureSuccessStatusCode();
        var npcId = await ReadGuidAsync(createNpc, "id");

        using var privateNpc = await client.GetAsync($"/api/v1/knowledge/npc/{npcSlug}");
        Assert.Equal(HttpStatusCode.NotFound, privateNpc.StatusCode);

        using var npcAdmin = await client.GetAsync($"/api/v1/admin/knowledge/npc/{npcId}");
        npcAdmin.EnsureSuccessStatusCode();
        csrf = await LocalApiFactory.GetCsrfAsync(client);
        using var publishNpc = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Post,
            $"/api/v1/admin/knowledge/npc/{npcId}/publish", new { }, csrf,
            npcAdmin.Headers.ETag?.ToString());
        publishNpc.EnsureSuccessStatusCode();

        using var publicNpc = await client.GetAsync($"/api/v1/knowledge/npc/{npcSlug}");
        publicNpc.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await publicNpc.Content.ReadAsStreamAsync());
        Assert.Equal("Warden Lyra", json.RootElement.GetProperty("name").GetString());
        Assert.Equal("Harbor Gate", json.RootElement.GetProperty("metadata").GetProperty("location").GetProperty("name").GetString());
    }

    [Fact]
    public async Task Knowledge_publish_rejects_unpublished_relation_and_stale_etag()
    {
        using var client = await factory.CreateAuthenticatedClientAsync(factory.AdminEmail);
        var suffix = Guid.NewGuid().ToString("N");
        var csrf = await LocalApiFactory.GetCsrfAsync(client);
        using var create = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Post,
            "/api/v1/admin/knowledge/quest", new
            {
                name = "Unready Quest", slug = $"unready-quest-{suffix}", summary = "Draft quest.", markdown = "# Quest",
                kind = "quest", difficulty = 2, rewardDescription = "Editorial reward", steps = new[] { new { position = 1, title = "Start", description = "Begin." } }
            }, csrf);
        create.EnsureSuccessStatusCode();
        var id = await ReadGuidAsync(create, "id");
        using var admin = await client.GetAsync($"/api/v1/admin/knowledge/quest/{id}");
        admin.EnsureSuccessStatusCode();
        var etag = admin.Headers.ETag?.ToString();
        csrf = await LocalApiFactory.GetCsrfAsync(client);
        using var edit = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Patch,
            $"/api/v1/admin/knowledge/quest/{id}", new
            {
                name = "Unready Quest Updated", slug = $"unready-quest-{suffix}", summary = "Draft quest.", markdown = "# Quest",
                kind = "quest", difficulty = 2, rewardDescription = "Editorial reward", steps = new[] { new { position = 1, title = "Start", description = "Begin." } }
            }, csrf, etag);
        edit.EnsureSuccessStatusCode();

        csrf = await LocalApiFactory.GetCsrfAsync(client);
        using var stalePublish = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Post,
            $"/api/v1/admin/knowledge/quest/{id}/publish", new { }, csrf, etag);
        Assert.Equal(HttpStatusCode.PreconditionFailed, stalePublish.StatusCode);
    }

    private static async Task<Guid> ReadGuidAsync(HttpResponseMessage response, string property)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        return document.RootElement.GetProperty(property).GetGuid();
    }
}
