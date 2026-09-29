using System.Net;
using System.Text.Json;
using Xunit;

namespace NovaHaven.Integration.Tests;

public sealed class CatalogApiIntegrationTests : IClassFixture<LocalApiFactory>
{
    private readonly LocalApiFactory factory;

    public CatalogApiIntegrationTests(LocalApiFactory factory) => this.factory = factory;

    [Fact]
    public async Task CatalogIsPublishedOnlyAndUsesImmutableRevisionLifecycle()
    {
        using var client = await factory.CreateAuthenticatedClientAsync(factory.AdminEmail);
        var suffix = Guid.NewGuid().ToString("N");
        var slug = $"moonsteel-{suffix}";
        var input = new
        {
            name = "Moonsteel Sword",
            slug,
            summary = "A forged blade.",
            markdown = "# Moonsteel Sword\n\nFirst revision.",
            kind = "weapon"
        };
        var csrf = await LocalApiFactory.GetCsrfAsync(client);
        using var create = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Post,
            "/api/v1/admin/catalog/items", input, csrf);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var itemId = await ReadGuidAsync(create, "id");

        using var privateDetail = await client.GetAsync($"/api/v1/catalog/items/{slug}");
        Assert.Equal(HttpStatusCode.NotFound, privateDetail.StatusCode);

        using var admin = await client.GetAsync($"/api/v1/admin/catalog/items/{itemId}");
        admin.EnsureSuccessStatusCode();
        var etag = admin.Headers.ETag?.ToString();
        Assert.False(string.IsNullOrWhiteSpace(etag));

        csrf = await LocalApiFactory.GetCsrfAsync(client);
        using var publish = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Post,
            $"/api/v1/admin/catalog/items/{itemId}/publish", new { }, csrf, etag);
        publish.EnsureSuccessStatusCode();

        using var publicFirst = await client.GetAsync($"/api/v1/catalog/items/{slug}");
        publicFirst.EnsureSuccessStatusCode();
        using var firstJson = JsonDocument.Parse(await publicFirst.Content.ReadAsStreamAsync());
        Assert.Equal("Moonsteel Sword", firstJson.RootElement.GetProperty("name").GetString());
        Assert.Equal("weapon", firstJson.RootElement.GetProperty("kind").GetString());
        Assert.Equal(1, firstJson.RootElement.GetProperty("revision").GetInt32());

        using var currentAdmin = await client.GetAsync($"/api/v1/admin/catalog/items/{itemId}");
        currentAdmin.EnsureSuccessStatusCode();
        var currentEtag = currentAdmin.Headers.ETag?.ToString();
        csrf = await LocalApiFactory.GetCsrfAsync(client);
        using var edit = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Patch,
            $"/api/v1/admin/catalog/items/{itemId}", new
            {
                name = "Moonsteel Sword Mk II", input.slug, input.summary,
                markdown = "# Moonsteel Sword Mk II\n\nSecond revision.", input.kind
            }, csrf, currentEtag);
        edit.EnsureSuccessStatusCode();

        using var publicBeforeSecondPublish = await client.GetAsync($"/api/v1/catalog/items/{slug}");
        publicBeforeSecondPublish.EnsureSuccessStatusCode();
        using var unchangedJson = JsonDocument.Parse(await publicBeforeSecondPublish.Content.ReadAsStreamAsync());
        Assert.Equal("Moonsteel Sword", unchangedJson.RootElement.GetProperty("name").GetString());
        Assert.Equal(1, unchangedJson.RootElement.GetProperty("revision").GetInt32());

        using var editedAdmin = await client.GetAsync($"/api/v1/admin/catalog/items/{itemId}");
        editedAdmin.EnsureSuccessStatusCode();
        var editedEtag = editedAdmin.Headers.ETag?.ToString();
        csrf = await LocalApiFactory.GetCsrfAsync(client);
        using var publishSecond = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Post,
            $"/api/v1/admin/catalog/items/{itemId}/publish", new { }, csrf, editedEtag);
        publishSecond.EnsureSuccessStatusCode();

        using var publicSecond = await client.GetAsync($"/api/v1/catalog/items/{slug}");
        publicSecond.EnsureSuccessStatusCode();
        using var secondJson = JsonDocument.Parse(await publicSecond.Content.ReadAsStreamAsync());
        Assert.Equal("Moonsteel Sword Mk II", secondJson.RootElement.GetProperty("name").GetString());
        Assert.Equal(2, secondJson.RootElement.GetProperty("revision").GetInt32());

        using var latestAdmin = await client.GetAsync($"/api/v1/admin/catalog/items/{itemId}");
        latestAdmin.EnsureSuccessStatusCode();
        var latestEtag = latestAdmin.Headers.ETag?.ToString();
        csrf = await LocalApiFactory.GetCsrfAsync(client);
        using var unpublish = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Post,
            $"/api/v1/admin/catalog/items/{itemId}/unpublish", new { }, csrf, latestEtag);
        unpublish.EnsureSuccessStatusCode();

        using var privateAfterUnpublish = await client.GetAsync($"/api/v1/catalog/items/{slug}");
        Assert.Equal(HttpStatusCode.NotFound, privateAfterUnpublish.StatusCode);
    }

    [Fact]
    public async Task RecipePublishSnapshotsPublishedItemRevisions()
    {
        using var client = await factory.CreateAuthenticatedClientAsync(factory.AdminEmail);
        var suffix = Guid.NewGuid().ToString("N");

        async Task<Guid> CreateAndPublishItemAsync(string name, string slug)
        {
            var csrf = await LocalApiFactory.GetCsrfAsync(client);
            using var create = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Post,
                "/api/v1/admin/catalog/items",
                new { name, slug, summary = "Catalog item for recipe integration.", markdown = $"# {name}", kind = "material" }, csrf);
            create.EnsureSuccessStatusCode();
            var id = await ReadGuidAsync(create, "id");
            using var admin = await client.GetAsync($"/api/v1/admin/catalog/items/{id}");
            admin.EnsureSuccessStatusCode();
            csrf = await LocalApiFactory.GetCsrfAsync(client);
            using var publish = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Post,
                $"/api/v1/admin/catalog/items/{id}/publish", new { }, csrf, admin.Headers.ETag?.ToString());
            publish.EnsureSuccessStatusCode();
            return id;
        }

        var ingredientId = await CreateAndPublishItemAsync("Moonsteel Ingot", $"moonsteel-ingot-{suffix}");
        var outputId = await CreateAndPublishItemAsync("Moonsteel Sword", $"moonsteel-sword-{suffix}");
        var recipeSlug = $"forge-moonsteel-{suffix}";
        var recipeInput = new
        {
            name = "Forge Moonsteel Sword",
            slug = recipeSlug,
            summary = "Forge one sword from an ingot.",
            markdown = "# Forge\n\nUse the forge.",
            ingredients = new[] { new { itemId = ingredientId, quantity = 1 } },
            outputs = new[] { new { itemId = outputId, quantity = 1 } }
        };
        var csrf = await LocalApiFactory.GetCsrfAsync(client);
        using var createRecipe = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Post,
            "/api/v1/admin/catalog/recipes", recipeInput, csrf);
        createRecipe.EnsureSuccessStatusCode();
        var recipeId = await ReadGuidAsync(createRecipe, "id");

        using var privateRecipe = await client.GetAsync($"/api/v1/catalog/recipes/{recipeSlug}");
        Assert.Equal(HttpStatusCode.NotFound, privateRecipe.StatusCode);
        using var adminRecipe = await client.GetAsync($"/api/v1/admin/catalog/recipes/{recipeId}");
        adminRecipe.EnsureSuccessStatusCode();
        csrf = await LocalApiFactory.GetCsrfAsync(client);
        using var publishRecipe = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Post,
            $"/api/v1/admin/catalog/recipes/{recipeId}/publish", new { }, csrf, adminRecipe.Headers.ETag?.ToString());
        publishRecipe.EnsureSuccessStatusCode();

        using var publicRecipe = await client.GetAsync($"/api/v1/catalog/recipes/{recipeSlug}");
        publicRecipe.EnsureSuccessStatusCode();
        using var recipeJson = JsonDocument.Parse(await publicRecipe.Content.ReadAsStreamAsync());
        Assert.Equal(1, recipeJson.RootElement.GetProperty("revision").GetInt32());
        Assert.Equal("Moonsteel Ingot", recipeJson.RootElement.GetProperty("ingredients")[0].GetProperty("itemName").GetString());
        Assert.Equal("Moonsteel Sword", recipeJson.RootElement.GetProperty("outputs")[0].GetProperty("itemName").GetString());

        using var latestAdminRecipe = await client.GetAsync($"/api/v1/admin/catalog/recipes/{recipeId}");
        latestAdminRecipe.EnsureSuccessStatusCode();
        csrf = await LocalApiFactory.GetCsrfAsync(client);
        using var unpublish = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Post,
            $"/api/v1/admin/catalog/recipes/{recipeId}/unpublish", new { }, csrf, latestAdminRecipe.Headers.ETag?.ToString());
        unpublish.EnsureSuccessStatusCode();
        using var privateAfterUnpublish = await client.GetAsync($"/api/v1/catalog/recipes/{recipeSlug}");
        Assert.Equal(HttpStatusCode.NotFound, privateAfterUnpublish.StatusCode);
    }

    private static async Task<Guid> ReadGuidAsync(HttpResponseMessage response, string property)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        return document.RootElement.GetProperty(property).GetGuid();
    }
}
