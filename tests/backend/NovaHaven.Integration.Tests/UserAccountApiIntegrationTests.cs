using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NovaHaven.Domain.Notifications.Entities;
using NovaHaven.Infrastructure.Data;
using NovaHaven.Infrastructure.Identity;
using NovaHaven.Infrastructure.Notifications;
using Xunit;

namespace NovaHaven.Integration.Tests;

public sealed class UserAccountApiIntegrationTests(LocalApiFactory factory) : IClassFixture<LocalApiFactory>
{
    [Fact]
    public async Task Registration_creates_an_active_standard_account_without_email_and_signs_in()
    {
        using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var email = $"new-player-{Guid.NewGuid():N}@local.test";

        using var missingCsrf = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password = "Player-Password-912!"
        });
        Assert.Equal(HttpStatusCode.BadRequest, missingCsrf.StatusCode);

        var csrf = await LocalApiFactory.GetCsrfAsync(client);
        using var registration = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Post,
            "/api/v1/auth/register", new { email, password = "Player-Password-912!" }, csrf);
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);

        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByEmailAsync(email);
            Assert.NotNull(user);
            Assert.True(user.EmailConfirmed);
            Assert.False(await users.IsInRoleAsync(user, "Admin"));
        }

        using var currentUser = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, currentUser.StatusCode);

        var signedInCsrf = await LocalApiFactory.GetCsrfAsync(client);
        using var duplicate = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Post,
            "/api/v1/auth/register", new { email, password = "Player-Password-912!" }, signedInCsrf);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        signedInCsrf = await LocalApiFactory.GetCsrfAsync(client);
        using var logout = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Post,
            "/api/v1/auth/logout", new { }, signedInCsrf);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        var signedOutCsrf = await LocalApiFactory.GetCsrfAsync(client);
        using var login = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Post,
            "/api/v1/auth/login", new { email, password = "Player-Password-912!" }, signedOutCsrf);
        Assert.Equal(HttpStatusCode.NoContent, login.StatusCode);

        using var signedInUser = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, signedInUser.StatusCode);
    }

    [Fact]
    public async Task Existing_unconfirmed_account_can_sign_in()
    {
        var email = $"legacy-{Guid.NewGuid():N}@local.test";
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = false };
            var created = await users.CreateAsync(user, "Player-Password-912!");
            Assert.True(created.Succeeded, string.Join("; ", created.Errors.Select(error => error.Code)));
        }

        using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var csrf = await LocalApiFactory.GetCsrfAsync(client);
        using var login = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Post,
            "/api/v1/auth/login", new { email, password = "Player-Password-912!" }, csrf);
        Assert.Equal(HttpStatusCode.NoContent, login.StatusCode);

        using var currentUser = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, currentUser.StatusCode);
    }

    [Fact]
    public async Task Push_configuration_requires_player_auth_and_returns_only_the_public_key()
    {
        using var anonymous = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        using var denied = await anonymous.GetAsync("/api/v1/notifications/push/config");
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);

        using var player = await factory.CreateAuthenticatedClientAsync(factory.EditorEmail);
        using var configured = await player.GetAsync("/api/v1/notifications/push/config");
        Assert.Equal(HttpStatusCode.OK, configured.StatusCode);
        using var json = JsonDocument.Parse(await configured.Content.ReadAsStreamAsync());
        Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("publicKey").GetString()));
        Assert.False(json.RootElement.TryGetProperty("privateKey", out _));

        var endpoint = $"https://fcm.googleapis.com/fcm/send/{Guid.NewGuid():N}";
        var csrf = await LocalApiFactory.GetCsrfAsync(player);
        using var saved = await LocalApiFactory.SendJsonAsync(player, HttpMethod.Put,
            "/api/v1/notifications/push/subscriptions",
            new { endpoint, keys = new { p256dh = "test-p256dh", auth = "test-auth" } }, csrf);
        Assert.Equal(HttpStatusCode.NoContent, saved.StatusCode);
        csrf = await LocalApiFactory.GetCsrfAsync(player);
        using var deleted = await LocalApiFactory.SendJsonAsync(player, HttpMethod.Delete,
            "/api/v1/notifications/push/subscriptions", new { endpoint }, csrf);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
    }

    [Fact]
    public async Task Development_vapid_key_pair_is_generated_persisted_and_reused()
    {
        var keyPath = Path.Combine(Path.GetTempPath(), $"nova-vapid-{Guid.NewGuid():N}.json");
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["WebPush:Subject"] = "mailto:local-test@example.test",
            ["WebPush:LocalKeyPath"] = keyPath
        }).Build();
        var environment = factory.Services.GetRequiredService<IHostEnvironment>();
        try
        {
            var first = await new VapidKeyProvider(configuration, environment).GetAsync();
            var second = await new VapidKeyProvider(configuration, environment).GetAsync();

            Assert.NotNull(first);
            Assert.NotNull(second);
            Assert.False(string.IsNullOrWhiteSpace(first.PublicKey));
            Assert.False(string.IsNullOrWhiteSpace(first.PrivateKey));
            Assert.Equal(first.PublicKey, second.PublicKey);
            Assert.Equal(first.PrivateKey, second.PrivateKey);
            Assert.True(File.Exists(keyPath));
        }
        finally
        {
            if (File.Exists(keyPath)) File.Delete(keyPath);
        }
    }

    [Fact]
    public async Task Notification_inbox_pages_only_the_current_users_rows_and_validates_offsets()
    {
        using var client = await factory.CreateAuthenticatedClientAsync(factory.EditorEmail);
        Guid firstUnreadId;
        Guid otherUserNotificationId;
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var current = await users.FindByEmailAsync(factory.EditorEmail);
            var other = await users.FindByEmailAsync(factory.AdminEmail);
            Assert.NotNull(current);
            Assert.NotNull(other);

            var db = scope.ServiceProvider.GetRequiredService<NovaDbContext>();
            var now = DateTimeOffset.UtcNow;
            var rows = Enumerable.Range(1, 23)
                .Select(index => UserNotification.Create(current.Id, $"Thông báo {index}", "Nội dung kiểm thử", "/news", now.AddMinutes(-index)))
                .ToArray();
            rows[0].MarkRead(now);
            firstUnreadId = rows[1].Id;
            db.UserNotifications.AddRange(rows);
            var privateRow = UserNotification.Create(other.Id, "Riêng tư", "Không được lộ", "/wiki", now);
            otherUserNotificationId = privateRow.Id;
            db.UserNotifications.Add(privateRow);
            await db.SaveChangesAsync();
        }

        using var page = await client.GetAsync("/api/v1/notifications?page=2&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        using var json = JsonDocument.Parse(await page.Content.ReadAsStreamAsync());
        var root = json.RootElement;
        Assert.Equal(2, root.GetProperty("page").GetInt32());
        Assert.Equal(10, root.GetProperty("pageSize").GetInt32());
        Assert.Equal(23, root.GetProperty("total").GetInt32());
        Assert.Equal(22, root.GetProperty("unreadCount").GetInt32());
        Assert.Equal(10, root.GetProperty("items").GetArrayLength());
        Assert.DoesNotContain(root.GetProperty("items").EnumerateArray(),
            item => item.GetProperty("title").GetString() == "Riêng tư");

        using var invalidPage = await client.GetAsync("/api/v1/notifications?page=2147483647&pageSize=50");
        Assert.Equal(HttpStatusCode.BadRequest, invalidPage.StatusCode);

        var csrf = await LocalApiFactory.GetCsrfAsync(client);
        using var markOwn = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Put,
            $"/api/v1/notifications/{firstUnreadId}/read", new { }, csrf);
        Assert.Equal(HttpStatusCode.NoContent, markOwn.StatusCode);
        using var markOther = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Put,
            $"/api/v1/notifications/{otherUserNotificationId}/read", new { }, csrf);
        Assert.Equal(HttpStatusCode.NotFound, markOther.StatusCode);
        using var unreadCount = await client.GetAsync("/api/v1/notifications/unread-count");
        using var unreadJson = JsonDocument.Parse(await unreadCount.Content.ReadAsStreamAsync());
        Assert.Equal(21, unreadJson.RootElement.GetProperty("unreadCount").GetInt32());
    }
}
