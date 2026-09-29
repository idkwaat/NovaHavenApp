using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NovaHaven.Infrastructure.Data;
using NovaHaven.Infrastructure.Identity;
using Xunit;

namespace NovaHaven.Integration.Tests;

public sealed class LocalApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string databaseName = $"NovaHaven_Integration_{Guid.NewGuid():N}";
    private readonly string testPassword = $"NovaTest-{Guid.NewGuid():N}a1!";
    private readonly string? previousConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__NovaDb");
    private bool databaseMayExist;

    public string AdminEmail { get; } = $"admin-{Guid.NewGuid():N}@local.test";
    public string EditorEmail { get; } = $"editor-{Guid.NewGuid():N}@local.test";
    public string TestPassword => testPassword;
    private string ConnectionString =>
        $"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Integrated Security=True;TrustServerCertificate=True";

    public LocalApiFactory()
    {
        // Program reads the connection string while WebApplicationFactory creates the host,
        // before ConfigureWebHost can replace the application's configuration.
        Environment.SetEnvironmentVariable("ConnectionStrings__NovaDb", ConnectionString);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Exercise the HTTP cookie flow over the in-memory http:// test server.
        // Development keeps the production cookie policy's SameSite behavior while
        // allowing the local non-TLS test client to send the auth cookie.
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:NovaDb"] = ConnectionString,
                ["SeedAdmin:Enabled"] = "false"
            }));
    }

    public async Task InitializeAsync()
    {
        _ = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var scope = Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<NovaDbContext>();
        databaseMayExist = true;
        await database.Database.MigrateAsync();

        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await EnsureRoleAsync(roles, "Admin");
        await EnsureUserAsync(users, AdminEmail, isAdmin: true);
        await EnsureUserAsync(users, EditorEmail, isAdmin: false);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        try
        {
            if (databaseMayExist)
            {
                using var scope = Services.CreateScope();
                var database = scope.ServiceProvider.GetRequiredService<NovaDbContext>();
                if (await database.Database.CanConnectAsync())
                {
                    await database.Database.EnsureDeletedAsync();
                }
            }
        }
        finally
        {
            await base.DisposeAsync();
            Environment.SetEnvironmentVariable("ConnectionStrings__NovaDb", previousConnectionString);
        }
    }

    public async Task<HttpClient> CreateAuthenticatedClientAsync(string email)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var csrf = await GetCsrfAsync(client);
        using var login = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
        {
            Content = JsonContent.Create(new { email, password = TestPassword })
        };
        login.Headers.Add("X-CSRF-TOKEN", csrf);
        var response = await client.SendAsync(login);
        if (response.StatusCode != HttpStatusCode.NoContent)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Login failed with {(int)response.StatusCode}: {body}");
        }

        return client;
    }

    public static async Task<string> GetCsrfAsync(HttpClient client)
    {
        using var response = await client.GetAsync("/api/v1/auth/csrf");
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        return document.RootElement.GetProperty("token").GetString()
            ?? throw new InvalidOperationException("The API did not return an anti-forgery token.");
    }

    public static async Task<HttpResponseMessage> SendJsonAsync(
        HttpClient client, HttpMethod method, string path, object body, string csrf, string? ifMatch = null)
    {
        using var request = new HttpRequestMessage(method, path) { Content = JsonContent.Create(body) };
        request.Headers.Add("X-CSRF-TOKEN", csrf);
        if (ifMatch is not null) request.Headers.TryAddWithoutValidation("If-Match", ifMatch);
        return await client.SendAsync(request);
    }

    public static async Task<HttpResponseMessage> SendMultipartAsync(
        HttpClient client, string path, string fieldName, string fileName, string contentType,
        byte[] bytes, string csrf)
    {
        using var form = new MultipartFormDataContent();
        using var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(file, fieldName, fileName);
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = form };
        request.Headers.Add("X-CSRF-TOKEN", csrf);
        return await client.SendAsync(request);
    }

    private async Task EnsureRoleAsync(RoleManager<IdentityRole<Guid>> roles, string roleName)
    {
        if (await roles.RoleExistsAsync(roleName)) return;
        var result = await roles.CreateAsync(new IdentityRole<Guid>(roleName));
        EnsureIdentitySuccess(result, $"creating role {roleName}");
    }

    private async Task EnsureUserAsync(UserManager<ApplicationUser> users, string email, bool isAdmin)
    {
        var user = await users.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
            var result = await users.CreateAsync(user, TestPassword);
            EnsureIdentitySuccess(result, $"creating user {email}");
        }

        if (isAdmin)
        {
            var result = await users.AddToRoleAsync(user, "Admin");
            EnsureIdentitySuccess(result, $"assigning Admin role to {email}");
        }
    }

    private static void EnsureIdentitySuccess(IdentityResult result, string operation)
    {
        if (result.Succeeded) return;
        throw new InvalidOperationException($"Identity {operation} failed: " +
            string.Join("; ", result.Errors.Select(error => error.Description)));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }
}

public sealed class WikiApiIntegrationTests : IClassFixture<LocalApiFactory>
{
    private readonly LocalApiFactory factory;

    public WikiApiIntegrationTests(LocalApiFactory factory) => this.factory = factory;

    [Fact]
    public async Task AnonymousAdminReadRequiresAuthentication()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync("/api/v1/admin/wiki/categories");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthenticatedNonAdminCannotMutateAdminWiki()
    {
        using var client = await factory.CreateAuthenticatedClientAsync(factory.EditorEmail);
        var csrf = await LocalApiFactory.GetCsrfAsync(client);

        using var response = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Post,
            "/api/v1/admin/wiki/categories",
            new { name = $"Editor attempt {Guid.NewGuid():N}", slug = $"editor-attempt-{Guid.NewGuid():N}", displayOrder = 0 }, csrf);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CategoryRequiresIfMatchAndRejectsDuplicateSlug()
    {
        using var client = await factory.CreateAuthenticatedClientAsync(factory.AdminEmail);
        var csrf = await LocalApiFactory.GetCsrfAsync(client);
        var slug = $"category-{Guid.NewGuid():N}";
        var body = new { name = "Integration Category", slug, displayOrder = 0 };

        using var create = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Post,
            "/api/v1/admin/wiki/categories", body, csrf);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var categoryId = await ReadGuidAsync(create, "id");

        using var get = await client.GetAsync($"/api/v1/admin/wiki/categories/{categoryId}");
        get.EnsureSuccessStatusCode();
        var etag = get.Headers.ETag?.ToString();
        Assert.False(string.IsNullOrWhiteSpace(etag));

        csrf = await LocalApiFactory.GetCsrfAsync(client);
        using var missingPrecondition = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Patch,
            $"/api/v1/admin/wiki/categories/{categoryId}",
            new { name = "Integration Category Updated", slug, displayOrder = 1, isActive = true }, csrf);
        Assert.Equal((HttpStatusCode)428, missingPrecondition.StatusCode);

        csrf = await LocalApiFactory.GetCsrfAsync(client);
        using var stale = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Patch,
            $"/api/v1/admin/wiki/categories/{categoryId}",
            new { name = "Integration Category Updated", slug, displayOrder = 1, isActive = true }, csrf, "\"stale\"");
        Assert.Equal(HttpStatusCode.PreconditionFailed, stale.StatusCode);

        csrf = await LocalApiFactory.GetCsrfAsync(client);
        using var duplicate = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Post,
            "/api/v1/admin/wiki/categories",
            new { name = "Another Integration Category", slug, displayOrder = 1 }, csrf);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task Tag_lifecycle_requires_csrf_and_matching_if_match()
    {
        using var client = await factory.CreateAuthenticatedClientAsync(factory.AdminEmail);
        var csrf = await LocalApiFactory.GetCsrfAsync(client);
        var suffix = Guid.NewGuid().ToString("N");
        using var create = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Post,
            "/api/v1/admin/wiki/tags", new { name = $"Test tag {suffix}", slug = $"test-tag-{suffix}" }, csrf);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        using var createBody = JsonDocument.Parse(await create.Content.ReadAsStreamAsync());
        var tagId = createBody.RootElement.GetProperty("id").GetGuid();
        var etag = createBody.RootElement.GetProperty("etag").GetString();
        Assert.False(string.IsNullOrWhiteSpace(etag));

        var tagPath = $"/api/v1/admin/wiki/tags/{tagId}";
        csrf = await LocalApiFactory.GetCsrfAsync(client);
        using var missing = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Patch, tagPath,
            new { name = $"Test tag {suffix}", slug = $"test-tag-{suffix}", isActive = true }, csrf);
        Assert.Equal((HttpStatusCode)428, missing.StatusCode);

        csrf = await LocalApiFactory.GetCsrfAsync(client);
        using var stale = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Patch, tagPath,
            new { name = $"Test tag {suffix}", slug = $"test-tag-{suffix}", isActive = true }, csrf, "\"stale\"");
        Assert.Equal(HttpStatusCode.PreconditionFailed, stale.StatusCode);

        csrf = await LocalApiFactory.GetCsrfAsync(client);
        using var update = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Patch, tagPath,
            new { name = $"Updated tag {suffix}", slug = $"test-tag-{suffix}", isActive = true }, csrf, etag);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.NotNull(update.Headers.ETag);

        csrf = await LocalApiFactory.GetCsrfAsync(client);
        using var delete = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Delete, tagPath,
            new { }, csrf, update.Headers.ETag!.ToString());
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    [Fact]
    public async Task PublicWikiOnlyExposesPublishedRevision()
    {
        using var client = await factory.CreateAuthenticatedClientAsync(factory.AdminEmail);
        var csrf = await LocalApiFactory.GetCsrfAsync(client);
        var suffix = Guid.NewGuid().ToString("N");
        var categorySlug = $"publish-category-{suffix}";
        var articleSlug = $"publish-article-{suffix}";

        using var categoryResponse = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Post,
            "/api/v1/admin/wiki/categories",
            new { name = $"Publish Category {suffix}", slug = categorySlug, displayOrder = 0 }, csrf);
        categoryResponse.EnsureSuccessStatusCode();
        var categoryId = await ReadGuidAsync(categoryResponse, "id");

        csrf = await LocalApiFactory.GetCsrfAsync(client);
        using var articleResponse = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Post,
            "/api/v1/admin/wiki/articles",
            new
            {
                title = "Integration Published Article",
                slug = articleSlug,
                summary = "Published from LocalDB integration test.",
                markdown = "# Integration\n\nPublished content.",
                categoryId,
                tagIds = Array.Empty<Guid>()
            }, csrf);
        articleResponse.EnsureSuccessStatusCode();
        var articleId = await ReadGuidAsync(articleResponse, "id");

        using var beforePublish = await client.GetAsync($"/api/v1/wiki/articles/{articleSlug}");
        Assert.Equal(HttpStatusCode.NotFound, beforePublish.StatusCode);

        using var adminArticle = await client.GetAsync($"/api/v1/admin/wiki/articles/{articleId}");
        adminArticle.EnsureSuccessStatusCode();
        var etag = adminArticle.Headers.ETag?.ToString();
        Assert.False(string.IsNullOrWhiteSpace(etag));

        csrf = await LocalApiFactory.GetCsrfAsync(client);
        using var publish = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Post,
            $"/api/v1/admin/wiki/articles/{articleId}/publish", new { }, csrf, etag);
        publish.EnsureSuccessStatusCode();

        using var publicArticle = await client.GetAsync($"/api/v1/wiki/articles/{articleSlug}");
        publicArticle.EnsureSuccessStatusCode();
        using var publicJson = JsonDocument.Parse(await publicArticle.Content.ReadAsStreamAsync());
        Assert.Equal("Integration Published Article", publicJson.RootElement.GetProperty("title").GetString());
        Assert.Equal(JsonValueKind.Array, publicJson.RootElement.GetProperty("related").ValueKind);

        var relatedSlug = $"related-article-{suffix}";
        csrf = await LocalApiFactory.GetCsrfAsync(client);
        using var relatedDraft = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Post,
            "/api/v1/admin/wiki/articles",
            new
            {
                title = "Related Draft",
                slug = relatedSlug,
                summary = "Still private.",
                markdown = "Private related draft.",
                categoryId,
                tagIds = Array.Empty<Guid>()
            }, csrf);
        relatedDraft.EnsureSuccessStatusCode();

        using var publicBeforeRelatedPublish = await client.GetAsync($"/api/v1/wiki/articles/{articleSlug}");
        publicBeforeRelatedPublish.EnsureSuccessStatusCode();
        using var beforeRelatedJson = JsonDocument.Parse(await publicBeforeRelatedPublish.Content.ReadAsStreamAsync());
        Assert.DoesNotContain(beforeRelatedJson.RootElement.GetProperty("related").EnumerateArray(),
            item => item.GetProperty("slug").GetString() == relatedSlug);

        var relatedId = await ReadGuidAsync(relatedDraft, "id");
        using var relatedAdmin = await client.GetAsync($"/api/v1/admin/wiki/articles/{relatedId}");
        relatedAdmin.EnsureSuccessStatusCode();
        var relatedEtag = relatedAdmin.Headers.ETag?.ToString();
        Assert.False(string.IsNullOrWhiteSpace(relatedEtag));
        csrf = await LocalApiFactory.GetCsrfAsync(client);
        using var publishRelated = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Post,
            $"/api/v1/admin/wiki/articles/{relatedId}/publish", new { }, csrf, relatedEtag);
        publishRelated.EnsureSuccessStatusCode();

        using var publicAfterRelatedPublish = await client.GetAsync($"/api/v1/wiki/articles/{articleSlug}");
        publicAfterRelatedPublish.EnsureSuccessStatusCode();
        using var afterRelatedJson = JsonDocument.Parse(await publicAfterRelatedPublish.Content.ReadAsStreamAsync());
        Assert.Contains(afterRelatedJson.RootElement.GetProperty("related").EnumerateArray(),
            item => item.GetProperty("slug").GetString() == relatedSlug);
    }

    [Fact]
    public async Task ConcurrentArticleEditsWithSameEtagHaveOneWinnerPerRound()
    {
        using var firstEditor = await factory.CreateAuthenticatedClientAsync(factory.AdminEmail);
        using var secondEditor = await factory.CreateAuthenticatedClientAsync(factory.AdminEmail);
        var suffix = Guid.NewGuid().ToString("N");
        var categoryCsrf = await LocalApiFactory.GetCsrfAsync(firstEditor);
        using var category = await LocalApiFactory.SendJsonAsync(firstEditor, HttpMethod.Post,
            "/api/v1/admin/wiki/categories",
            new { name = $"Stress Category {suffix}", slug = $"stress-category-{suffix}", displayOrder = 0 },
            categoryCsrf);
        Assert.Equal(HttpStatusCode.Created, category.StatusCode);
        var categoryId = await ReadGuidAsync(category, "id");
        var articleSlug = $"stress-article-{suffix}";
        var createCsrf = await LocalApiFactory.GetCsrfAsync(firstEditor);
        using var create = await LocalApiFactory.SendJsonAsync(firstEditor, HttpMethod.Post,
            "/api/v1/admin/wiki/articles",
            new
            {
                title = "Stress Initial",
                slug = articleSlug,
                summary = "Published snapshot remains private from later edits.",
                markdown = "# Stress Initial\n\nPublished snapshot.",
                categoryId,
                tagIds = Array.Empty<Guid>(),
                mediaIds = Array.Empty<Guid>()
            }, createCsrf);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var articleId = await ReadGuidAsync(create, "id");

        using var beforePublish = await firstEditor.GetAsync($"/api/v1/admin/wiki/articles/{articleId}");
        beforePublish.EnsureSuccessStatusCode();
        var publishCsrf = await LocalApiFactory.GetCsrfAsync(firstEditor);
        using var publish = await LocalApiFactory.SendJsonAsync(firstEditor, HttpMethod.Post,
            $"/api/v1/admin/wiki/articles/{articleId}/publish", new { }, publishCsrf,
            beforePublish.Headers.ETag?.ToString());
        Assert.Equal(HttpStatusCode.OK, publish.StatusCode);

        for (var round = 0; round < 10; round++)
        {
            using var current = await firstEditor.GetAsync($"/api/v1/admin/wiki/articles/{articleId}");
            current.EnsureSuccessStatusCode();
            var etag = current.Headers.ETag?.ToString();
            Assert.False(string.IsNullOrWhiteSpace(etag));
            var firstEdit = new
            {
                title = $"Stress editor one {round}", slug = articleSlug,
                summary = $"Winner one at round {round}.",
                markdown = $"# Winner one\n\nRound {round}.", categoryId,
                tagIds = Array.Empty<Guid>(), mediaIds = Array.Empty<Guid>()
            };
            var secondEdit = new
            {
                title = $"Stress editor two {round}", slug = articleSlug,
                summary = $"Winner two at round {round}.",
                markdown = $"# Winner two\n\nRound {round}.", categoryId,
                tagIds = Array.Empty<Guid>(), mediaIds = Array.Empty<Guid>()
            };
            var firstCsrf = await LocalApiFactory.GetCsrfAsync(firstEditor);
            var secondCsrf = await LocalApiFactory.GetCsrfAsync(secondEditor);
            var results = await Task.WhenAll(
                LocalApiFactory.SendJsonAsync(firstEditor, HttpMethod.Patch,
                    $"/api/v1/admin/wiki/articles/{articleId}", firstEdit, firstCsrf, etag),
                LocalApiFactory.SendJsonAsync(secondEditor, HttpMethod.Patch,
                    $"/api/v1/admin/wiki/articles/{articleId}", secondEdit, secondCsrf, etag));
            using var firstResult = results[0];
            using var secondResult = results[1];

            var observedStatuses = string.Join(", ", results.Select(result => (int)result.StatusCode));
            Assert.True(results.Count(result => result.StatusCode == HttpStatusCode.OK) == 1,
                $"Expected one winning edit; observed HTTP {observedStatuses}.");
            var winner = firstResult.StatusCode == HttpStatusCode.OK ? firstEdit : secondEdit;
            var loser = firstResult.StatusCode == HttpStatusCode.OK ? secondResult : firstResult;
            Assert.Contains(loser.StatusCode,
                new[] { HttpStatusCode.PreconditionFailed, HttpStatusCode.Conflict });
            if (loser.StatusCode == HttpStatusCode.Conflict)
            {
                // Serializable SQL Server transactions may choose the losing request as
                // deadlock victim; that path intentionally maps error 1205 to retryable 409.
                using var problem = JsonDocument.Parse(await loser.Content.ReadAsStreamAsync());
                Assert.Equal(409, problem.RootElement.GetProperty("status").GetInt32());
                Assert.Contains("Concurrent update conflict",
                    problem.RootElement.GetProperty("title").GetString());
            }
            using var afterRace = await firstEditor.GetAsync($"/api/v1/admin/wiki/articles/{articleId}");
            afterRace.EnsureSuccessStatusCode();
            using var draft = JsonDocument.Parse(await afterRace.Content.ReadAsStreamAsync());
            Assert.Equal(winner.title, draft.RootElement.GetProperty("title").GetString());
            Assert.Equal(winner.markdown, draft.RootElement.GetProperty("markdown").GetString());
        }

        using var publicArticle = await firstEditor.GetAsync($"/api/v1/wiki/articles/{articleSlug}");
        publicArticle.EnsureSuccessStatusCode();
        using var publicJson = JsonDocument.Parse(await publicArticle.Content.ReadAsStreamAsync());
        Assert.Equal("Stress Initial", publicJson.RootElement.GetProperty("title").GetString());
        Assert.Equal(1, publicJson.RootElement.GetProperty("revision").GetInt32());
    }

    [Fact]
    public async Task MediaUploadRejectsSpoofedImageSignature()
    {
        using var client = await factory.CreateAuthenticatedClientAsync(factory.AdminEmail);
        var csrf = await LocalApiFactory.GetCsrfAsync(client);

        using var response = await LocalApiFactory.SendMultipartAsync(client,
            "/api/v1/admin/wiki/media", "file", "not-an-image.png", "image/png",
            "MZ-this-is-not-a-png"u8.ToArray(), csrf);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task MediaIsPrivateUntilReferencedRevisionIsPublished()
    {
        using var client = await factory.CreateAuthenticatedClientAsync(factory.AdminEmail);
        var csrf = await LocalApiFactory.GetCsrfAsync(client);
        using var upload = await LocalApiFactory.SendMultipartAsync(client,
            "/api/v1/admin/wiki/media", "file", "pixel.png", "image/png", TinyPng, csrf);
        upload.EnsureSuccessStatusCode();
        var mediaId = await ReadGuidAsync(upload, "id");

        using var privateMedia = await client.GetAsync($"/api/v1/wiki/media/{mediaId}");
        Assert.Equal(HttpStatusCode.NotFound, privateMedia.StatusCode);

        var suffix = Guid.NewGuid().ToString("N");
        csrf = await LocalApiFactory.GetCsrfAsync(client);
        using var category = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Post,
            "/api/v1/admin/wiki/categories",
            new { name = $"Media Category {suffix}", slug = $"media-category-{suffix}", displayOrder = 0 }, csrf);
        category.EnsureSuccessStatusCode();
        var categoryId = await ReadGuidAsync(category, "id");

        csrf = await LocalApiFactory.GetCsrfAsync(client);
        using var article = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Post,
            "/api/v1/admin/wiki/articles",
            new
            {
                title = "Media Article",
                slug = $"media-article-{suffix}",
                summary = "Media integration test.",
                markdown = $"![pixel](/api/v1/wiki/media/{mediaId})",
                categoryId,
                tagIds = Array.Empty<Guid>(),
                mediaIds = new[] { mediaId }
            }, csrf);
        article.EnsureSuccessStatusCode();
        var articleId = await ReadGuidAsync(article, "id");

        using var articleDraft = await client.GetAsync($"/api/v1/admin/wiki/articles/{articleId}");
        articleDraft.EnsureSuccessStatusCode();
        var etag = articleDraft.Headers.ETag?.ToString();
        Assert.False(string.IsNullOrWhiteSpace(etag));
        csrf = await LocalApiFactory.GetCsrfAsync(client);
        using var publish = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Post,
            $"/api/v1/admin/wiki/articles/{articleId}/publish", new { }, csrf, etag);
        publish.EnsureSuccessStatusCode();

        using var publicMedia = await client.GetAsync($"/api/v1/wiki/media/{mediaId}");
        publicMedia.EnsureSuccessStatusCode();
        Assert.Equal("image/png", publicMedia.Content.Headers.ContentType?.MediaType);
        Assert.Equal(TinyPng, await publicMedia.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task RevisionHistoryRestoreKeepsPublicRevisionAndWritesAudit()
    {
        using var client = await factory.CreateAuthenticatedClientAsync(factory.AdminEmail);
        var suffix = Guid.NewGuid().ToString("N");
        var csrf = await LocalApiFactory.GetCsrfAsync(client);
        using var category = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Post,
            "/api/v1/admin/wiki/categories",
            new { name = $"Revision Category {suffix}", slug = $"revision-category-{suffix}", displayOrder = 0 }, csrf);
        category.EnsureSuccessStatusCode();
        var categoryId = await ReadGuidAsync(category, "id");

        csrf = await LocalApiFactory.GetCsrfAsync(client);
        var articleBody = new
        {
            title = "Revision One",
            slug = $"revision-article-{suffix}",
            summary = "Revision summary.",
            markdown = "Revision one body.",
            categoryId,
            tagIds = Array.Empty<Guid>(),
            mediaIds = Array.Empty<Guid>()
        };
        using var article = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Post,
            "/api/v1/admin/wiki/articles", articleBody, csrf);
        article.EnsureSuccessStatusCode();
        var articleId = await ReadGuidAsync(article, "id");

        async Task<string> CurrentArticleEtagAsync()
        {
            using var current = await client.GetAsync($"/api/v1/admin/wiki/articles/{articleId}");
            current.EnsureSuccessStatusCode();
            return current.Headers.ETag?.ToString() ?? throw new InvalidOperationException("Missing article ETag.");
        }

        csrf = await LocalApiFactory.GetCsrfAsync(client);
        using var publishOne = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Post,
            $"/api/v1/admin/wiki/articles/{articleId}/publish", new { }, csrf, await CurrentArticleEtagAsync());
        publishOne.EnsureSuccessStatusCode();

        var firstRevisionId = await ReadGuidAsync(publishOne, "revisionId");
        csrf = await LocalApiFactory.GetCsrfAsync(client);
        using var edit = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Patch,
            $"/api/v1/admin/wiki/articles/{articleId}",
            new
            {
                title = "Revision Two", articleBody.slug, articleBody.summary,
                markdown = "Revision two body.", categoryId, tagIds = Array.Empty<Guid>(), mediaIds = Array.Empty<Guid>()
            }, csrf, await CurrentArticleEtagAsync());
        edit.EnsureSuccessStatusCode();

        csrf = await LocalApiFactory.GetCsrfAsync(client);
        using var publishTwo = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Post,
            $"/api/v1/admin/wiki/articles/{articleId}/publish", new { }, csrf, await CurrentArticleEtagAsync());
        publishTwo.EnsureSuccessStatusCode();

        using var publicBeforeRestore = await client.GetAsync($"/api/v1/wiki/articles/{articleBody.slug}");
        publicBeforeRestore.EnsureSuccessStatusCode();
        using var publicBeforeJson = JsonDocument.Parse(await publicBeforeRestore.Content.ReadAsStreamAsync());
        Assert.Equal("Revision Two", publicBeforeJson.RootElement.GetProperty("title").GetString());

        using var revisions = await client.GetAsync($"/api/v1/admin/wiki/articles/{articleId}/revisions");
        revisions.EnsureSuccessStatusCode();
        using var revisionJson = JsonDocument.Parse(await revisions.Content.ReadAsStreamAsync());
        Assert.Equal(2, revisionJson.RootElement.GetArrayLength());

        csrf = await LocalApiFactory.GetCsrfAsync(client);
        using var restore = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Post,
            $"/api/v1/admin/wiki/articles/{articleId}/revisions/{firstRevisionId}/restore", new { }, csrf,
            await CurrentArticleEtagAsync());
        restore.EnsureSuccessStatusCode();

        using var draftAfterRestore = await client.GetAsync($"/api/v1/admin/wiki/articles/{articleId}");
        draftAfterRestore.EnsureSuccessStatusCode();
        using var draftJson = JsonDocument.Parse(await draftAfterRestore.Content.ReadAsStreamAsync());
        Assert.Equal("Revision One", draftJson.RootElement.GetProperty("title").GetString());

        using var publicAfterRestore = await client.GetAsync($"/api/v1/wiki/articles/{articleBody.slug}");
        publicAfterRestore.EnsureSuccessStatusCode();
        using var publicAfterJson = JsonDocument.Parse(await publicAfterRestore.Content.ReadAsStreamAsync());
        Assert.Equal("Revision Two", publicAfterJson.RootElement.GetProperty("title").GetString());

        using var audit = await client.GetAsync($"/api/v1/admin/audit?entityType=WikiArticle&entityId={articleId}");
        audit.EnsureSuccessStatusCode();
        using var auditJson = JsonDocument.Parse(await audit.Content.ReadAsStreamAsync());
        var actions = auditJson.RootElement.GetProperty("items").EnumerateArray()
            .Select(item => item.GetProperty("action").GetString()).ToArray();
        Assert.Contains("article.published", actions);
        Assert.Contains("article.revision_restored", actions);
    }

    [Fact]
    public async Task NewsIsPublishedOnlyAndUsesEtagLifecycle()
    {
        using var client = await factory.CreateAuthenticatedClientAsync(factory.AdminEmail);
        var suffix = Guid.NewGuid().ToString("N");
        var slug = $"news-{suffix}";
        var input = new { title = "Local News", slug, summary = "Local news summary.", markdown = "News body." };
        var csrf = await LocalApiFactory.GetCsrfAsync(client);
        using var create = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Post, "/api/v1/admin/news", input, csrf);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var newsId = await ReadGuidAsync(create, "id");

        using var before = await client.GetAsync($"/api/v1/news/{slug}");
        Assert.Equal(HttpStatusCode.NotFound, before.StatusCode);
        using var admin = await client.GetAsync($"/api/v1/admin/news/{newsId}");
        admin.EnsureSuccessStatusCode();
        var etag = admin.Headers.ETag?.ToString();
        Assert.False(string.IsNullOrWhiteSpace(etag));

        csrf = await LocalApiFactory.GetCsrfAsync(client);
        using var missing = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Patch,
            $"/api/v1/admin/news/{newsId}", new { title = "Changed", input.slug, input.summary, input.markdown }, csrf);
        Assert.Equal((HttpStatusCode)428, missing.StatusCode);

        csrf = await LocalApiFactory.GetCsrfAsync(client);
        using var publish = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Post,
            $"/api/v1/admin/news/{newsId}/publish", new { }, csrf, etag);
        publish.EnsureSuccessStatusCode();

        using var publicNews = await client.GetAsync($"/api/v1/news/{slug}");
        publicNews.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await publicNews.Content.ReadAsStreamAsync());
        Assert.Equal("Local News", json.RootElement.GetProperty("title").GetString());
    }

    private static readonly byte[] TinyPng = Convert.FromHexString(
        "89504E470D0A1A0A0000000D49484452000000010000000108060000001F15C489" +
        "0000000D49444154789C6360000000020001E221BC330000000049454E44AE426082");

    private static async Task<Guid> ReadGuidAsync(HttpResponseMessage response, string property)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        return document.RootElement.GetProperty(property).GetGuid();
    }
}
