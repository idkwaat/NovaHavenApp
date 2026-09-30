using System.Net;
using System.Text.Json;
using Xunit;

namespace NovaHaven.Integration.Tests;

public sealed class RewardsCommerceApiIntegrationTests : IClassFixture<LocalApiFactory>
{
    private readonly LocalApiFactory factory;
    public RewardsCommerceApiIntegrationTests(LocalApiFactory factory) => this.factory = factory;

    [Fact]
    public async Task Reward_definition_publishes_without_executing_external_grant()
    {
        using var client = await factory.CreateAuthenticatedClientAsync(factory.AdminEmail);
        var csrf = await LocalApiFactory.GetCsrfAsync(client); var suffix = Guid.NewGuid().ToString("N");
        using var create = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Post, "/api/v1/admin/rewards", new { name = "Starter Pack", slug = $"starter-pack-{suffix}", summary = "Editorial reward.", markdown = "# Starter", kind = "item", deliveryDescription = "A Minecraft adapter must acknowledge and deliver this reward." }, csrf);
        create.EnsureSuccessStatusCode();
        var id = await ReadGuidAsync(create, "id");
        using var admin = await client.GetAsync($"/api/v1/admin/rewards/{id}"); admin.EnsureSuccessStatusCode();
        csrf = await LocalApiFactory.GetCsrfAsync(client);
        using var publish = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Post, $"/api/v1/admin/rewards/{id}/publish", new { }, csrf, admin.Headers.ETag?.ToString()); publish.EnsureSuccessStatusCode();
        var slug = $"starter-pack-{suffix}";
        using var publishedDetail = await client.GetAsync($"/api/v1/rewards/{slug}"); publishedDetail.EnsureSuccessStatusCode();
        using var originalJson = JsonDocument.Parse(await publishedDetail.Content.ReadAsStreamAsync());
        Assert.Equal("Starter Pack", originalJson.RootElement.GetProperty("name").GetString());

        using var latestAdmin = await client.GetAsync($"/api/v1/admin/rewards/{id}"); latestAdmin.EnsureSuccessStatusCode();
        csrf = await LocalApiFactory.GetCsrfAsync(client);
        using var draftEdit = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Patch,
            $"/api/v1/admin/rewards/{id}", new
            {
                name = "Hidden Draft Name", slug, summary = "Unpublished draft edit.",
                markdown = "# Hidden draft", kind = "item", deliveryDescription = "Still requires external acknowledgement."
            }, csrf, latestAdmin.Headers.ETag?.ToString());
        draftEdit.EnsureSuccessStatusCode();

        using var unchangedPublicDetail = await client.GetAsync($"/api/v1/rewards/{slug}"); unchangedPublicDetail.EnsureSuccessStatusCode();
        using var unchangedJson = JsonDocument.Parse(await unchangedPublicDetail.Content.ReadAsStreamAsync());
        Assert.Equal("Starter Pack", unchangedJson.RootElement.GetProperty("name").GetString());
        using var publicList = await client.GetAsync("/api/v1/rewards?q=Starter"); publicList.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await publicList.Content.ReadAsStreamAsync());
        Assert.True(json.RootElement.GetProperty("items")[0].GetProperty("externalAcknowledgementRequired").GetBoolean());
        Assert.Equal("Starter Pack", json.RootElement.GetProperty("items")[0].GetProperty("name").GetString());
        using var hiddenDraftSearch = await client.GetAsync("/api/v1/rewards?q=Hidden+Draft"); hiddenDraftSearch.EnsureSuccessStatusCode();
        using var hiddenDraftJson = JsonDocument.Parse(await hiddenDraftSearch.Content.ReadAsStreamAsync());
        Assert.Equal(0, hiddenDraftJson.RootElement.GetProperty("total").GetInt32());

        csrf = await LocalApiFactory.GetCsrfAsync(client);
        using var unpublish = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Post,
            $"/api/v1/admin/rewards/{id}/unpublish", new { }, csrf, draftEdit.Headers.ETag?.ToString());
        Assert.Equal(HttpStatusCode.NoContent, unpublish.StatusCode);
        Assert.NotNull(unpublish.Headers.ETag);
    }

    [Fact]
    public async Task Commerce_definition_is_public_catalog_only_and_has_no_checkout_endpoint()
    {
        using var client = await factory.CreateAuthenticatedClientAsync(factory.AdminEmail);
        var csrf = await LocalApiFactory.GetCsrfAsync(client); var suffix = Guid.NewGuid().ToString("N");
        using var create = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Post, "/api/v1/admin/commerce/offers", new { name = "Supporter", slug = $"supporter-{suffix}", summary = "Definition only.", markdown = "# Supporter", kind = "donation", displayPrice = "5 USD", providerProductCode = "public-supporter" }, csrf);
        create.EnsureSuccessStatusCode(); var id = await ReadGuidAsync(create, "id");
        using var admin = await client.GetAsync($"/api/v1/admin/commerce/offers/{id}"); admin.EnsureSuccessStatusCode(); csrf = await LocalApiFactory.GetCsrfAsync(client);
        using var publish = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Post, $"/api/v1/admin/commerce/offers/{id}/publish", new { }, csrf, admin.Headers.ETag?.ToString()); publish.EnsureSuccessStatusCode();
        using var publicOffer = await client.GetAsync($"/api/v1/commerce/offers/supporter-{suffix}"); publicOffer.EnsureSuccessStatusCode(); using var json = JsonDocument.Parse(await publicOffer.Content.ReadAsStreamAsync());
        Assert.True(json.RootElement.GetProperty("definitionOnly").GetBoolean());
        using var checkout = await client.PostAsync($"/api/v1/commerce/offers/{id}/checkout", new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.NotFound, checkout.StatusCode);
    }

    private static async Task<Guid> ReadGuidAsync(HttpResponseMessage response, string property)
    { using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync()); return document.RootElement.GetProperty(property).GetGuid(); }
}
