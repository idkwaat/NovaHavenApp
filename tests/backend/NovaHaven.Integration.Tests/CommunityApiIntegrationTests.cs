using System.Net;
using System.Text.Json;
using Xunit;

namespace NovaHaven.Integration.Tests;

public sealed class CommunityApiIntegrationTests : IClassFixture<LocalApiFactory>
{
    private readonly LocalApiFactory factory;

    public CommunityApiIntegrationTests(LocalApiFactory factory) => this.factory = factory;

    [Fact]
    public async Task Event_is_published_only_and_admin_can_manage_registrations()
    {
        using var client = await factory.CreateAuthenticatedClientAsync(factory.AdminEmail);
        var suffix = Guid.NewGuid().ToString("N");
        var slug = $"harbor-festival-{suffix}";
        var input = new
        {
            name = "Harbor Festival", slug, summary = "A community gathering.", markdown = "# Harbor Festival",
            kind = "event", startsAt = "2026-12-01T10:00:00Z", endsAt = "2026-12-01T12:00:00Z", capacity = 30, registrationOpen = true
        };
        var csrf = await LocalApiFactory.GetCsrfAsync(client);
        using var create = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Post, "/api/v1/admin/community/event", input, csrf);
        create.EnsureSuccessStatusCode();
        var eventId = await ReadGuidAsync(create, "id");

        using var privateEvent = await client.GetAsync($"/api/v1/community/event/{slug}");
        Assert.Equal(HttpStatusCode.NotFound, privateEvent.StatusCode);

        using var admin = await client.GetAsync($"/api/v1/admin/community/event/{eventId}");
        admin.EnsureSuccessStatusCode();
        csrf = await LocalApiFactory.GetCsrfAsync(client);
        using var publish = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Post,
            $"/api/v1/admin/community/event/{eventId}/publish", new { }, csrf, admin.Headers.ETag?.ToString());
        publish.EnsureSuccessStatusCode();

        using var publicEvent = await client.GetAsync($"/api/v1/community/event/{slug}");
        publicEvent.EnsureSuccessStatusCode();
        using var eventJson = JsonDocument.Parse(await publicEvent.Content.ReadAsStreamAsync());
        Assert.Equal(30, eventJson.RootElement.GetProperty("metadata").GetProperty("capacity").GetInt32());

        csrf = await LocalApiFactory.GetCsrfAsync(client);
        using var register = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Post,
            $"/api/v1/admin/community/event/{eventId}/registrations", new { displayName = "Lyra", contact = "discord:lyra" }, csrf);
        register.EnsureSuccessStatusCode();
        using var registrations = await client.GetAsync($"/api/v1/admin/community/event/{eventId}/registrations");
        registrations.EnsureSuccessStatusCode();
        using var registrationJson = JsonDocument.Parse(await registrations.Content.ReadAsStreamAsync());
        Assert.Equal("Lyra", registrationJson.RootElement[0].GetProperty("displayName").GetString());
    }

    [Fact]
    public async Task Leaderboard_publishes_editorial_standings_without_gameplay_claims()
    {
        using var client = await factory.CreateAuthenticatedClientAsync(factory.AdminEmail);
        var suffix = Guid.NewGuid().ToString("N");
        var csrf = await LocalApiFactory.GetCsrfAsync(client);
        using var create = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Post, "/api/v1/admin/community/leaderboard", new
        {
            name = "Fishing Cup", slug = $"fishing-cup-{suffix}", summary = "Editorial event standings.", markdown = "# Fishing Cup",
            kind = "leaderboard", leaderboardCategory = "Fishing", rows = new[] { new { rank = 1, participantName = "Lyra", score = 120.5, note = "Verified by event staff" } }
        }, csrf);
        create.EnsureSuccessStatusCode();
        var id = await ReadGuidAsync(create, "id");
        using var admin = await client.GetAsync($"/api/v1/admin/community/leaderboard/{id}");
        admin.EnsureSuccessStatusCode();
        csrf = await LocalApiFactory.GetCsrfAsync(client);
        using var publish = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Post,
            $"/api/v1/admin/community/leaderboard/{id}/publish", new { }, csrf, admin.Headers.ETag?.ToString());
        publish.EnsureSuccessStatusCode();
        using var publicBoard = await client.GetAsync($"/api/v1/community/leaderboard/fishing-cup-{suffix}");
        publicBoard.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await publicBoard.Content.ReadAsStreamAsync());
        Assert.Equal("Lyra", json.RootElement.GetProperty("metadata").GetProperty("rows")[0].GetProperty("participantName").GetString());
    }

    private static async Task<Guid> ReadGuidAsync(HttpResponseMessage response, string property)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        return document.RootElement.GetProperty(property).GetGuid();
    }
}
