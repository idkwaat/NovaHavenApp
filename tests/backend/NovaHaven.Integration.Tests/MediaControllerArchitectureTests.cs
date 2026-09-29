using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using NovaHaven.Api.Controllers;
using NovaHaven.Application.Features.Media.Services;
using NovaHaven.Infrastructure.Data;
using Xunit;

namespace NovaHaven.Integration.Tests;

public sealed class MediaControllerArchitectureTests
{
    [Fact]
    public void Media_routes_keep_admin_protection_and_published_only_public_read()
    {
        var controller = typeof(MediaController);
        Assert.NotNull(controller.GetCustomAttribute<ApiControllerAttribute>());
        Assert.Equal("api/v1", controller.GetCustomAttribute<RouteAttribute>()?.Template);
        Assert.Equal("AdminOnly", controller.GetCustomAttribute<AuthorizeAttribute>()?.Policy);

        var methods = controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        var routes = methods.SelectMany(method => method.GetCustomAttributes<HttpMethodAttribute>()
                .Select(attribute => $"{attribute.HttpMethods.Single()} api/v1/{attribute.Template}"))
            .OrderBy(route => route, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            [
                "GET api/v1/admin/wiki/media",
                "GET api/v1/admin/wiki/media/{id:guid}",
                "GET api/v1/wiki/media/{id:guid}",
                "POST api/v1/admin/wiki/media"
            ],
            routes);

        var publicRead = Assert.Single(methods, method => method.GetCustomAttributes<HttpMethodAttribute>()
            .Any(attribute => attribute.HttpMethods.Contains("GET") &&
                              attribute.Template!.StartsWith("wiki/", StringComparison.Ordinal)));
        Assert.NotNull(publicRead.GetCustomAttribute<AllowAnonymousAttribute>());
        var upload = Assert.Single(methods, method => method.GetCustomAttributes<HttpMethodAttribute>()
            .Any(attribute => attribute.HttpMethods.Contains("POST")));
        Assert.NotNull(upload.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());

        var constructor = Assert.Single(controller.GetConstructors());
        var dependencies = constructor.GetParameters().Select(parameter => parameter.ParameterType).ToArray();
        Assert.Contains(typeof(WikiMediaService), dependencies);
        Assert.DoesNotContain(typeof(NovaDbContext), dependencies);
    }
}
