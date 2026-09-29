using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using NovaHaven.Infrastructure.Identity;
using Xunit;

namespace NovaHaven.Integration.Tests;

public sealed class UserAccountApiIntegrationTests(LocalApiFactory factory) : IClassFixture<LocalApiFactory>
{
    [Fact]
    public async Task Registration_requires_csrf_and_does_not_grant_admin_or_allow_unconfirmed_login()
    {
        using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        const string email = "new-player@local.test";

        using var missingCsrf = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password = "Player-Password-912!"
        });
        Assert.Equal(HttpStatusCode.BadRequest, missingCsrf.StatusCode);

        var csrf = await LocalApiFactory.GetCsrfAsync(client);
        using var registration = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Post,
            "/api/v1/auth/register", new { email, password = "Player-Password-912!" }, csrf);
        Assert.Equal(HttpStatusCode.Accepted, registration.StatusCode);

        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByEmailAsync(email);
            Assert.NotNull(user);
            Assert.False(user.EmailConfirmed);
            Assert.False(await users.IsInRoleAsync(user, "Admin"));
        }

        using var login = await LocalApiFactory.SendJsonAsync(client, HttpMethod.Post,
            "/api/v1/auth/login", new { email, password = "Player-Password-912!" }, csrf);
        Assert.Equal(HttpStatusCode.Forbidden, login.StatusCode);
    }
}
