using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using NovaHaven.Api.Controllers;
using NovaHaven.Application.Features.Wiki.Services;
using NovaHaven.Infrastructure.Data;
using Xunit;

namespace NovaHaven.Integration.Tests;

public sealed class WikiControllerArchitectureTests
{
    [Fact]
    public void Public_wiki_controller_uses_explicit_anonymous_api_routes()
    {
        var controller = typeof(WikiController);

        Assert.NotNull(controller.GetCustomAttribute<ApiControllerAttribute>());
        Assert.NotNull(controller.GetCustomAttribute<Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute>());
        Assert.Equal("api/v1/wiki", controller.GetCustomAttribute<RouteAttribute>()?.Template);

        var routes = controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .SelectMany(method => method.GetCustomAttributes<HttpMethodAttribute>()
                .Select(attribute => $"{attribute.HttpMethods.Single()} /api/v1/wiki/{attribute.Template}"))
            .OrderBy(route => route, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            [
                "GET /api/v1/wiki/articles",
                "GET /api/v1/wiki/articles/{slug}",
                "GET /api/v1/wiki/categories",
                "GET /api/v1/wiki/tags"
            ],
            routes);
    }

    [Fact]
    public void Public_wiki_controller_depends_on_bll_service_not_ef_context()
    {
        var constructor = Assert.Single(typeof(WikiController).GetConstructors());
        var dependencies = constructor.GetParameters().Select(parameter => parameter.ParameterType).ToArray();

        Assert.Contains(typeof(WikiReadService), dependencies);
        Assert.DoesNotContain(typeof(NovaDbContext), dependencies);
    }
}
